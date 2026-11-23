# The three apps in one Container Apps environment on the Consumption plan: billed per second of
# CPU/memory actually allocated, at a much lower "idle" rate while a replica serves nothing, with
# 180,000 vCPU-s, 360,000 GiB-s and 2 million requests free per month (per subscription).

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${local.name}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  logs_destination           = "log-analytics"

  # Consumption only: dedicated profiles add a management fee.
  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
  }

  tags = local.tags
}

locals {
  # An app's own address is <name>.<the environment's default domain>; computed from the
  # environment (not the apps), so web and API can point at each other without a cycle.
  web_app_name    = "imova-web"
  api_app_name    = "imova-api"
  worker_app_name = "imova-worker"

  web_url = coalesce(var.web_url, "https://${local.web_app_name}.${azurerm_container_app_environment.main.default_domain}")
  api_url = coalesce(var.api_url, "https://${local.api_app_name}.${azurerm_container_app_environment.main.default_domain}")

  image = {
    web    = "${var.container_registry_login_server}/imova-web:${var.image_tag}"
    api    = "${var.container_registry_login_server}/imova-api:${var.image_tag}"
    worker = "${var.container_registry_login_server}/imova-worker:${var.image_tag}"
  }

  # Container App secret name => Key Vault secret (always its latest version).
  kv_secret = merge(
    {
      "postgres-connection-string"    = azurerm_key_vault_secret.postgres_connection_string.versionless_id
      "jwt-signing-key"               = azurerm_key_vault_secret.jwt_key.versionless_id
      "storage-connection-string"     = azurerm_key_vault_secret.storage_connection_string.versionless_id
      "appinsights-connection-string" = azurerm_key_vault_secret.appinsights_connection_string.versionless_id
    },
    { for name, secret in azurerm_key_vault_secret.manual : name => secret.versionless_id },
    { for secret in azurerm_key_vault_secret.email_password : "email-password" => secret.versionless_id },
  )

  # Settings the API and the Worker share. value = plain setting, secret = a key of local.kv_secret.
  backend_env = concat(
    [
      { name = "ConnectionStrings__Default", secret = "postgres-connection-string" },
      { name = "Storage__ConnectionString", secret = "storage-connection-string" },
      { name = "Storage__AllowedOrigin", value = local.web_url },
      { name = "App__WebBaseUrl", value = local.web_url },
      { name = "APPLICATIONINSIGHTS_CONNECTION_STRING", secret = "appinsights-connection-string" },
      # EF Core logs every SQL command at Information; the console logs go to Log Analytics, where
      # that would eat the daily cap. Database timings are already in Application Insights (spans).
      { name = "Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command", value = "Warning" },
    ],
    var.telegram_bot_username == null ? [] : [
      { name = "Telegram__BotUsername", value = var.telegram_bot_username },
    ],
    var.acs_email == null ? [] : [
      # Azure Communication Services' SMTP endpoint, STARTTLS on 587 (modules/environment/email.tf).
      { name = "Email__Host", value = "smtp.azurecomm.net" },
      { name = "Email__Port", value = "587" },
      { name = "Email__EnableSsl", value = "true" },
      { name = "Email__Username", value = local.smtp_username },
      { name = "Email__FromAddress", value = var.acs_email.from_address },
      { name = "Email__Password", secret = "email-password" },
    ],
  )

  api_env = concat(local.backend_env,
    [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "Jwt__Key", secret = "jwt-signing-key" },
      { name = "Cors__AllowedOrigins__0", value = local.web_url },
      # The visitor's real IP behind the ingress (rate limits): trust private addresses, walk back
      # two X-Forwarded-For hops (ingress, then the web app). See AuthRateLimiting.ConfigureForwardedHeaders.
      { name = "ForwardedHeaders__KnownNetworks__0", value = "10.0.0.0/8" },
      { name = "ForwardedHeaders__KnownNetworks__1", value = "172.16.0.0/12" },
      { name = "ForwardedHeaders__KnownNetworks__2", value = "192.168.0.0/16" },
      { name = "ForwardedHeaders__KnownNetworks__3", value = "100.64.0.0/10" },
      { name = "ForwardedHeaders__ForwardLimit", value = "2" },
    ],
    var.google_auth_enabled ? [
      { name = "GoogleAuth__ClientId", secret = "google-client-id" },
      { name = "GoogleAuth__ClientSecret", secret = "google-client-secret" },
    ] : [],
    # Read by MediatR itself (LicenseAccessor), no code needed. Only the API uses MediatR.
    var.mediatr_license_enabled ? [
      { name = "MEDIATR_LICENSE_KEY", secret = "mediatr-license-key" },
    ] : [],
  )

  worker_env = concat(local.backend_env,
    [
      { name = "DOTNET_ENVIRONMENT", value = "Production" },
    ],
    var.telegram_bot_username == null ? [] : [
      { name = "Telegram__BotToken", secret = "telegram-bot-token" },
    ],
  )

  web_env = [
    # Server-side calls to the API (Server Components, actions, the session middleware): by app
    # name, inside the environment. The public address would resolve to the environment's public
    # IP from in here too, so the call would leave and come back in with Azure's outbound address,
    # and the API couldn't see the visitor's IP behind it (rate limits).
    { name = "API_URL", value = "http://${local.api_app_name}" },
    # The browser's calls (uploads, typeaheads, the messaging hub) — read at runtime, see
    # src/lib/api/publicConfig.ts, so the same image runs in every environment.
    { name = "PUBLIC_API_URL", value = local.api_url },
    { name = "SITE_URL", value = local.web_url },
    { name = "APPLICATIONINSIGHTS_CONNECTION_STRING", secret = "appinsights-connection-string" },
  ]

  # The secrets each app may read: exactly the ones its settings use.
  app_secrets = {
    for app, env in { web = local.web_env, api = local.api_env, worker = local.worker_env } :
    app => toset([for e in env : e.secret if try(e.secret, null) != null])
  }
}

