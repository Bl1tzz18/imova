# Multi-stage: dependencies and the build happen in throwaway stages; the final image only holds
# Next's standalone server output (~200 MB instead of ~1.1 GB with every dev dependency and the
# build cache). Matters locally too — every `docker compose up --build frontend` used to leave a
# 1.1 GB dangling image behind.
FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
# ci, not install: exactly the lock file, and fails (like CI) when it has drifted from package.json.
RUN npm ci

FROM node:24-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
# Everything environment-specific is a runtime variable (API_URL, PUBLIC_API_URL, SITE_URL, …), so
# the same image runs in dev and prod — nothing is baked in at build time.
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
# The release tag in Application Insights: the deploy passes the git commit (--build-arg APP_VERSION=<sha>).
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION
EXPOSE 3000
CMD ["node", "server.js"]
