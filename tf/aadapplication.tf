#resource "random_password" "password" {
#  length  = 16
#  special = true
#  min_numeric = 1
#  min_special = 1
#}

#resource "azuread_application" "app" {
#  name                       = var.bot_name
#  available_to_other_tenants = false
#}

#resource "azuread_application_password" "password" {
#  application_object_id = azuread_application.app.object_id
#  value                 = random_password.password.result
#  end_date              = var.azuread_application_password_end_date

#  lifecycle {
#    ignore_changes = [end_date]
#  }
#}
