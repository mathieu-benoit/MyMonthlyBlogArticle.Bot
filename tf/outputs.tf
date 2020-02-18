output "app_insights_instrumentation_key" {
  description = "Application Insights InstrumentationKey"
  value       = azurerm_application_insights.ai.instrumentation_key
}

output "azuread_application_application_id" {
  description = "AAD App's Application Id for the Bot Service"
  value       = azuread_application.app.application_id
}

output "azuread_application_application_secret" {
  description = "AAD App's Application Secret for the Bot Service"
  value       = azuread_application_password.password.value
}
