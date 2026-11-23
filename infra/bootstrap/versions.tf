terraform {
  required_version = ">= 1.11"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
  }
}

# The subscription comes from ARM_SUBSCRIPTION_ID (see infra/README.md), never from a file.
provider "azurerm" {
  features {}
  # The state account has no access keys; data-plane calls use your Entra ID sign-in.
  storage_use_azuread = true
}
