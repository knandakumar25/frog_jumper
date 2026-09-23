#!/bin/sh
set -e

echo "Waiting for PostgreSQL to be ready..."
python << END
import time
import sys
import psycopg2
import os

host = os.environ.get("DB_HOST", "db")
port = os.environ.get("DB_PORT", "5432")
user = os.environ.get("DB_USER", "postgres")
password = os.environ.get("DB_PASSWORD", "postgres")
dbname = os.environ.get("DB_NAME", "accounts_db")

retries = 30
while retries > 0:
    try:
        conn = psycopg2.connect(
            host=host,
            port=port,
            user=user,
            password=password,
            dbname=dbname,
            connect_timeout=3
        )
        conn.close()
        print("PostgreSQL is available!")
        sys.exit(0)
    except psycopg2.OperationalError:
        print("PostgreSQL not ready yet, retrying in 2 seconds...")
        retries -= 1
        time.sleep(2)

print("Timed out waiting for PostgreSQL.")
sys.exit(1)
END

echo "Applying database migrations..."
python manage.py migrate --no-input

echo "Starting application: $@"
exec "$@"
