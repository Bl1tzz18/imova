# Multi-stage: dependencies and the build happen in throwaway stages; the final image only holds
# Next's standalone server output (~200 MB instead of ~1.1 GB with every dev dependency and the
# build cache). Matters locally too — every `docker compose up --build frontend` used to leave a
# 1.1 GB dangling image behind.
FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install

FROM node:20-alpine AS build
WORKDIR /app
# NEXT_PUBLIC_* vars are inlined into the client bundle at build time, so this has to be a build
# arg (not just a runtime env var) — it's the origin the *browser* uses to call the backend
# directly for the image-upload endpoints (see lib/api/media.ts).
ARG NEXT_PUBLIC_API_URL=http://localhost:8080
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]
