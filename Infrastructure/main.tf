# The demos service, now only the box office, until it becomes a service of its own as the lost property
# office has: its table and UI backend, from the modules in Common/Infrastructure, and the demo sign-in. This is the module a package carries, with each function's zip in its functions folder. An
# environment supplies the provider, the state, and the inputs that differ between environments.

locals {
  # The service's id, as Service.Id has it in the code: it qualifies every link's feature, as in
  # demos/ClaimItem, and names the shell's folder for its module.
  service = "demos"

  # Every resource is named <environment>-<service>-<what it is>, so services sharing an environment,
  # and the UI shell beside them, never take each other's names.
  name_prefix = "${var.name_prefix}-${local.service}"

  # Each demo: its title, its text, its personas and its reset link. The sign-in offers every demo's
  # personas, the lost property office's included.
  demos = concat([jsondecode(file("${path.module}/../LostProperty/demo.json"))], jsondecode(file("${path.module}/../demos.json")))
}

module "aws" {
  source = "../Common/Infrastructure/modules/table"

  name_prefix = local.name_prefix
}

# Visitors sign in as the demos' personas, which demos.json lists, with no passwords.
module "sign_in" {
  source = "../SignIn/Infrastructure"

  name_prefix = local.name_prefix
  shell_url   = var.shell_url
  demos       = local.demos
}

module "ui_backend" {
  source = "../Common/Infrastructure/modules/ui-backend"

  package_zip = "${path.module}/../functions/Demos.UiBackend.zip"
  handler     = "Demos.UiBackend::Demos.UiBackend.Function::Handle"

  name_prefix       = local.name_prefix
  demos_table_name  = module.aws.table_name
  demos_policy_json = module.aws.policy_json
  jwt_issuer        = module.sign_in.issuer
  jwt_audience      = [module.sign_in.client_id]
}



# The service's module for the UI shell, in the shell's bucket, when the environment has a shell.
module "ui" {
  source = "../Common/Infrastructure/modules/ui-module"
  count  = var.shell_bucket == null ? 0 : 1

  dist = "${path.module}/../Demos.Ui/dist"

  shell_bucket = var.shell_bucket
  service_id   = local.service
}
