variable "location" {
  description = "Azure region for the state storage account."
  type        = string
  default     = "polandcentral"
}

variable "storage_account_name" {
  description = "Globally unique name of the state storage account. Must match infra/backend.hcl."
  type        = string
  default     = "stimovatfstate"
}
