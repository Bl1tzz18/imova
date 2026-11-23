#!/usr/bin/env bash
# Builds the three images once and promotes the same tag from dev to prod.
#
#   scripts/deploy.sh build                 # build + push imova-{web,api,worker}:<git sha>
#   scripts/deploy.sh release dev  <tag>    # point dev's apps at that tag
#   scripts/deploy.sh release prod <tag>    # same images, after checking them on dev
#
# Needs: docker, the Azure CLI signed in (az login), and permission to push to the registry.
# ACR_NAME overrides the registry (default acrimova, as in infra/shared/terraform.tfvars).
set -euo pipefail

ACR_NAME="${ACR_NAME:-acrimova}"
REPO_ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"

usage() {
  sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'
  exit 1
}

build() {
  if [[ -n "$(git -C "$REPO_ROOT" status --porcelain)" ]]; then
    echo "The working tree has uncommitted changes: commit first, so the tag says exactly what's inside." >&2
    exit 1
  fi
  local tag registry
  tag="$(git -C "$REPO_ROOT" rev-parse --short=12 HEAD)"
  registry="$(az acr show --name "$ACR_NAME" --query loginServer --output tsv)"

  az acr login --name "$ACR_NAME"

  docker build --build-arg APP_VERSION="$tag" -t "$registry/imova-api:$tag" \
    -f "$REPO_ROOT/src/backend/Imova.Api/Dockerfile" "$REPO_ROOT"
  docker build --build-arg APP_VERSION="$tag" -t "$registry/imova-worker:$tag" \
    -f "$REPO_ROOT/src/backend/Imova.Worker/Dockerfile" "$REPO_ROOT"
  docker build --build-arg APP_VERSION="$tag" -t "$registry/imova-web:$tag" \
    "$REPO_ROOT/src/frontend/imova-web"

  for image in imova-api imova-worker imova-web; do
    docker push "$registry/$image:$tag"
  done

  # Local copies aren't needed after the push (they fill the WSL disk otherwise).
  docker image rm "$registry/imova-api:$tag" "$registry/imova-worker:$tag" "$registry/imova-web:$tag" >/dev/null
  docker image prune -f >/dev/null

  echo "Pushed tag $tag. Next: scripts/deploy.sh release dev $tag"
}

release() {
  local env="$1" tag="$2" group="rg-imova-$1" registry
  [[ "$env" == "dev" || "$env" == "prod" ]] || usage
  registry="$(az acr show --name "$ACR_NAME" --query loginServer --output tsv)"

  for image in imova-api imova-worker imova-web; do
    az acr repository show --name "$ACR_NAME" --image "$image:$tag" --output none \
      || { echo "$image:$tag isn't in the registry: run scripts/deploy.sh build first." >&2; exit 1; }
  done

  # The API first: it runs the database migrations the other two may depend on.
  for app in imova-api imova-worker imova-web; do
    echo "Updating $app in $env to $tag"
    az containerapp update --name "$app" --resource-group "$group" \
      --image "$registry/$app:$tag" --output none
  done

  echo "$env now runs $tag."
}

case "${1:-}" in
  build) build ;;
  release) [[ $# -eq 3 ]] || usage; release "$2" "$3" ;;
  *) usage ;;
esac
