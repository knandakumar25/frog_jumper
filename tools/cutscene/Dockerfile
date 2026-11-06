# Reproducible CPU-only artwork and video packaging environment.
FROM python:3.15.2-slim-trixie
# CLI orchestration uses Python 3.15.2; Blender retains its ABI-matched embedded
# distribution interpreter for bpy and its bundled native extensions.
RUN apt-get update && apt-get install -y --no-install-recommends blender=4.3.2+dfsg-2 ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /workspace
ENTRYPOINT ["python3", "tools/cutscene/build.py"]
