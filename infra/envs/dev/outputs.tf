output "resource_group_name" {
  value = module.imova.resource_group_name
}

output "web_url" {
  value = module.imova.web_url
}

output "api_url" {
  value = module.imova.api_url
}

output "container_app_names" {
  value = module.imova.container_app_names
}

output "web_default_hostname" {
  value = module.imova.web_default_hostname
}

output "api_default_hostname" {
  value = module.imova.api_default_hostname
}

output "static_ip_address" {
  value = module.imova.static_ip_address
}

output "custom_domain_verification_id" {
  value     = module.imova.custom_domain_verification_id
  sensitive = true
}

output "key_vault_name" {
  value = module.imova.key_vault_name
}

output "postgres_fqdn" {
  value = module.imova.postgres_fqdn
}

output "postgres_server_name" {
  value = module.imova.postgres_server_name
}

output "storage_account_name" {
  value = module.imova.storage_account_name
}
