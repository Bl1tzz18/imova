# Outgoing email for this environment (var.acs_email): its own Communication Service linked to the
# shared, verified email domain (infra/shared/email.tf), and SMTP credentials the apps use like any
# SMTP server. Azure Communication Services authenticates SMTP through an Entra application: the
# SMTP username points at the application, and the password is one of its client secrets, created
# here and written to Key Vault. Rotate it by bumping var.secrets_version.

resource "azurerm_communication_service" "main" {
  count = var.acs_email != null ? 1 : 0

  name                = "acs-${local.name}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "Europe"
  tags                = local.tags
}

resource "azurerm_communication_service_email_domain_association" "main" {
  count = var.acs_email != null ? 1 : 0

  communication_service_id = azurerm_communication_service.main[0].id
  email_service_domain_id  = var.acs_email.domain_id
}

resource "azuread_application_registration" "smtp" {
  count = var.acs_email != null ? 1 : 0

  display_name = "imova-${var.environment}-smtp"
  description  = "SMTP sign-in of the IMOVA ${var.environment} apps to Azure Communication Services (Terraform)."
}

resource "azuread_service_principal" "smtp" {
  count = var.acs_email != null ? 1 : 0

  client_id = azuread_application_registration.smtp[0].client_id
}

resource "azuread_application_password" "smtp" {
  count = var.acs_email != null ? 1 : 0

  application_id = azuread_application_registration.smtp[0].id
  display_name   = "smtp"
  # Two years; a new one is made earlier whenever secrets_version is bumped.
  end_date            = timeadd(time_rotating.smtp_password[0].id, "17520h")
  rotate_when_changed = { version = tostring(var.secrets_version) }
}

# Pins the password's start so its end date doesn't move on every plan.
resource "time_rotating" "smtp_password" {
  count = var.acs_email != null ? 1 : 0

  rotation_days = 700
}

# The smallest built-in role that lets the application send through this Communication Service.
resource "azurerm_role_assignment" "smtp" {
  count = var.acs_email != null ? 1 : 0

  scope                = azurerm_communication_service.main[0].id
  role_definition_name = "Communication and Email Service Owner"
  principal_id         = azuread_service_principal.smtp[0].object_id
  principal_type       = "ServicePrincipal"
}

locals {
  smtp_username = "imova-${var.environment}"
}

resource "azapi_resource" "smtp_username" {
  count = var.acs_email != null ? 1 : 0

  type = "Microsoft.Communication/communicationServices/smtpUsernames@2026-03-18"
  # Azure refuses a resource name equal to the username.
  name      = "smtp-${local.smtp_username}"
  parent_id = azurerm_communication_service.main[0].id

  body = {
    properties = {
      username           = local.smtp_username
      entraApplicationId = azuread_application_registration.smtp[0].client_id
      tenantId           = data.azurerm_client_config.current.tenant_id
    }
  }

  depends_on = [
    azurerm_role_assignment.smtp,
    azurerm_communication_service_email_domain_association.main,
  ]
}

resource "azurerm_key_vault_secret" "email_password" {
  count = var.acs_email != null ? 1 : 0

  name         = "email-password"
  key_vault_id = azurerm_key_vault.main.id
  value        = azuread_application_password.smtp[0].value
  content_type = "SMTP password (Entra application client secret)"

  depends_on = [time_sleep.key_vault_roles]
}
