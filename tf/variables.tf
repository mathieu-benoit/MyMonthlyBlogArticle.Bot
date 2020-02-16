variable "bot_name" {
  description = "Name of the Bot service name and all other Azure services associated to it: resource group and application insights too."
}

variable "location" {
  description = "Location of the resources."
}

variable "bot_endpoint" {
  description = "The Bot's HTTPS backend endpoint."
  default     = "https://endpoint.com/api/messages"
}

variable "bot_display_name" {
  description = "The Bot's Display Name."
  default     = "Microsoft Azure News & Updates"
}

variable "azuread_application_password_end_date" {
  default = "2020-12-31T00:00:00.00Z"
}
