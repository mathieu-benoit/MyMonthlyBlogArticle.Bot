provider "azurerm" {
  version = "=1.39.0"
}

resource "azurerm_resource_group" "rg" {
  name     = var.bot_name
  location = var.location
}

resource "azurerm_management_lock" "lock" {
  name       = "CanNotDelete"
  scope      = azurerm_resource_group.rg.id
  lock_level = "CanNotDelete"
  notes      = "This Resource Group and its Resources can't be deleted."
}

resource "azurerm_application_insights" "ai" {
  name                = var.bot_name
  location            = var.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_application_insights_api_key" "aikey" {
  name                    = var.bot_name
  application_insights_id = azurerm_application_insights.ai.id
  read_permissions        = ["aggregate", "api", "draft", "extendqueries", "search"]
}

resource "azurerm_bot_channels_registration" "bot" {
  name                                  = var.bot_name
  location                              = "global"
  resource_group_name                   = azurerm_resource_group.rg.name
  display_name                          = var.bot_display_name
  microsoft_app_id                      = var.microsoft_app_id
  sku                                   = "F0"
  endpoint                              = var.bot_endpoint
  developer_app_insights_key            = azurerm_application_insights.ai.instrumentation_key
  developer_app_insights_api_key        = azurerm_application_insights_api_key.aikey.api_key
  developer_app_insights_application_id = azurerm_application_insights.ai.app_id
}

resource "azurerm_bot_channel_ms_teams" "teams_channel" {
  bot_name            = azurerm_bot_channels_registration.bot.name
  location            = azurerm_bot_channels_registration.bot.location
  resource_group_name = azurerm_resource_group.rg.name
}