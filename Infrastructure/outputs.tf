output "ui_backend_url" {
  value = module.ui_backend.url
}

output "sign_in" {
  description = "The demo sign-in's endpoints, for the UI shell's sign_in input."
  value       = module.sign_in.endpoints
}

output "demos" {
  description = "Each demo: its title, its text, its personas and its reset link, for the UI shell's demos input."
  value       = local.demos
}

output "table_name" {
  value = module.aws.table_name
}
