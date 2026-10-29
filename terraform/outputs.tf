output "web_app_hostname" {
  value = azurerm_linux_web_app.leasing.default_hostname
}

output "servicebus_queues" {
  value = [for q in azurerm_servicebus_queue.leasing : q.name]
}
