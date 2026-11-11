# Reproducible CPU-only media tooling: ffmpeg/ffprobe for cutscene audio packaging and the
# media validators. Blender was removed on 2026-11-11; art now comes from SpriteCook
# (docs/ART_REGENERATION.md). Run a script with:
#   docker compose --profile cutscene run --rm cutscene tools/cutscene/<script>.py
FROM python:3.15.2-slim-trixie
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /workspace
ENTRYPOINT ["python3"]
