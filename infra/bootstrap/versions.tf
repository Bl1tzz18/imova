terraform {
  required_version = ">= 1.11"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
  }

  # Created with local state (the account didn't exist yet), then moved into the account it
  # created: terraform init -migrate-state -backend-config=../backend.hcl. Rebuilding from nothing
  # (account gone): comment this block out, apply with local state, uncomment, migrate again.
  backend "azurerm" {
    key = "bootstrap.tfstate"
  }
}

# The subscription comes from ARM_SUBSCRIPTION_ID (see infra/README.md), never from a file.
provider "azurerm" {
  features {}
  # The state account has no access keys; data-plane calls use your Entra ID sign-in.
  storage_use_azuread = true
}
