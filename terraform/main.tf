locals {
  name = "lab-leasing-${var.environment}"
  queues = [
    "credit-check-requests",
    "leasing-credit-check-responses",
    "notice-outbox",
    "leasing-ordered-projection-messages",
  ]
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "leasing" {
  name     = "rg-${local.name}"
  location = var.location
}

resource "azurerm_log_analytics_workspace" "leasing" {
  name                = "log-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "leasing" {
  name                = "appi-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name
  workspace_id        = azurerm_log_analytics_workspace.leasing.id
  application_type    = "web"
}

resource "azurerm_key_vault" "leasing" {
  name                       = "kv-${replace(local.name, "-", "")}"
  location                   = azurerm_resource_group.leasing.location
  resource_group_name        = azurerm_resource_group.leasing.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
}

resource "azurerm_postgresql_flexible_server" "leasing" {
  name                          = "psql-${local.name}"
  location                      = azurerm_resource_group.leasing.location
  resource_group_name           = azurerm_resource_group.leasing.name
  version                       = "16"
  sku_name                      = "B_Standard_B1ms"
  storage_mb                    = 32768
  administrator_login           = var.postgres_admin_login
  administrator_password        = var.postgres_admin_password
  public_network_access_enabled = false
}

resource "azurerm_postgresql_flexible_server_database" "leasing" {
  name      = "leasing"
  server_id = azurerm_postgresql_flexible_server.leasing.id
}

resource "azurerm_servicebus_namespace" "leasing" {
  name                = "sb-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "leasing" {
  for_each     = toset(local.queues)
  name         = "${var.environment}-${each.value}"
  namespace_id = azurerm_servicebus_namespace.leasing.id

  dead_lettering_on_message_expiration = true
}

resource "azurerm_signalr_service" "leasing" {
  name                = "sigr-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name

  sku {
    name     = "Standard_S1"
    capacity = 1
  }

  service_mode = "Default"
}

resource "azurerm_service_plan" "leasing" {
  name                = "asp-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name
  os_type             = "Linux"
  sku_name            = "P0v3"
}

resource "azurerm_linux_web_app" "leasing" {
  name                = "app-${local.name}"
  location            = azurerm_resource_group.leasing.location
  resource_group_name = azurerm_resource_group.leasing.name
  service_plan_id     = azurerm_service_plan.leasing.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      docker_image_name   = replace(var.container_image, "ghcr.io/", "")
      docker_registry_url = "https://ghcr.io"
    }
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT                           = title(var.environment)
    APPLICATIONINSIGHTS_CONNECTION_STRING            = azurerm_application_insights.leasing.connection_string
    Messaging__Transport                             = "AzureServiceBus"
    Messaging__AzureServiceBusHost__ConnectionString = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.leasing.name};SecretName=servicebus-connection-string)"
    Azure__SignalR__ConnectionString                 = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.leasing.name};SecretName=signalr-connection-string)"
    ConnectionStrings__LeasingDb                     = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.leasing.name};SecretName=leasing-db-connection-string)"
  }
}

resource "azurerm_role_assignment" "web_app_reads_secrets" {
  scope                = azurerm_key_vault.leasing.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.leasing.identity[0].principal_id
}
