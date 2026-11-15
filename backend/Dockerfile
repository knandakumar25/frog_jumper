# syntax=docker/dockerfile:1
# Axiom Force accounts API (Express.js on Node 26), replacing the Django backend on 2026-11-15.
#
# Build context is ./backend (see docker-compose.yml). Stages:
#   deps     - production packages only (npm ci --omit=dev)
#   dev      - adds the test packages; the target Compose runs, so
#              `docker compose exec -T backend npm test` works in the running container
#   runtime  - production image: production packages and source, nothing else
# Both final targets run as the image's unprivileged `node` user. Start-up applies the SQL
# migrations, then serves on port 8000 (the port nginx, the game and the website already use).
FROM node:26.13.1-alpine3.23 AS base
WORKDIR /app
ENV NODE_ENV=production

FROM base AS deps
# The lockfile pins every package version; the npm cache mount keeps rebuilds fast without
# storing the cache in a layer.
RUN --mount=type=bind,source=package.json,target=package.json \
    --mount=type=bind,source=package-lock.json,target=package-lock.json \
    --mount=type=cache,target=/root/.npm \
    npm ci --omit=dev

FROM base AS dev-deps
RUN --mount=type=bind,source=package.json,target=package.json \
    --mount=type=bind,source=package-lock.json,target=package-lock.json \
    --mount=type=cache,target=/root/.npm \
    npm ci --include=dev

FROM base AS runtime
LABEL org.opencontainers.image.title="axiom-force-backend" \
      org.opencontainers.image.description="Axiom Force accounts API (Express.js)"
COPY --link --from=deps /app/node_modules ./node_modules
COPY --link package.json package-lock.json ./
COPY --link data ./data
COPY --link migrations ./migrations
COPY --link src ./src
USER node
EXPOSE 8000
# Migrate, then serve. Exec form, so the server gets SIGTERM directly and shuts down cleanly.
CMD ["node", "src/server.js"]

FROM runtime AS dev
USER root
COPY --link --from=dev-deps /app/node_modules ./node_modules
COPY --link test ./test
# Playwright helpers (tools/playwright/README.md): throwaway test API and loopback proxy.
COPY --link scripts ./scripts
USER node
