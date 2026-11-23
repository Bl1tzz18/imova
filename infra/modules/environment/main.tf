# One IMOVA environment (dev or prod): its own resource group, database, storage, Key Vault,
# monitoring and the three apps, in a Container Apps environment on the Consumption plan.
#
# Deliberately NOT here, to keep the cost minimal (see infra/README.md, "Costs"):
# - no virtual network: integrating one adds a standard load balancer + two static public IPs
#   (~$25/month per environment); the database is reached over TLS with a firewall instead;
# - no dedicated workload profiles, private endpoints, Redis or zone redundancy.

data "azurerm_client_config" "current" {}

locals {
  name = "imova-${var.environment}"

  tags = merge(var.tags, {
    app         = "imova"
    environment = var.environment
    managed_by  = "terraform"
  })
}

# Globally unique names (Key Vault, storage, database) need a suffix; it's kept in the state, so it
# never changes after the first apply.
resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

# The apps' one identity: pulls images and reads Key Vault secrets. No passwords for either.
resource "azurerm_user_assigned_identity" "apps" {
  name                = "id-${local.name}-apps"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.tags
}

resource "azurerm_role_assignment" "apps_acr_pull" {
  scope                = var.container_registry_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.apps.principal_id
  principal_type       = "ServicePrincipal"
}
