output "resource_group_name" {
  description = "Name of the created resource group"
  value       = azurerm_resource_group.main.name
}

output "postgres_server_fqdn" {
  description = "PostgreSQL server hostname"
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgres_database_name" {
  description = "Application database name"
  value       = azurerm_postgresql_flexible_server_database.app.name
}

output "container_app_environment_id" {
  description = "Container Apps environment ID (used when deploying the apps)"
  value       = azurerm_container_app_environment.main.id
}

output "postgres_connection_string" {
  description = "Connection string for the backend (sensitive)"
  value       = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.app.name};Username=${var.postgres_admin_user};Password=${var.postgres_admin_password};SSL Mode=Require"
  sensitive   = true
}