# --- Web (Next.js) ------------------------------------------------------------------------------

resource "azurerm_container_app" "web" {
  name                         = local.web_app_name
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.apps.id]
  }

  registry {
    server   = var.container_registry_login_server
    identity = azurerm_user_assigned_identity.apps.id
  }

  dynamic "secret" {
    for_each = local.app_secrets.web
    content {
      name                = secret.value
      key_vault_secret_id = local.kv_secret[secret.value]
      identity            = azurerm_user_assigned_identity.apps.id
    }
  }

  ingress {
    external_enabled           = true
    target_port                = 3000
    transport                  = "auto"
    allow_insecure_connections = false

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.apps.web.min_replicas
    max_replicas = var.apps.web.max_replicas

    container {
      name   = "web"
      image  = local.image.web
      cpu    = var.apps.web.cpu
      memory = var.apps.web.memory

      dynamic "env" {
        for_each = local.web_env
        content {
          name        = env.value.name
          value       = try(env.value.value, null)
          secret_name = try(env.value.secret, null)
        }
      }
    }
  }

  tags = local.tags

  lifecycle {
    # Deploys change the image (scripts/deploy.sh); Terraform only sets it at creation.
    ignore_changes = [template[0].container[0].image]
  }

  depends_on = [azurerm_role_assignment.apps_acr_pull, time_sleep.key_vault_roles]
}

# --- API (ASP.NET Core) -------------------------------------------------------------------------

resource "azurerm_container_app" "api" {
  name                         = local.api_app_name
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.apps.id]
  }

  registry {
    server   = var.container_registry_login_server
    identity = azurerm_user_assigned_identity.apps.id
  }

  dynamic "secret" {
    for_each = local.app_secrets.api
    content {
      name                = secret.value
      key_vault_secret_id = local.kv_secret[secret.value]
      identity            = azurerm_user_assigned_identity.apps.id
    }
  }

  ingress {
    # Public: the browser calls it directly (uploads, typeaheads, the messaging hub's WebSocket).
    external_enabled           = true
    target_port                = 8080
    transport                  = "auto"
    allow_insecure_connections = false

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.apps.api.min_replicas
    # One replica until SignalR has a Redis backplane: presence, hub groups and rate limits are in memory.
    max_replicas = 1

    container {
      name   = "api"
      image  = local.image.api
      cpu    = var.apps.api.cpu
      memory = var.apps.api.memory

      dynamic "env" {
        for_each = local.api_env
        content {
          name        = env.value.name
          value       = try(env.value.value, null)
          secret_name = try(env.value.secret, null)
        }
      }

      # The API runs the EF migrations on startup; give it up to 3 minutes before liveness counts.
      startup_probe {
        transport               = "HTTP"
        port                    = 8080
        path                    = "/health"
        interval_seconds        = 5
        timeout                 = 3
        failure_count_threshold = 36
      }

      liveness_probe {
        transport               = "HTTP"
        port                    = 8080
        path                    = "/health"
        interval_seconds        = 30
        timeout                 = 5
        failure_count_threshold = 3
      }
    }
  }

  tags = local.tags

  lifecycle {
    ignore_changes = [template[0].container[0].image]
  }

  depends_on = [azurerm_role_assignment.apps_acr_pull, time_sleep.key_vault_roles]
}

# --- Worker (scheduled jobs + the Telegram bot) -------------------------------------------------

resource "azurerm_container_app" "worker" {
  name                         = local.worker_app_name
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.apps.id]
  }

  registry {
    server   = var.container_registry_login_server
    identity = azurerm_user_assigned_identity.apps.id
  }

  dynamic "secret" {
    for_each = local.app_secrets.worker
    content {
      name                = secret.value
      key_vault_secret_id = local.kv_secret[secret.value]
      identity            = azurerm_user_assigned_identity.apps.id
    }
  }

  # No ingress: nothing calls the Worker. Exactly 0 or 1 replica (validated in variables.tf).
  template {
    min_replicas = var.apps.worker.replicas
    max_replicas = max(var.apps.worker.replicas, 1)

    container {
      name   = "worker"
      image  = local.image.worker
      cpu    = var.apps.worker.cpu
      memory = var.apps.worker.memory

      dynamic "env" {
        for_each = local.worker_env
        content {
          name        = env.value.name
          value       = try(env.value.value, null)
          secret_name = try(env.value.secret, null)
        }
      }
    }
  }

  tags = local.tags

  lifecycle {
    ignore_changes = [template[0].container[0].image]
  }

  depends_on = [azurerm_role_assignment.apps_acr_pull, time_sleep.key_vault_roles]
}
