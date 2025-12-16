# Generowanie losowego sufiksu, aby nazwy były unikalne globalnie
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

locals {
  project_name = "smarthotel"
  location     = "Poland Central" # Dobra lokalizacja dla Polski
}

# Grupa Zasobów
resource "azurerm_resource_group" "rg" {
  name     = "rg-${local.project_name}-${random_string.suffix.result}"
  location = local.location
}

# Storage Account (wymagany przez Azure Functions)
resource "azurerm_storage_account" "sa" {
  name                     = "sa${local.project_name}${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Log Analytics Workspace (Magazyn logów - wymagany przez nowe App Insights)
resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-${local.project_name}-${random_string.suffix.result}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Application Insights (Monitoring & Observability)
resource "azurerm_application_insights" "appinsights" {
  name                = "appins-${local.project_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.law.id
}
 
# Service Plan (Serverless Consumption Plan - tani!)
resource "azurerm_service_plan" "asp" {
  name                = "asp-${local.project_name}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "B1" # Consumption tier
}

# Function App (Backend - usługa obliczeniowa 1)
resource "azurerm_linux_function_app" "function_app" {
  name                = "func-${local.project_name}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  storage_account_name       = azurerm_storage_account.sa.name
  storage_account_access_key = azurerm_storage_account.sa.primary_access_key
  service_plan_id            = azurerm_service_plan.asp.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
      use_dotnet_isolated_runtime = true
    }
    application_insights_key = azurerm_application_insights.appinsights.instrumentation_key
  }
  
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
  }
}

# SQL Server (Warstwa Danych)
resource "azurerm_mssql_server" "sqlserver" {
  name                         = "sql-${local.project_name}-${random_string.suffix.result}"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = "SuperSafePassword123!" # W produkcji używamy Key Vault!
}

# SQL Database (Serverless tier - oszczędność kosztów)
resource "azurerm_mssql_database" "db" {
  name           = "sqldb-${local.project_name}"
  server_id      = azurerm_mssql_server.sqlserver.id
  sku_name       = "S0" # Basic tier, tanio
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  storage_account_type = "Local"
}

# Firewall rule (pozwól Azure services łączyć się z bazą)
resource "azurerm_mssql_firewall_rule" "allow_azure_ips" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sqlserver.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_storage_queue" "queue" {
  name                 = "rezerwacje-queue"
  storage_account_name = azurerm_storage_account.sa.name
}