variable "location" {
  description = "Azure region."
  type        = string
  default     = "westeurope"
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
