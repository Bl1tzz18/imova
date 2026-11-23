terraform {
  required_version = ">= 1.11"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.13"
    }
  }

  # The rest of the settings: infra/backend.hcl (terraform init -backend-config=../../backend.hcl).
  backend "azurerm" {
    key = "prod.tfstate"
  }
}

# The subscription comes from ARM_SUBSCRIPTION_ID (infra/README.md), never from a file.
provider "azurerm" {
  storage_use_azuread = true

  features {
    key_vault {
      # A destroyed vault stays recoverable for its retention period instead of being purged.
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }

    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}
