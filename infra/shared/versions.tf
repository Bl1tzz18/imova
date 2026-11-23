terraform {
  required_version = ">= 1.11"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
  }

  # The rest of the settings: infra/backend.hcl (terraform init -backend-config=../backend.hcl).
  backend "azurerm" {
    key = "shared.tfstate"
  }
}

provider "azurerm" {
  features {}
  storage_use_azuread = true
}
