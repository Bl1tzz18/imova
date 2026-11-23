output "container_registry_login_server" {
  value = azurerm_container_registry.shared.login_server
}

output "dns_name_servers" {
  description = "Set these four at the registrar (tophost.md) to move imova.md's DNS to Azure."
  value       = azurerm_dns_zone.main.name_servers
}
