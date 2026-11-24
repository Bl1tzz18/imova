variable "environment" {
  description = "Short environment name, used in every resource name: dev or prod."
  type        = string

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "environment must be dev or prod."
  }
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "polandcentral"
}

variable "tags" {
  description = "Extra tags for every resource."
  type        = map(string)
  default     = {}
}

# --- Images -------------------------------------------------------------------------------------

variable "container_registry_id" {
  description = "The shared registry's resource id (the apps get AcrPull on it)."
  type        = string
}

variable "container_registry_login_server" {
  description = "The shared registry's login server, e.g. acrimova.azurecr.io."
  type        = string
}

variable "image_tag" {
  description = <<-EOT
    Tag of the three images (imova-web, imova-api, imova-worker) the apps are CREATED with. Later
    deploys change the image with scripts/deploy.sh, and Terraform leaves it alone (ignore_changes).
  EOT
  type        = string
}

# --- Addresses ----------------------------------------------------------------------------------

variable "web_url" {
  description = "The site's public address (https://imova.md). Null = the web app's own *.azurecontainerapps.io address."
  type        = string
  default     = null
}

variable "api_url" {
  description = "The API's public address (https://api.imova.md). Null = the API app's own *.azurecontainerapps.io address."
  type        = string
  default     = null
}

# --- Sizes and scaling (the main cost levers) ----------------------------------------------------

variable "apps" {
  description = <<-EOT
    CPU/memory and replica counts per app. Allowed pairs: 0.25/0.5Gi, 0.5/1Gi, 0.75/1.5Gi, 1/2Gi.
    min_replicas = 0 scales the app to zero when idle (cheapest, but the first visitor waits for a
    cold start). The API stays at one replica until SignalR has a Redis backplane, and the Worker
    runs 0 or 1 (only one process may poll the Telegram bot).
  EOT
  type = object({
    web    = object({ cpu = number, memory = string, min_replicas = number, max_replicas = number })
    api    = object({ cpu = number, memory = string, min_replicas = number })
    worker = object({ cpu = number, memory = string, replicas = number })
  })

  validation {
    condition     = contains([0, 1], var.apps.worker.replicas)
    error_message = "The Worker runs 0 or 1 replica: two would both poll the Telegram bot and run every job twice."
  }

  validation {
    condition     = contains([0, 1], var.apps.api.min_replicas)
    error_message = "The API runs at most one replica (SignalR presence and rate limits are in memory), so min_replicas is 0 or 1."
  }
}

# --- Database -----------------------------------------------------------------------------------

variable "postgres" {
  description = "PostgreSQL Flexible Server settings. B_Standard_B1ms (1 vCore, 2 GiB) is the cheapest SKU."
  type = object({
    sku_name              = optional(string, "B_Standard_B1ms")
    storage_mb            = optional(number, 32768)
    storage_tier          = optional(string, "P4")
    backup_retention_days = optional(number, 7)
  })
  default = {}

  validation {
    condition     = var.postgres.backup_retention_days >= 1 && var.postgres.backup_retention_days <= 7
    error_message = "backup_retention_days must be 1-7: the privacy policy promises backups are kept at most 7 days."
  }
}

variable "postgres_admin_ip_ranges" {
  description = "Extra address ranges allowed to reach the database directly (e.g. your own IP for psql), name => [start, end]."
  type        = map(tuple([string, string]))
  default     = {}
}

variable "secrets_version" {
  description = <<-EOT
    The generated secrets (database password, JWT signing key) are write-only: never stored in the
    Terraform state. They're (re)generated only when this number changes; bump it to rotate both.
  EOT
  type        = number
  default     = 1
}

# --- Monitoring ---------------------------------------------------------------------------------

variable "log_daily_cap_gb" {
  description = "Daily ingestion cap for logs and telemetry, so a log loop can't run up the bill."
  type        = number
}

variable "alert_email" {
  description = "Where alerts go. Null = no alerts."
  type        = string
  default     = null
}

variable "availability_test_enabled" {
  description = "Uptime test of the home page every 15 minutes from 3 EU locations (~$5.60/month)."
  type        = bool
  default     = false
}

variable "error_alert_enabled" {
  description = "Email when an exception or error log appears (one log alert rule, ~$0.50/month)."
  type        = bool
  default     = false
}

# --- Optional integrations (each needs its secret set by hand in Key Vault; see infra/README.md) --

variable "google_auth_enabled" {
  description = "Google sign-in: creates the google-client-id/google-client-secret secrets (placeholders until set by hand)."
  type        = bool
  default     = false
}

variable "mediatr_license_enabled" {
  description = <<-EOT
    MediatR (Lucky Penny) needs a license in production: creates the mediatr-license-key secret
    (set by hand) and hands it to the API as MEDIATR_LICENSE_KEY, which MediatR reads by itself.
    Without it MediatR still works but logs a license warning (fine for dev).
  EOT
  type        = bool
  default     = false
}

variable "posthog" {
  description = <<-EOT
    Product analytics (PostHog, EU cloud): the project's public key (the web app, POSTHOG_KEY —
    the cookie banner and PostHog only appear with it) and its numeric project id (the API, to delete
    a person when an account is deleted, with the posthog-personal-api-key secret, set by hand).
    Null = no product analytics and no cookie banner.
  EOT
  type = object({
    project_key = string
    project_id  = string
  })
  default = null
}

variable "telegram_bot_username" {
  description = "The environment's own Telegram bot (one bot per environment). Null = no Telegram."
  type        = string
  default     = null
}

variable "acs_email" {
  description = <<-EOT
    Outgoing email through Azure Communication Services (infra/shared/email.tf): the shared,
    verified email domain and the sender. The environment gets its own Communication Service, an
    Entra application for SMTP and the password in Key Vault — nothing to set by hand.
    Null = emails are only logged.
  EOT
  type = object({
    domain_id    = string
    from_address = string
  })
  default = null
}

# --- Protection ---------------------------------------------------------------------------------

variable "protect_data" {
  description = "CanNotDelete locks on the database, storage and Key Vault, and purge protection on Key Vault (prod)."
  type        = bool
  default     = false
}

# --- Custom domains (KAN-5) ---------------------------------------------------------------------

variable "domains" {
  description = <<-EOT
    The environment's own domains, in the shared DNS zone (infra/shared). Null = only the apps'
    *.azurecontainerapps.io addresses. Names are relative to the zone, "@" = the zone itself.
    web: the first name is the site's address; any others (www) are bound too, and the web app
    redirects them to the first. Each name gets its record, its asuid TXT proof and a free managed
    certificate. web_url/api_url still have to be set to the new addresses separately — only once
    the certificates are bound.
  EOT
  type = object({
    zone_name           = string
    zone_resource_group = string
    web                 = list(string)
    api                 = string
  })
  default = null
}
