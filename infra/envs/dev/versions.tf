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
    # The SMTP application for outgoing email (modules/environment/email.tf).
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.10"
    }
    azapi = {
      source  = "azure/azapi"
      version = "~> 2.14"
    }
  }

  # The rest of the settings: infra/backend.hcl (terraform init -backend-config=../../backend.hcl).
  backend "azurerm" {
    key = "dev.tfstate"
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
      # Dev is destroyed after every working session, and Azure adds resources Terraform doesn't
      # know about (Application Insights' smart-detection action group) — let destroy remove them.
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Both sign in like azurerm: the Azure CLI locally, the same identity in CI later.
provider "azuread" {}

provider "azapi" {}
