# ─── Stage 1: Build React (from src/frontend) ──────────────────────────
FROM node:18-alpine AS build
WORKDIR /app

# point at the frontend package.json
COPY package*.json ./
RUN npm ci

# Copy React source files to the correct location
COPY public/ ./public/
COPY src/ ./src/
COPY tsconfig*.json ./
COPY .env* ./

# produce a build/ folder
RUN npm run build

# ─── Stage 2: Serve with Nginx ───────────────────────────────────────
FROM nginx:1.25-alpine

# copy the static build
COPY --from=build /app/build /usr/share/nginx/html

# serve healthz and SPA fallback
COPY nginx.conf /etc/nginx/conf.d/default.conf

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -qO- http://localhost/healthz || exit 1

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]