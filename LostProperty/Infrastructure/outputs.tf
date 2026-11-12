output "ui_backend_url" {
  value = module.ui_backend.url
}

output "demo" {
  description = "The demo: its title, its text, its personas and its reset link, for the UI shell's demos input and the sign-in's."
  value       = jsondecode(file("${path.module}/../demo.json"))
}

output "table_name" {
  value = module.table.table_name
}
