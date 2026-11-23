# PostgreSQL 16 (the version compose runs) with PostGIS, on the cheapest SKU (Burstable B1ms).
# Public endpoint on purpose (a private one needs the VNet, see main.tf): TLS is required by the
# server and verified by the apps (SSL Mode=VerifyFull), the password is 32 random characters, and
# the firewall only lets in Azure services plus the ranges in var.postgres_admin_ip_ranges.

resource "azurerm_postgresql_flexible_server" "main" {
  name                = "psql-${local.name}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  version             = "16"

  sku_name     = var.postgres.sku_name
  storage_mb   = var.postgres.storage_mb
  storage_tier = var.postgres.storage_tier
  # Grows the disk instead of stopping writes when it fills up.
  auto_grow_enabled = true

  # The privacy policy promises backups are kept at most 7 days (validated in variables.tf).
  backup_retention_days        = var.postgres.backup_retention_days
  geo_redundant_backup_enabled = false

  public_network_access_enabled = true

  administrator_login               = local.postgres_admin_login
  administrator_password_wo         = ephemeral.random_password.postgres_admin.result
  administrator_password_wo_version = var.secrets_version

  authentication {
    password_auth_enabled         = true
    active_directory_auth_enabled = false
  }

  # Sunday 03:00 UTC (05:00/06:00 in Chișinău), the quietest hour.
  maintenance_window {
    day_of_week  = 0
    start_hour   = 3
    start_minute = 0
  }

  tags = local.tags

  lifecycle {
    # Azure picks an availability zone at creation; it can't be changed afterwards anyway.
    ignore_changes = [zone]
  }
}

# Extensions must be allow-listed before CREATE EXTENSION works; the API's migrations create PostGIS.
resource "azurerm_postgresql_flexible_server_configuration" "extensions" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.main.id
  value     = "POSTGIS"
}

resource "azurerm_postgresql_flexible_server_database" "imova" {
  name      = "imova"
  server_id = azurerm_postgresql_flexible_server.main.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# 0.0.0.0 is Azure's "allow Azure services" rule: the Container Apps have no fixed outbound
# address without a VNet.
resource "azurerm_postgresql_flexible_server_firewall_rule" "azure_services" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "admin" {
  for_each = var.postgres_admin_ip_ranges

  name             = each.key
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = each.value[0]
  end_ip_address   = each.value[1]
}

resource "azurerm_management_lock" "postgres" {
  count = var.protect_data ? 1 : 0

  name       = "do-not-delete"
  scope      = azurerm_postgresql_flexible_server.main.id
  lock_level = "CanNotDelete"
  notes      = "The production database."
}
