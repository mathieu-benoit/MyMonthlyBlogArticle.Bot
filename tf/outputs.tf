output "app_insights_instrumentation_key" {
  description = "Application Insights InstrumentationKey"
  value       = azurerm_application_insights.ai.instrumentation_key
}