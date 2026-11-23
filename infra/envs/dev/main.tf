# Dev exists only while you're working on it: `terraform apply` to create it, `terraform destroy`
# when done; it costs nothing in between (infra/README.md, "Dev on demand"). Its data is disposable.
# While up: web and API scale to zero when idle, the Worker is off (set apps.worker.replicas = 1 to
# try alerts, expiry or the Telegram bot), no alerts.

data "azurerm_container_registry" "shared" {
  name                = var.container_registry_name
  resource_group_name = "rg-imova-shared"
}

module "imova" {
  source = "../../modules/environment"

  environment = "dev"
  location    = "polandcentral"

  container_registry_id           = data.azurerm_container_registry.shared.id
  container_registry_login_server = data.azurerm_container_registry.shared.login_server
  image_tag                       = var.image_tag

  apps = {
    web    = { cpu = 0.25, memory = "0.5Gi", min_replicas = 0, max_replicas = 1 }
    api    = { cpu = 0.5, memory = "1Gi", min_replicas = 0 }
    worker = { cpu = 0.25, memory = "0.5Gi", replicas = 0 }
  }

  log_daily_cap_gb = 0.1

  protect_data = false
}
