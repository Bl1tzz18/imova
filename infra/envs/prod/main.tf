# Prod: after launch, one always-on replica of the web app and the API (no cold starts for visitors
# and search engines; an idle replica is billed at the much lower idle rate) and the Worker always
# on; until launch it sleeps (below). Error alerts and delete locks on the data either way. Uptime is watched by a free outside service, not Azure's
# paid availability test (infra/README.md, "Uptime").

data "azurerm_container_registry" "shared" {
  name                = var.container_registry_name
  resource_group_name = "rg-imova-shared"
}

data "azurerm_client_config" "current" {}

locals {
  # The shared email domain (infra/shared/email.tf); there's no data source for it.
  email_domain_id = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/rg-imova-shared/providers/Microsoft.Communication/emailServices/ecs-imova/domains/imova.md"
}

module "imova" {
  source = "../../modules/environment"

  environment = "prod"
  location    = "polandcentral"

  container_registry_id           = data.azurerm_container_registry.shared.id
  container_registry_login_server = data.azurerm_container_registry.shared.login_server
  image_tag                       = var.image_tag

  # imova.md (www redirects to it) and api.imova.md, in the shared zone, with managed certificates.
  domains = {
    zone_name           = "imova.md"
    zone_resource_group = "rg-imova-shared"
    web                 = ["@", "www"]
    api                 = "api"
  }

  # The site's and the API's public addresses (emails, CORS, canonical URLs, the browser's API
  # address). Set only once the certificates above were bound.
  web_url = "https://imova.md"
  api_url = "https://api.imova.md"

  # UNTIL LAUNCH: asleep — web and API scale to zero (a visit wakes them in a few seconds), the
  # Worker is off. On launch day: web min_replicas = 1, api min_replicas = 1, worker replicas = 1
  # (~$29/month more; infra/README.md, "Launch checklist").
  apps = {
    web    = { cpu = 0.25, memory = "0.5Gi", min_replicas = 0, max_replicas = 2 }
    api    = { cpu = 0.5, memory = "1Gi", min_replicas = 0 }
    worker = { cpu = 0.25, memory = "0.5Gi", replicas = 0 }
  }

  log_daily_cap_gb          = 0.5
  alert_email               = "catalinfilip47@gmail.com"
  availability_test_enabled = false # ~$5.60/month; a free outside monitor does it instead
  error_alert_enabled       = true

  # Outgoing email from no-reply@imova.md through Azure Communication Services (infra/shared/email.tf).
  acs_email = {
    domain_id    = local.email_domain_id
    from_address = "no-reply@imova.md"
  }

  # Community license, valid until 2027-11-23: renew it before then (infra/README.md).
  mediatr_license_enabled = true

  # KAN-8: prod's own Google OAuth client (JavaScript origin https://imova.md) and its own
  # Telegram bot — never dev's, only one process may poll a bot. The values are set by hand in Key
  # Vault (google-client-id, google-client-secret, telegram-bot-token; infra/README.md). The bot
  # only answers while the Worker runs (replicas = 1 from launch).
  google_auth_enabled   = true
  telegram_bot_username = "imova_md_bot"

  # Product analytics (PostHog EU) + the cookie banner. Uncomment with the project's values once the
  # PostHog project exists, then set posthog-personal-api-key in Key Vault (infra/README.md).
  # posthog = { project_key = "phc_…", project_id = "12345" }

  protect_data = true
}
