# Custom domains with free managed certificates (KAN-5). For each name:
#   1. DNS: an A record (the zone apex, to the environment's static IP) or a CNAME (to the app's own
#      hostname), plus the asuid.<name> TXT record that proves the domain is ours;
#   2. the name is added to the app, without a certificate yet;
#   3. Container Apps issues a managed certificate (HTTP validation for the apex, CNAME otherwise)
#      and renews it by itself;
#   4. the certificate is bound to the name. The provider has no resource for this step, so it's an
#      `az containerapp hostname bind` (needs the Azure CLI, signed in, wherever Terraform runs).

locals {
  domain_hosts = var.domains == null ? {} : merge(
    { for name in var.domains.web : name => { app = "web" } },
    { (var.domains.api) = { app = "api" } },
  )

  domains = {
    for name, host in local.domain_hosts : (name == "@" ? var.domains.zone_name : "${name}.${var.domains.zone_name}") => {
      record_name = name
      asuid_name  = name == "@" ? "asuid" : "asuid.${name}"
      apex        = name == "@"
      app         = host.app
    }
  }

  domains_by_app = {
    for app in distinct([for d in values(local.domains) : d.app]) :
    app => sort([for fqdn, d in local.domains : fqdn if d.app == app])
  }

  domain_apps = {
    web = azurerm_container_app.web
    api = azurerm_container_app.api
  }
}

resource "azurerm_dns_a_record" "apex" {
  for_each = { for fqdn, d in local.domains : fqdn => d if d.apex }

  name                = "@"
  zone_name           = var.domains.zone_name
  resource_group_name = var.domains.zone_resource_group
  ttl                 = 3600
  records             = [azurerm_container_app_environment.main.static_ip_address]
  tags                = local.tags
}

resource "azurerm_dns_cname_record" "host" {
  for_each = { for fqdn, d in local.domains : fqdn => d if !d.apex }

  name                = each.value.record_name
  zone_name           = var.domains.zone_name
  resource_group_name = var.domains.zone_resource_group
  ttl                 = 3600
  record              = local.domain_apps[each.value.app].ingress[0].fqdn
  tags                = local.tags
}

resource "azurerm_dns_txt_record" "asuid" {
  for_each = local.domains

  name                = each.value.asuid_name
  zone_name           = var.domains.zone_name
  resource_group_name = var.domains.zone_resource_group
  ttl                 = 3600

  record {
    value = local.domain_apps[each.value.app].custom_domain_verification_id
  }

  tags = local.tags
}

resource "azurerm_container_app_custom_domain" "host" {
  for_each = local.domains

  name             = each.key
  container_app_id = local.domain_apps[each.value.app].id

  lifecycle {
    # Bound to its managed certificate by terraform_data.bind_certificates below, not here.
    ignore_changes = [certificate_binding_type, container_app_environment_certificate_id]
  }

  depends_on = [
    azurerm_dns_a_record.apex,
    azurerm_dns_cname_record.host,
    azurerm_dns_txt_record.asuid,
  ]
}

resource "azurerm_container_app_environment_managed_certificate" "host" {
  for_each = local.domains

  name                         = "mc-${replace(each.key, ".", "-")}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  subject_name                 = each.key
  # The apex can't have a CNAME, so it's validated over HTTP (through its A record).
  domain_control_validation = each.value.apex ? "HTTP" : "CNAME"
  tags                      = local.tags

  depends_on = [azurerm_container_app_custom_domain.host]
}

# One per app, binding its names one after the other: two binds on the same app at once make
# Azure cancel one ("Operation failed or canceled").
resource "terraform_data" "bind_certificates" {
  for_each = local.domains_by_app

  triggers_replace = flatten([
    for fqdn in each.value : [
      azurerm_container_app_environment_managed_certificate.host[fqdn].id,
      azurerm_container_app_custom_domain.host[fqdn].id,
    ]
  ])

  provisioner "local-exec" {
    command = join(" && ", [
      for fqdn in each.value : join(" ", [
        "az containerapp hostname bind",
        "--resource-group ${azurerm_resource_group.main.name}",
        "--name ${local.domain_apps[each.key].name}",
        "--hostname ${fqdn}",
        "--environment ${azurerm_container_app_environment.main.name}",
        "--certificate ${azurerm_container_app_environment_managed_certificate.host[fqdn].id}",
        "--output none",
      ])
    ])
  }
}
