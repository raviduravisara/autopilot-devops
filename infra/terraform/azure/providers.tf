terraform {
  required_version = ">= 1.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id

  # Skip auto-registering ~50 resource providers (very slow on student
  # subscriptions). The few providers this project needs are registered
  # manually with: az provider register --namespace <name>
  resource_provider_registrations = "none"
}
