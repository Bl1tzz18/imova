FROM node:20-alpine AS base
WORKDIR /app

# NEXT_PUBLIC_* vars are inlined into the client bundle at build time, so this has to be a build
# arg (not just a runtime env var) — it's the origin the *browser* uses to call the backend
# directly for the image-upload endpoints (see lib/api/media.ts).
ARG NEXT_PUBLIC_API_URL=http://localhost:8080
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

COPY package.json package-lock.json* ./
RUN npm install

COPY . .
RUN npm run build

ENV PORT=3000
EXPOSE 3000

CMD ["npm", "start"]
