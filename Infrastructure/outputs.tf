output "ui_backend_url" {
  value = module.ui_backend.url
}

output "sign_in" {
  description = "The demo sign-in's endpoints, for the UI shell's sign_in input."
  value       = module.sign_in.endpoints
}

output "personas" {
  description = "The personas visitors can be, for the UI shell's personas input."
  value       = jsondecode(file("${path.module}/../personas.json"))
}

output "table_name" {
  value = module.aws.table_name
}
