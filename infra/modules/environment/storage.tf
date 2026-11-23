# Listing photos (public read: they're shown straight from storage) and message images (private,
# served only through the API). The browser uploads straight here with a SAS URL, so the account's
# CORS allows the site's origin. The API and the Worker also write this rule at startup
# (BlobStorageService, from Storage__AllowedOrigin): keep the two identical (same origin, methods,
# headers, max age), or they'd keep overwriting each other.

resource "azurerm_storage_account" "main" {
  name                     = "stimova${var.environment}${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"

  min_tls_version                  = "TLS1_2"
  https_traffic_only_enabled       = true
  cross_tenant_replication_enabled = false
  # The listing-images container is public read, which the account must allow.
  allow_nested_items_to_be_public = true
  # The apps sign upload URLs with the account key (Storage:ConnectionString).
  shared_access_key_enabled = true

  blob_properties {
    # A deleted photo can be restored for 7 days (cheap insurance; full backups are KAN-15).
    delete_retention_policy {
      days = 7
    }

    container_delete_retention_policy {
      days = 7
    }

    cors_rule {
      allowed_origins    = [local.web_url]
      allowed_methods    = ["GET", "HEAD", "PUT", "OPTIONS"]
      allowed_headers    = ["*"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }

  tags = local.tags
}

resource "azurerm_storage_container" "listing_images" {
  name                  = "listing-images"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "blob"
}

resource "azurerm_storage_container" "message_attachments" {
  name                  = "message-attachments"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

resource "azurerm_management_lock" "storage" {
  count = var.protect_data ? 1 : 0

  name       = "do-not-delete"
  scope      = azurerm_storage_account.main.id
  lock_level = "CanNotDelete"
  notes      = "Every listing photo and message image."
}
