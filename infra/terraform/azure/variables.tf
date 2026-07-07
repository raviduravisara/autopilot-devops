variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "southeastasia"
}

variable "resource_group_name" {
  description = "Name of the resource group that holds everything"
  type        = string
  default     = "autopilot-rg"
}

variable "prefix" {
  description = "Short prefix for resource names (keep lowercase, no spaces)"
  type        = string
  default     = "autopilot"
}

variable "postgres_admin_user" {
  description = "PostgreSQL administrator username"
  type        = string
  default     = "autopilotadmin"
}

variable "postgres_admin_password" {
  description = "PostgreSQL administrator password (provide via tfvars or TF_VAR env, never commit)"
  type        = string
  sensitive   = true
}

variable "postgres_database_name" {
  description = "Application database name"
  type        = string
  default     = "autopilot"
}
