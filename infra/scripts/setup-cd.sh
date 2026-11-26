#!/usr/bin/env bash
# One-time setup for the CD half of the GitHub "CI/CD" workflow (.github/workflows/ci.yml). Run it once,
# signed in to Azure as the subscription's owner (az login) and to GitHub (gh auth login):
#
#   infra/scripts/setup-cd.sh
#
# It creates (or finds again — running it twice is harmless) the Entra application
# "imova-github-deploy" with no password: GitHub signs in with a short-lived OIDC token, accepted
# only for this repo's runs in the GitHub environment "production" (which waits for your approval).
# Its rights: Owner of rg-imova-prod and rg-imova-shared (Terraform manages role assignments and
# delete locks there; it also pushes the images to the registry), the state storage's blob data,
# and in Microsoft Graph only the applications it owns — it is made owner of imova-prod-smtp, which
# Terraform manages. And the GitHub environment "production" (deployments from main only, you as
# the required reviewer) with the three IDs the workflow needs as variables (none is a secret).
# At the end it prints the deploy identity's object id: it goes in envs/prod/main.tf and
# envs/dev/main.tf (key_vault_secret_officers.github), then one `terraform apply` from your machine
# gives it the Key Vault role — after that, every deploy is a merge plus an Approve.
set -euo pipefail

REPO="${REPO:-Bl1tzz18/imova}"
APP_NAME="imova-github-deploy"
GRAPH_APP_ID="00000003-0000-0000-c000-000000000000" # Microsoft Graph
GH="${GH:-gh}"
command -v "$GH" >/dev/null || GH="$HOME/.local/bin/gh"

SUBSCRIPTION_ID="$(az account show --query id -o tsv)"
TENANT_ID="$(az account show --query tenantId -o tsv)"
# "owner@<owner id>/repo@<repo id>", as GitHub's OIDC token may name the repo.
REPO_WITH_IDS="$("$GH" api "repos/$REPO" --jq '"\(.owner.login)@\(.owner.id)/\(.name)@\(.id)"')"
echo "Subscription $SUBSCRIPTION_ID, tenant $TENANT_ID, repo $REPO ($REPO_WITH_IDS)"

# --- The applications, their service principals and who may sign in as them ----------------------
# identity <display-name> <github-environment> → sets APP_ID and SP_ID
identity() {
  APP_ID="$(az ad app list --display-name "$1" --query '[0].appId' -o tsv)"
  if [[ -z "$APP_ID" ]]; then
    APP_ID="$(az ad app create --display-name "$1" --sign-in-audience AzureADMyOrg --query appId -o tsv)"
    echo "Created application $1 ($APP_ID)"
  fi
  local app_object_id subject
  app_object_id="$(az ad app show --id "$APP_ID" --query id -o tsv)"
  SP_ID="$(az ad sp list --filter "appId eq '$APP_ID'" --query '[0].id' -o tsv)"
  if [[ -z "$SP_ID" ]]; then
    SP_ID="$(az ad sp create --id "$APP_ID" --query id -o tsv)"
  fi
  # Only GitHub Actions runs of this repo in that environment can sign in as it. GitHub names the
  # repo in the token either by name ("repo:owner/name:…") or, for newer repos, with the owner's
  # and the repo's numeric ids too ("repo:owner@123/name@456:…" — this one does); both are trusted.
  local name subject
  for name in "github-$2" "github-$2-ids"; do
    if [[ "$name" == *-ids ]]; then subject="repo:$REPO_WITH_IDS:environment:$2"; else subject="repo:$REPO:environment:$2"; fi
    if ! az ad app federated-credential list --id "$app_object_id" --query "[?subject=='$subject'].name" -o tsv | grep -q .; then
      az ad app federated-credential create --id "$app_object_id" --parameters "{
        \"name\": \"$name\",
        \"issuer\": \"https://token.actions.githubusercontent.com\",
        \"subject\": \"$subject\",
        \"audiences\": [\"api://AzureADTokenExchange\"]
      }" --output none
      echo "$1 trusts $subject"
    fi
  done
}

identity "$APP_NAME" production

