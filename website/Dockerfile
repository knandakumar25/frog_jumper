# Axiom Force — web portal image.
#
# Two stages: Vite builds the static bundle, then Nginx serves it and proxies
# /api/ and /admin/ to the `backend` service (see nginx.conf), so the browser
# only ever talks to one origin.
FROM node:26-alpine AS builder

WORKDIR /app

# Install dependencies first so the npm layer survives source-only edits.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite inlines every VITE_* value into the bundle at build time, so the API
# base URL has to be known here rather than at runtime.
ARG VITE_API_URL=http://localhost:8000/api
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80
