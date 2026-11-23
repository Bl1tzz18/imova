# What dev and prod share: one image registry (an image is built once and promoted from dev to
# prod unchanged). imova.md's DNS stays at the registrar (tophost.md, free): the few records the
# site needs are added there by hand (infra/README.md, "Custom domain").

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
