variable "location" {
  description = "Azure region."
  type        = string
  default     = "polandcentral"
}

variable "container_registry_name" {
  description = "Globally unique, 5-50 letters and digits. The environments look it up by this name."
  type        = string
}

variable "container_registry_sku" {
  description = <<-EOT
    Standard is free for 12 months on the Azure free account (1 registry, 100 GB); afterwards it's
    ~$20/month, so switch to Basic (~$5/month) before the free year ends (infra/README.md).
  EOT
  type        = string
  default     = "Standard"

  validation {
    condition     = contains(["Basic", "Standard"], var.container_registry_sku)
    error_message = "Basic or Standard: Premium isn't needed."
  }
}

variable "dns_zone_name" {
  description = "The public domain whose DNS Azure answers (nameservers set at the registrar, tophost.md)."
  type        = string
}

variable "spf_includes" {
  description = <<-EOT
    Other senders allowed to send as @imova.md, added to the SPF record next to Azure's (e.g. the
    mailbox provider's "zoho.eu" once contact@imova.md exists). One SPF record per domain is allowed,
    so every sender goes in this one.
  EOT
  type        = list(string)
  default     = []
}

variable "apex_txt_extra" {
  description = "Other TXT values on imova.md itself (e.g. the mailbox provider's verification code)."
  type        = list(string)
  default     = []
}

variable "mx_records" {
  description = <<-EOT
    Where mail for @imova.md is delivered (the mailbox provider, e.g. Zoho's mx.zoho.eu / mx2 / mx3),
    preference => mail server. Empty = imova.md receives no mail.
  EOT
  type        = map(string)
  default     = {}
}

variable "txt_records" {
  description = "Other TXT records, name (relative to the zone) => value — e.g. the mailbox provider's DKIM key."
  type        = map(string)
  default     = {}
}
