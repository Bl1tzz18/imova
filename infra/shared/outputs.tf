output "container_registry_login_server" {
  value = azurerm_container_registry.shared.login_server
}

output "dns_name_servers" {
  description = "Set these four at the registrar (tophost.md) to move imova.md's DNS to Azure."
  value       = azurerm_dns_zone.main.name_servers
}

output "email_domain_id" {
  description = "For the environments' email setting (acs_email.domain_id)."
  value       = azurerm_email_communication_service_domain.main.id
}

output "email_from_address" {
  value = "${azurerm_email_communication_service_domain_sender_username.no_reply.name}@${azurerm_email_communication_service_domain.main.name}"
}