# --- Azure roles ---------------------------------------------------------------------------------
assign() { # principal role scope
  if [[ -z "$(az role assignment list --assignee "$1" --role "$2" --scope "$3" --query '[0].id' -o tsv)" ]]; then
    az role assignment create --assignee-object-id "$1" --assignee-principal-type ServicePrincipal \
      --role "$2" --scope "$3" --output none
    echo "$2 on ${3##*/}"
  fi
}
RG="/subscriptions/$SUBSCRIPTION_ID/resourceGroups"
assign "$SP_ID" Owner "$RG/rg-imova-prod"
assign "$SP_ID" Owner "$RG/rg-imova-shared"
assign "$SP_ID" "Storage Blob Data Contributor" "$RG/rg-imova-tfstate/providers/Microsoft.Storage/storageAccounts/stimovatfstate"

# --- Microsoft Graph: manage only the applications it owns (imova-prod-smtp) ----------------------
OWNED_BY="$(az ad sp show --id "$GRAPH_APP_ID" --query "appRoles[?value=='Application.ReadWrite.OwnedBy'].id | [0]" -o tsv)"
GRAPH_SP_ID="$(az ad sp show --id "$GRAPH_APP_ID" --query id -o tsv)"
if [[ -z "$(az rest --method get --url "https://graph.microsoft.com/v1.0/servicePrincipals/$SP_ID/appRoleAssignments" \
  --query "value[?appRoleId=='$OWNED_BY'].id | [0]" -o tsv)" ]]; then
  az rest --method post --url "https://graph.microsoft.com/v1.0/servicePrincipals/$SP_ID/appRoleAssignments" \
    --body "{\"principalId\": \"$SP_ID\", \"resourceId\": \"$GRAPH_SP_ID\", \"appRoleId\": \"$OWNED_BY\"}" --output none
  echo "Graph: Application.ReadWrite.OwnedBy"
fi

SMTP_APP_ID="$(az ad app list --display-name imova-prod-smtp --query '[0].id' -o tsv)"
SMTP_SP_ID="$(az ad sp list --display-name imova-prod-smtp --query '[0].id' -o tsv)"
add_owner() { # objects-path object-id
  if ! az rest --method get --url "https://graph.microsoft.com/v1.0/$1/$2/owners" --query "value[].id" -o tsv | grep -q "$SP_ID"; then
    az rest --method post --url "https://graph.microsoft.com/v1.0/$1/$2/owners/\$ref" \
      --body "{\"@odata.id\": \"https://graph.microsoft.com/v1.0/directoryObjects/$SP_ID\"}" --output none
    echo "Owner of imova-prod-smtp ($1)"
  fi
}
add_owner applications "$SMTP_APP_ID"
add_owner servicePrincipals "$SMTP_SP_ID"

# --- GitHub: the production environment, main only, with the IDs; it waits for you ---------------
REVIEWER_ID="$("$GH" api user --jq .id)"
environment() { # name app-id protection-json
  "$GH" api --method PUT "repos/$REPO/environments/$1" --input - >/dev/null <<<"$3"
  if ! "$GH" api "repos/$REPO/environments/$1/deployment-branch-policies" --jq '.branch_policies[].name' | grep -qx main; then
    "$GH" api --method POST "repos/$REPO/environments/$1/deployment-branch-policies" -f name=main -f type=branch >/dev/null
  fi
  "$GH" variable set AZURE_CLIENT_ID --env "$1" --repo "$REPO" --body "$2"
  "$GH" variable set AZURE_TENANT_ID --env "$1" --repo "$REPO" --body "$TENANT_ID"
  "$GH" variable set AZURE_SUBSCRIPTION_ID --env "$1" --repo "$REPO" --body "$SUBSCRIPTION_ID"
}
BRANCHES='"deployment_branch_policy": { "protected_branches": false, "custom_branch_policies": true }'
# You approve your own deploys, so self-review must stay allowed.
environment production "$APP_ID" "{ $BRANCHES, \"prevent_self_review\": false,
  \"reviewers\": [{ \"type\": \"User\", \"id\": $REVIEWER_ID }] }"

echo
echo "Done. The deploy identity's object id (key_vault_secret_officers.github):"
echo "  $SP_ID"
