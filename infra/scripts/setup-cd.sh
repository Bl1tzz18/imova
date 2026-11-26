#!/usr/bin/env bash
# One-time setup for the GitHub "Deploy" workflow (.github/workflows/deploy.yml). Run it once,
# signed in to Azure as the subscription's owner (az login) and to GitHub (gh auth login):
#
#   infra/scripts/setup-cd.sh
#
# It creates (or finds again — running it twice is harmless):
# - an Entra application "imova-github-deploy" with no password: GitHub signs in with a short-lived
#   OIDC token, and only a run of this repo in the "production" environment is accepted;
# - its rights: Owner of rg-imova-prod and rg-imova-shared (Terraform manages role assignments and
#   delete locks there), the state storage's blob data, and in Microsoft Graph only the
#   applications it owns — it is made owner of imova-prod-smtp, which Terraform manages;
# - the GitHub environment "production" (deployments from main only) with the three IDs the
#   workflow needs, as variables — none of them is a secret.
# At the end it prints the identity's object id: it goes in envs/prod/main.tf and envs/dev/main.tf
# (key_vault_secret_officers.github), then one `terraform apply` from your machine gives it the
# Key Vault role — after that, every deploy is the button.
set -euo pipefail

REPO="${REPO:-Bl1tzz18/imova}"
APP_NAME="imova-github-deploy"
GRAPH_APP_ID="00000003-0000-0000-c000-000000000000" # Microsoft Graph
GH="${GH:-gh}"
command -v "$GH" >/dev/null || GH="$HOME/.local/bin/gh"

SUBSCRIPTION_ID="$(az account show --query id -o tsv)"
TENANT_ID="$(az account show --query tenantId -o tsv)"
echo "Subscription $SUBSCRIPTION_ID, tenant $TENANT_ID, repo $REPO"

# --- The application and its service principal ---------------------------------------------------
APP_ID="$(az ad app list --display-name "$APP_NAME" --query '[0].appId' -o tsv)"
if [[ -z "$APP_ID" ]]; then
  APP_ID="$(az ad app create --display-name "$APP_NAME" --sign-in-audience AzureADMyOrg --query appId -o tsv)"
  echo "Created application $APP_NAME ($APP_ID)"
fi
APP_OBJECT_ID="$(az ad app show --id "$APP_ID" --query id -o tsv)"
SP_ID="$(az ad sp list --filter "appId eq '$APP_ID'" --query '[0].id' -o tsv)"
if [[ -z "$SP_ID" ]]; then
  SP_ID="$(az ad sp create --id "$APP_ID" --query id -o tsv)"
fi

# Only GitHub Actions runs of this repo in the "production" environment can sign in as it.
SUBJECT="repo:$REPO:environment:production"
if ! az ad app federated-credential list --id "$APP_OBJECT_ID" --query "[?subject=='$SUBJECT'].name" -o tsv | grep -q .; then
  az ad app federated-credential create --id "$APP_OBJECT_ID" --parameters "{
    \"name\": \"github-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"$SUBJECT\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" --output none
  echo "Trusts $SUBJECT"
fi

# --- Azure roles ---------------------------------------------------------------------------------
assign() { # role scope
  if [[ -z "$(az role assignment list --assignee "$SP_ID" --role "$1" --scope "$2" --query '[0].id' -o tsv)" ]]; then
    az role assignment create --assignee-object-id "$SP_ID" --assignee-principal-type ServicePrincipal \
      --role "$1" --scope "$2" --output none
    echo "$1 on ${2##*/}"
  fi
}
RG="/subscriptions/$SUBSCRIPTION_ID/resourceGroups"
assign Owner "$RG/rg-imova-prod"
assign Owner "$RG/rg-imova-shared"
assign "Storage Blob Data Contributor" "$RG/rg-imova-tfstate/providers/Microsoft.Storage/storageAccounts/stimovatfstate"

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

# --- GitHub: the "production" environment, main only, with the IDs -------------------------------
"$GH" api --method PUT "repos/$REPO/environments/production" --input - >/dev/null <<'JSON'
{ "deployment_branch_policy": { "protected_branches": false, "custom_branch_policies": true } }
JSON
if ! "$GH" api "repos/$REPO/environments/production/deployment-branch-policies" --jq '.branch_policies[].name' | grep -qx main; then
  "$GH" api --method POST "repos/$REPO/environments/production/deployment-branch-policies" -f name=main -f type=branch >/dev/null
fi
"$GH" variable set AZURE_CLIENT_ID --env production --repo "$REPO" --body "$APP_ID"
"$GH" variable set AZURE_TENANT_ID --env production --repo "$REPO" --body "$TENANT_ID"
"$GH" variable set AZURE_SUBSCRIPTION_ID --env production --repo "$REPO" --body "$SUBSCRIPTION_ID"

echo
echo "Done. The deploy identity's object id (key_vault_secret_officers.github):"
echo "  $SP_ID"
