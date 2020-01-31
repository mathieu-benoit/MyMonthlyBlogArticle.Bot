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