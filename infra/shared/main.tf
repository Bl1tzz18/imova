# What dev and prod share: one image registry (an image is built once and promoted from dev to
# prod unchanged) and the imova.md DNS zone (prod uses the apex and www, dev a subdomain).

locals {
  tags = {
    app        = "imova"
    managed_by = "terraform"
    stack      = "shared"
  }
}

resource "azurerm_resource_group" "shared" {
  name     = "rg-imova-shared"
  location = var.location
  tags     = local.tags
}

# Basic: the cheapest tier (~$5/month, 10 GB included). No admin user: the apps pull with their
# managed identity, people push after `az acr login`.
resource "azurerm_container_registry" "shared" {
  name                = var.container_registry_name
  resource_group_name = azurerm_resource_group.shared.name
  location            = azurerm_resource_group.shared.location
  sku                 = "Basic"
  admin_enabled       = false
  tags                = local.tags
}

# ~$0.50/month. The records (site, www, mail) are added with the custom domains (KAN-5) and the
# email provider (KAN-14).
resource "azurerm_dns_zone" "main" {
  name                = var.dns_zone_name
  resource_group_name = azurerm_resource_group.shared.name
  tags                = local.tags
}

resource "azurerm_management_lock" "dns" {
  name       = "do-not-delete"
  scope      = azurerm_dns_zone.main.id
  lock_level = "CanNotDelete"
  notes      = "Deleting the zone takes the site and its email offline."
}
