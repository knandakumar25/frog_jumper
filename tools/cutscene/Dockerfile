# Reproducible CPU-only artwork and video packaging environment.
FROM debian:trixie-slim
RUN apt-get update && apt-get install -y --no-install-recommends blender=4.3.2+dfsg-2 ffmpeg python3 && rm -rf /var/lib/apt/lists/*
WORKDIR /workspace
ENTRYPOINT ["python3", "tools/cutscene/build.py"]
