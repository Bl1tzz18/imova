output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "web_url" {
  description = "The site's address."
  value       = local.web_url
}

output "api_url" {
  description = "The API's address."
  value       = local.api_url
}

output "container_app_names" {
  description = "For scripts/deploy.sh."
  value = {
    web    = azurerm_container_app.web.name
    api    = azurerm_container_app.api.name
    worker = azurerm_container_app.worker.name
  }
}

output "web_default_hostname" {
  description = "The web app's own hostname — the CNAME target for the custom domain (KAN-5)."
  value       = azurerm_container_app.web.ingress[0].fqdn
}

output "api_default_hostname" {
  value = azurerm_container_app.api.ingress[0].fqdn
}

output "static_ip_address" {
  description = "Target of the apex domain's A record (imova.md; KAN-5)."
  value       = azurerm_container_app_environment.main.static_ip_address
}

output "custom_domain_verification_id" {
  description = "Goes in the asuid.<host> TXT record when a custom domain is added (KAN-5)."
  value       = azurerm_container_app_environment.main.custom_domain_verification_id
  sensitive   = true
}

output "key_vault_name" {
  description = "Where the hand-set secrets go (az keyvault secret set --vault-name ...)."
  value       = azurerm_key_vault.main.name
}

output "postgres_fqdn" {
  value = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgres_server_name" {
  description = "For az postgres flexible-server stop/start (dev, to save money)."
  value       = azurerm_postgresql_flexible_server.main.name
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}

output "application_insights_name" {
  value = azurerm_application_insights.main.name
}
