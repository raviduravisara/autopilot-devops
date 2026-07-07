# Resource group: a logical container that holds all AutoPilot resources.
# Deleting this group deletes everything inside it (handy for cost control).
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    project     = "autopilot-devops"
    environment = "demo"
    managed_by  = "terraform"
  }
}

# PostgreSQL Flexible Server on the cheapest Burstable tier (B1ms).
# Burstable is the lowest-cost option, suitable for demos.
resource "azurerm_postgresql_flexible_server" "main" {
  name                          = "${var.prefix}-pg"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "16"
  administrator_login           = var.postgres_admin_user
  administrator_password        = var.postgres_admin_password
  sku_name                      = "B_Standard_B1ms"
  storage_mb                    = 32768
  public_network_access_enabled = true
  zone                          = "1"

  tags = {
    project = "autopilot-devops"
  }
}

# The application database inside the server.
resource "azurerm_postgresql_flexible_server_database" "app" {
  name      = var.postgres_database_name
  server_id = azurerm_postgresql_flexible_server.main.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# Firewall rule allowing Azure services (like Container Apps) to reach Postgres.
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Log Analytics workspace: required backend for Container Apps environment logs.
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${var.prefix}-logs"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Container Apps environment: the runtime that hosts the backend and frontend
# container apps. Scale-to-zero keeps idle cost near zero.
resource "azurerm_container_app_environment" "main" {
  name                       = "${var.prefix}-env"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}
