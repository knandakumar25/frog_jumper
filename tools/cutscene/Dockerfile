# syntax=docker/dockerfile:1
# Reproducible CPU-only media tooling: ffmpeg/ffprobe for cutscene audio packaging and the
# media validators. Blender was removed on 2026-11-11; art now comes from SpriteCook
# (docs/ART_REGENERATION.md). Run a script with:
#   docker compose --profile cutscene run --rm cutscene tools/cutscene/<script>.py
#
# libraqm0 (with the fribidi and harfbuzz libraries apt pulls in) and Pillow are here for
# tools/art/render_titles.py (added 2026-11-15, Phase 7). Pillow loads libraqm at run time to
# join Arabic letters, order Arabic and Hebrew right to left and place Thai vowel marks; the
# Windows host's Pillow has no libraqm, so the localized titles are rendered in this image:
#   docker compose --profile cutscene run --rm cutscene tools/art/render_titles.py <plate.png>
# This service runs only under the "cutscene" profile; the always-on stack never starts it.
FROM python:3.15.2-slim-trixie
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    rm -f /etc/apt/apt.conf.d/docker-clean && \
    apt-get update && apt-get install -y --no-install-recommends ffmpeg libraqm0
# Pinned to the Pillow version the host art tools use, so titles render the same way.
RUN --mount=type=cache,target=/root/.cache/pip pip install pillow==13.0.0
WORKDIR /workspace
ENTRYPOINT ["python3"]
