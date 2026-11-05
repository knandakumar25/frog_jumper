# Axiom Force — Django REST Framework backend image.
#
# Built on Python 3.14 because that is the interpreter the project targets.
# Dependencies are installed only inside this image; no host virtualenv is needed.
# `python:latest` is deliberately not
# used: it resolves to a newer CPython whose dependency tree has no prebuilt
# wheels yet and forces slow source builds.
FROM python:3.14-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# libpq5 is required at runtime by psycopg2; everything else stays out of the
# image to keep it small.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libpq5 \
    && rm -rf /var/lib/apt/lists/*

# Build from the repository root; copy its canonical manifest before application code for caching.
COPY requirements.txt /app/
RUN pip install --no-cache-dir -r requirements.txt

COPY backend/ /app/

# entrypoint.sh is checked out with CRLF endings on Windows, which makes the
# kernel reject the shebang and `set -e` fail on a stray \r. Normalise it.
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh

EXPOSE 8000

# The entrypoint blocks until PostgreSQL answers, runs `manage.py migrate`,
# then execs whatever command compose supplies (the dev server by default).
ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["python", "manage.py", "runserver", "0.0.0.0:8000"]
