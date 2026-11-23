# Every secret the apps read lives here; the apps reference it by name (always the latest version)
# through their managed identity. Three kinds:
# - generated, write-only (database password, JWT key): created from an ephemeral random value,
#   sent to Azure but never written to the Terraform state; rotated by bumping var.secrets_version;
# - derived (storage and Application Insights connection strings): kept in sync by Terraform;
# - set by hand (Google, Telegram, MediatR): created with a placeholder, then `az keyvault secret set`
#   (infra/README.md); Terraform never overwrites the real value.

resource "azurerm_key_vault" "main" {
  name                       = "kv-${local.name}-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  soft_delete_retention_days = var.protect_data ? 90 : 7
  purge_protection_enabled   = var.protect_data
  tags                       = local.tags
}

# Whoever runs Terraform writes the secrets; the apps only read them.
resource "azurerm_role_assignment" "deployer_secrets_officer" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "apps_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.apps.principal_id
  principal_type       = "ServicePrincipal"
}

# Role assignments take a minute to reach Key Vault; writing secrets sooner fails with a 403.
resource "time_sleep" "key_vault_roles" {
  create_duration = "60s"

  depends_on = [
    azurerm_role_assignment.deployer_secrets_officer,
    azurerm_role_assignment.apps_secrets_user,
  ]
}

# --- Generated, write-only ----------------------------------------------------------------------

ephemeral "random_password" "postgres_admin" {
  length  = 32
  special = false
}

ephemeral "random_password" "jwt_key" {
  length  = 64
  special = false
}

locals {
  postgres_admin_login = "imova_admin"

  # Ephemeral too (it contains the password): only ever passed to the write-only secret value.
  postgres_connection_string = join(";", [
    "Host=${azurerm_postgresql_flexible_server.main.fqdn}",
    "Port=5432",
    "Database=${azurerm_postgresql_flexible_server_database.imova.name}",
    "Username=${local.postgres_admin_login}",
    "Password=${ephemeral.random_password.postgres_admin.result}",
    "SSL Mode=VerifyFull",
  ])
}

resource "azurerm_key_vault_secret" "postgres_connection_string" {
  name             = "postgres-connection-string"
  key_vault_id     = azurerm_key_vault.main.id
  value_wo         = local.postgres_connection_string
  value_wo_version = var.secrets_version
  content_type     = "Npgsql connection string"

  depends_on = [time_sleep.key_vault_roles]
}

resource "azurerm_key_vault_secret" "jwt_key" {
  name             = "jwt-signing-key"
  key_vault_id     = azurerm_key_vault.main.id
  value_wo         = ephemeral.random_password.jwt_key.result
  value_wo_version = var.secrets_version
  content_type     = "Login token signing key"

  depends_on = [time_sleep.key_vault_roles]
}

# --- Derived ------------------------------------------------------------------------------------

resource "azurerm_key_vault_secret" "storage_connection_string" {
  name         = "storage-connection-string"
  key_vault_id = azurerm_key_vault.main.id
  value        = azurerm_storage_account.main.primary_connection_string
  content_type = "Blob storage connection string"

  depends_on = [time_sleep.key_vault_roles]
}

resource "azurerm_key_vault_secret" "appinsights_connection_string" {
  name         = "appinsights-connection-string"
  key_vault_id = azurerm_key_vault.main.id
  value        = azurerm_application_insights.main.connection_string
  content_type = "Application Insights connection string"

  depends_on = [time_sleep.key_vault_roles]
}

# --- Set by hand --------------------------------------------------------------------------------

locals {
  manual_secrets = toset(concat(
    var.google_auth_enabled ? ["google-client-id", "google-client-secret"] : [],
    var.mediatr_license_enabled ? ["mediatr-license-key"] : [],
    var.telegram_bot_username != null ? ["telegram-bot-token"] : [],
  ))
}

resource "azurerm_key_vault_secret" "manual" {
  for_each = local.manual_secrets

  name         = each.key
  key_vault_id = azurerm_key_vault.main.id
  value        = "set-me"
  content_type = "Set by hand: az keyvault secret set (infra/README.md)"

  lifecycle {
    # The real value is set outside Terraform and must survive every apply.
    ignore_changes = [value, content_type, tags]
  }

  depends_on = [time_sleep.key_vault_roles]
}

resource "azurerm_management_lock" "key_vault" {
  count = var.protect_data ? 1 : 0

  name       = "do-not-delete"
  scope      = azurerm_key_vault.main.id
  lock_level = "CanNotDelete"
  notes      = "Holds the database password and the login token signing key."
}
