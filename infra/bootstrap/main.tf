# Run once per subscription, with local state (this stack can't keep its state in the account it
# creates before it exists). Everything else (shared/, envs/*) stores its state here.

data "azurerm_client_config" "current" {}

locals {
  tags = {
    app        = "imova"
    managed_by = "terraform"
    stack      = "bootstrap"
  }
}

resource "azurerm_resource_group" "tfstate" {
  name     = "rg-imova-tfstate"
  location = var.location
  tags     = local.tags
}

resource "azurerm_storage_account" "tfstate" {
  name                            = var.storage_account_name
  resource_group_name             = azurerm_resource_group.tfstate.name
  location                        = azurerm_resource_group.tfstate.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = false
  # Entra ID only: no account keys that could leak and unlock every state file.
  shared_access_key_enabled        = false
  default_to_oauth_authentication  = true
  cross_tenant_replication_enabled = false

  blob_properties {
    # Every state write keeps the previous version — a broken apply can be rolled back by hand.
    versioning_enabled = true

    delete_retention_policy {
      days = 30
    }

    container_delete_retention_policy {
      days = 30
    }
  }

  tags = local.tags
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}

# Whoever runs bootstrap may read and write state through Entra ID.
resource "azurerm_role_assignment" "state_writer" {
  scope                = azurerm_storage_account.tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}

# Nobody deletes the state account by accident.
resource "azurerm_management_lock" "tfstate" {
  name       = "do-not-delete"
  scope      = azurerm_storage_account.tfstate.id
  lock_level = "CanNotDelete"
  notes      = "Holds the Terraform state of every IMOVA environment."
}
