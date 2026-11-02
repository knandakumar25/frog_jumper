"""Validate Apple presets and prepare an iOS source handoff; never invoke Godot or Apple signing tools.

The Docker driver calls prepare-ios only after an explicit export request. The check
commands inspect files without writing a bundle. Signing material is excluded.
"""
import argparse
import configparser
import json
import os
from pathlib import Path
import re
import zipfile


ROOT_FILES = (
    "project.godot", "AxiomForce.csproj", "export_presets.cfg",
    "default_bus_layout.tres", "icon.svg", "Root.resx", "Resources.resx",
)
ROOT_DIRS = ("assets", "scenes", "scripts", "addons", "Properties", "export")
EXCLUDED_DIRS = {".git", ".github", ".godot", ".validation", ".venv", ".ssh", ".aws", "__pycache__", "node_modules", "obj"}
SECRET_SUFFIXES = {".p8", ".p12", ".pfx", ".pem", ".key", ".keystore", ".jks", ".mobileprovision"}


def preset_options(root, name):
    """Read a named Godot export preset without interpolation or modifying the tracked file."""
    parser = configparser.ConfigParser(interpolation=None, strict=False)
    parser.read(root / "export_presets.cfg", encoding="utf-8")
    for section in parser.sections():
        if re.fullmatch(r"preset\.\d+", section):
            if json.loads(parser.get(section, "name")) == name:
                return dict(parser.items(section + ".options"))
    raise ValueError(f"Missing {name} export preset.")


def validate_apple(root, platform):
    """Require a real bundle identifier and, for iOS, the owner's ten-character Apple Team ID."""
    options = preset_options(root, platform)
    bundle = json.loads(options.get("application/bundle_identifier", '""'))
    if not re.fullmatch(r"(?:[A-Za-z0-9][A-Za-z0-9-]*\.){2,}[A-Za-z0-9-]+", bundle):
        raise ValueError("Set your reverse-DNS application/bundle_identifier in the Apple export preset.")
    if ".example." in "." + bundle + "." or "yourcompany" in bundle.lower():
        raise ValueError("Replace the example bundle identifier with an identifier you own.")
    if platform == "iOS":
        team = json.loads(options.get("application/app_store_team_id", '""'))
        if not re.fullmatch(r"[A-Z0-9]{10}", team):
            raise ValueError("Set your ten-character application/app_store_team_id in the iOS preset.")
        if options.get("application/export_project_only") != "true":
            raise ValueError("Keep application/export_project_only=true; finish the archive in Xcode.")
    elif options.get("codesign/codesign") != "0":
        raise ValueError("Docker macOS packaging requires codesign/codesign=0; sign later on a Mac.")


def safe_file(path):
    """Keep runtime/source files while excluding local credentials and symlinks."""
    name = path.name.lower()
    return (
        not path.is_symlink()
        and not name.startswith(".env")
        and name not in {".netrc", ".npmrc", "export_credentials.cfg", "id_rsa", "id_ed25519"}
        and path.suffix.lower() not in SECRET_SUFFIXES
    )


def source_files(root):
    """Collect portable project inputs from explicit roots; preserve native add-on bin directories."""
    root = root.resolve()
    for required in ("project.godot", "AxiomForce.csproj", "export_presets.cfg"):
        if not (root / required).is_file():
            raise ValueError(f"Missing project input: {required}")
    files = []
    for name in ROOT_FILES:
        path = root / name
        if path.is_file() and safe_file(path):
            files.append(path)
    for name in ROOT_DIRS:
        directory = root / name
        if not directory.is_dir() or directory.is_symlink():
            continue
        for parent, directories, names in os.walk(directory, followlinks=False):
            directories[:] = sorted(
                item for item in directories
                if item.lower() not in EXCLUDED_DIRS and not (Path(parent) / item).is_symlink()
            )
            for item in sorted(names):
                path = Path(parent) / item
                if safe_file(path) and path.resolve().is_relative_to(root):
                    files.append(path)
    return sorted(files, key=lambda path: path.relative_to(root).as_posix())


def sanitized_presets(text):
    """Remove signing passwords, encryption keys and provisioning paths from the source handoff."""
    lines = []
    for line in text.splitlines():
        key = line.split("=", 1)[0].strip().lower()
        sensitive = (
            "password" in key or key.endswith("_pass") or "script_encryption_key" == key
            or "certificate_file" in key or "provisioning_profile" in key
            or "api_key" in key or "apple_id_name" in key
        )
        lines.append(key + '=""' if sensitive and "=" in line else line)
    return "\n".join(lines) + "\n"


def prepare_ios(root):
    """Atomically write a source archive for a Mac; this is neither an Xcode project nor an IPA."""
    root = root.resolve()
    files = source_files(root)
    output = root / "build/ios/AxiomForce-iOS-source.zip"
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_suffix(".zip.tmp")
    try:
        with zipfile.ZipFile(temporary, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for path in files:
                relative = path.relative_to(root).as_posix()
                if relative == "export_presets.cfg":
                    archive.writestr(relative, sanitized_presets(path.read_text(encoding="utf-8")))
                else:
                    archive.write(path, relative)
            archive.writestr("APPLE_HANDOFF.json", json.dumps({
                "kind": "ios-source-handoff", "godot": "4.7.2-mono", "dotnet": "10",
                "next": "Read export/APPLE_RELEASE.md on a Mac with Xcode.",
                "not_an_ipa": True, "files": len(files),
            }, indent=2) + "\n")
        temporary.replace(output)
    finally:
        temporary.unlink(missing_ok=True)
    return output


def main():
    """Offer read-only checks and an explicit source-preparation command; errors exit nonzero."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("check", "check-macos", "check-ios", "prepare-ios"))
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    try:
        if args.command == "check":
            print(f"Portable source inputs: {len(source_files(args.root))}; no archive written.")
        elif args.command == "prepare-ios":
            if os.environ.get("GITHUB_ACTIONS") == "true" or not Path("/.dockerenv").is_file():
                raise ValueError("Prepare the iOS handoff through the local Docker export service.")
            print(f"iOS SOURCE HANDOFF ONLY: {prepare_ios(args.root)}")
            print("No Xcode project or IPA was built. Complete the documented Mac steps.")
        else:
            platform = "macOS" if args.command == "check-macos" else "iOS"
            validate_apple(args.root, platform)
            print(f"{platform} preset identifiers validated; no export performed.")
    except (ValueError, OSError, configparser.Error, json.JSONDecodeError) as error:
        parser.exit(2, f"ERROR: {error}\n")


if __name__ == "__main__":
    main()
