variable "location" {
  description = "Azure region."
  type        = string
  default     = "westeurope"
}

variable "container_registry_name" {
  description = "Globally unique, 5-50 letters and digits. The environments look it up by this name."
  type        = string
}

variable "dns_zone_name" {
  description = "The public domain whose DNS Azure answers (nameservers set at the registrar, tophost.md)."
  type        = string
}
