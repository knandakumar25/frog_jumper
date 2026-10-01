"""
Utility to download OCI / Docker container images using standard Python HTTP libraries
and package them into tar archives suitable for 'container.exe image load'.
Requires no Docker daemon or client.
"""

import os
import sys
import gzip
import json
import tarfile
import urllib.request
import ssl
import shutil

def fetch_image(image_ref, output_tar, arch="amd64", os_name="linux"):
    print(f"--> Fetching image '{image_ref}' ({os_name}/{arch}) -> '{output_tar}'")
    
    # Parse image reference
    if "/" in image_ref:
        parts = image_ref.split("/")
        if "." in parts[0] or ":" in parts[0]:
            registry = parts[0]
            repo = "/".join(parts[1:])
        else:
            registry = "registry-1.docker.io"
            repo = image_ref
    else:
        registry = "registry-1.docker.io"
        repo = f"library/{image_ref}"
        
    if ":" in repo:
        repo, tag = repo.split(":", 1)
    else:
        tag = "latest"
        
    full_repotag = f"{image_ref if ':' in image_ref else image_ref + ':latest'}"
    if not ("/" in image_ref) and not repo.startswith("library/"):
        full_repotag = f"{image_ref}:{tag}"
    elif repo.startswith("library/"):
        full_repotag = f"{repo.split('/', 1)[1]}:{tag}"

    ctx = ssl.create_default_context()
    
    # 1. Obtain Auth Bearer token
    auth_url = f"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repo}:pull"
    req = urllib.request.Request(auth_url)
    with urllib.request.urlopen(req, context=ctx) as resp:
        token = json.loads(resp.read().decode())["token"]
        
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.docker.distribution.manifest.v2+json, application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.oci.image.index.v1+json"
    }
    
    # 2. Fetch Manifest or Multi-arch Index
    manifest_url = f"https://{registry}/v2/{repo}/manifests/{tag}"
    req = urllib.request.Request(manifest_url, headers=headers)
    with urllib.request.urlopen(req, context=ctx) as resp:
        manifest_data = json.loads(resp.read().decode())
        
    if "manifests" in manifest_data:
        chosen_digest = None
        for m in manifest_data["manifests"]:
            platform = m.get("platform", {})
            if platform.get("architecture") == arch and platform.get("os") == os_name:
                chosen_digest = m["digest"]
                break
        if not chosen_digest:
            raise RuntimeError(f"Architecture '{os_name}/{arch}' not found in multi-arch manifest.")
            
        manifest_url = f"https://{registry}/v2/{repo}/manifests/{chosen_digest}"
        req = urllib.request.Request(manifest_url, headers=headers)
        with urllib.request.urlopen(req, context=ctx) as resp:
            manifest_data = json.loads(resp.read().decode())
            
    config_digest = manifest_data["config"]["digest"]
    layers = manifest_data["layers"]
    print(f"    Config Digest: {config_digest}")
    print(f"    Layers to download: {len(layers)}")
    
    # 3. Download Config JSON
    config_hex = config_digest.split(":")[-1]
    config_url = f"https://{registry}/v2/{repo}/blobs/{config_digest}"
    req = urllib.request.Request(config_url, headers=headers)
    with urllib.request.urlopen(req, context=ctx) as resp:
        config_bytes = resp.read()
        
    # 4. Download and unpack layers into temporary staging directory
    temp_dir = output_tar + ".staging"
    os.makedirs(temp_dir, exist_ok=True)
    layer_tar_paths = []
    
    try:
        config_filename = f"{config_hex}.json"
        with open(os.path.join(temp_dir, config_filename), "wb") as f:
            f.write(config_bytes)
            
        for i, layer in enumerate(layers):
            ldigest = layer["digest"]
            lhex = ldigest.split(":")[-1]
            layer_dir = os.path.join(temp_dir, lhex)
            os.makedirs(layer_dir, exist_ok=True)
            
            size_mb = layer.get("size", 0) / (1024 * 1024)
            print(f"    [{i+1}/{len(layers)}] Layer {ldigest[:19]} ({size_mb:.1f} MB)...")
            layer_url = f"https://{registry}/v2/{repo}/blobs/{ldigest}"
            req = urllib.request.Request(layer_url, headers=headers)
            with urllib.request.urlopen(req, context=ctx) as resp:
                compressed_data = resp.read()
                
            if compressed_data[:2] == b"\x1f\x8b":
                decompressed = gzip.decompress(compressed_data)
            else:
                decompressed = compressed_data
                
            layer_tar_path = os.path.join(layer_dir, "layer.tar")
            with open(layer_tar_path, "wb") as f:
                f.write(decompressed)
                
            with open(os.path.join(layer_dir, "VERSION"), "w") as f:
                f.write("1.0")
                
            v1_meta = {"id": lhex, "created": "1970-01-01T00:00:00Z", "container_config": {}}
            with open(os.path.join(layer_dir, "json"), "w") as f:
                json.dump(v1_meta, f)
                
            layer_tar_paths.append(f"{lhex}/layer.tar")
            
        # 5. Write manifest.json
        manifest_json = [
            {
                "Config": config_filename,
                "RepoTags": [full_repotag],
                "Layers": layer_tar_paths
            }
        ]
        with open(os.path.join(temp_dir, "manifest.json"), "w") as f:
            json.dump(manifest_json, f, indent=2)
            
        # 6. Archive to final .tar
        os.makedirs(os.path.dirname(os.path.abspath(output_tar)), exist_ok=True)
        with tarfile.open(output_tar, "w") as tar:
            for item in os.listdir(temp_dir):
                item_path = os.path.join(temp_dir, item)
                tar.add(item_path, arcname=item)
                
        final_size_mb = os.path.getsize(output_tar) / (1024 * 1024)
        print(f"--> Successfully created '{output_tar}' ({final_size_mb:.1f} MB)")
    finally:
        shutil.rmtree(temp_dir, ignore_errors=True)

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python fetch_image.py <image:tag> <output.tar>")
        sys.exit(1)
    fetch_image(sys.argv[1], sys.argv[2])
