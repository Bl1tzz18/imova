# Outgoing email for every environment (confirmations, password resets, alerts) through Azure
# Communication Services Email, from no-reply@imova.md. Data stays in Europe. Microsoft is already
# the hosting provider in the privacy policy ("Furnizorul nostru de servicii de email").
#
# Shared: the email service and the verified imova.md domain. Each environment has its own
# Communication Service, linked to this domain, with its own SMTP credentials
# (modules/environment/email.tf).
#
# Default sending limits for a custom domain: 30 emails a minute, 100 an hour per subscription —
# raised on request once the domain has a sending history (infra/README.md, "Email").

resource "azurerm_email_communication_service" "main" {
  name                = "ecs-imova"
  resource_group_name = azurerm_resource_group.shared.name
  data_location       = "Europe"
  tags                = local.tags
}

resource "azurerm_email_communication_service_domain" "main" {
  name                             = var.dns_zone_name
  email_service_id                 = azurerm_email_communication_service.main.id
  domain_management                = "CustomerManaged"
  user_engagement_tracking_enabled = false
  tags                             = local.tags
}

resource "azurerm_email_communication_service_domain_sender_username" "no_reply" {
  name                    = "no-reply"
  email_service_domain_id = azurerm_email_communication_service_domain.main.id
  display_name            = "IMOVA"
}

locals {
  email_records = azurerm_email_communication_service_domain.main.verification_records[0]

  # Azure's SPF ("v=spf1 include:spf.protection.outlook.com -all") with any other sender added
  # before the final -all.
  spf = replace(
    local.email_records.spf[0].value,
    " -all",
    "${join("", [for include in var.spf_includes : " include:${include}"])} -all",
  )
}

# Every TXT value on the zone apex lives in this one record set: Azure's domain proof, the SPF,
# and anything else (a mailbox provider's verification).
resource "azurerm_dns_txt_record" "apex" {
  name                = "@"
  zone_name           = azurerm_dns_zone.main.name
  resource_group_name = azurerm_resource_group.shared.name
  ttl                 = 3600

  dynamic "record" {
    for_each = concat([local.email_records.domain[0].value, local.spf], var.apex_txt_extra)
    content {
      value = record.value
    }
  }

  tags = local.tags
}

resource "azurerm_dns_cname_record" "dkim" {
  name                = local.email_records.dkim[0].name
  zone_name           = azurerm_dns_zone.main.name
  resource_group_name = azurerm_resource_group.shared.name
  ttl                 = 3600
  record              = local.email_records.dkim[0].value
  tags                = local.tags
}

resource "azurerm_dns_cname_record" "dkim2" {
  name                = local.email_records.dkim2[0].name
  zone_name           = azurerm_dns_zone.main.name
  resource_group_name = azurerm_resource_group.shared.name
  ttl                 = 3600
  record              = local.email_records.dkim2[0].value
  tags                = local.tags
}

# Receivers may check it; "none" = only report, never reject, while the setup settles.
resource "azurerm_dns_txt_record" "dmarc" {
  name                = "_dmarc"
  zone_name           = azurerm_dns_zone.main.name
  resource_group_name = azurerm_resource_group.shared.name
  ttl                 = 3600

  record {
    value = "v=DMARC1; p=none; adkim=r; aspf=r"
  }

  tags = local.tags
}

# Asks Azure to check each record now that it exists. The check itself runs asynchronously (a few
# minutes); its result shows in the portal and in `az communication email domain show`.
resource "azapi_resource_action" "verify_email_domain" {
  for_each = toset(["Domain", "SPF", "DKIM", "DKIM2"])

  type        = "Microsoft.Communication/emailServices/domains@2026-03-18"
  resource_id = azurerm_email_communication_service_domain.main.id
  action      = "initiateVerification"
  method      = "POST"
  body        = { verificationType = each.key }

  depends_on = [
    azurerm_dns_txt_record.apex,
    azurerm_dns_cname_record.dkim,
    azurerm_dns_cname_record.dkim2,
  ]
}

# Incoming mail (contact@imova.md) — the mailbox provider's servers, once it's set up.
resource "azurerm_dns_mx_record" "apex" {
  count = length(var.mx_records) > 0 ? 1 : 0

  name                = "@"
  zone_name           = azurerm_dns_zone.main.name
  resource_group_name = azurerm_resource_group.shared.name
  ttl                 = 3600

  dynamic "record" {
    for_each = var.mx_records
    content {
      preference = tonumber(record.key)
      exchange   = record.value
    }
  }

  tags = local.tags
}
