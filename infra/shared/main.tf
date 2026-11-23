# What dev and prod share: one image registry (an image is built once and promoted from dev to
# prod unchanged) and the imova.md DNS zone (prod uses imova.md, www and api; dev uses dev.imova.md
# and api.dev.imova.md, rewritten by Terraform every time dev is recreated).

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

# Standard while the free account's 12 months last (it's the tier they include), then Basic
# (~$5/month, 10 GB) — see var.container_registry_sku. No admin user: the apps pull with their
# managed identity, people push after `az acr login`.
resource "azurerm_container_registry" "shared" {
  name                = var.container_registry_name
  resource_group_name = azurerm_resource_group.shared.name
  location            = azurerm_resource_group.shared.location
  sku                 = var.container_registry_sku
  admin_enabled       = false
  tags                = local.tags
}

# ~$0.50/month for the whole domain. imova.md stays registered at tophost.md; only its nameservers
# point here. The records (site, www, api, dev, mail) are added with the custom domains (KAN-5)
# and the email provider (KAN-14).
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
