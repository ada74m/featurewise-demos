# The demos service: the AWS facilities' resources and each entrypoint's, from the modules beside each
# project. This is the module a package carries, with each function's zip in its functions folder. An
# environment supplies the provider, the state, and the inputs that differ between environments.

locals {
  # The service's id, as Service.Id has it in the code: it qualifies every link's feature, as in
  # demos/ClaimItem, and names the shell's folder for its module.
  service = "demos"

  # Every resource is named <environment>-<service>-<what it is>, so services sharing an environment,
  # and the UI shell beside them, never take each other's names.
  name_prefix = "${var.name_prefix}-${local.service}"
}

module "aws" {
  source = "../Demos.Aws/Infrastructure"

  name_prefix = local.name_prefix
}

# Visitors sign in as the demos' personas, which personas.json lists, with no passwords.
module "sign_in" {
  source = "../Demos.SignIn/Infrastructure"

  name_prefix = local.name_prefix
  shell_url   = var.shell_url
}

module "ui_backend" {
  source = "../Demos.UiBackend/Infrastructure"

  name_prefix       = local.name_prefix
  demos_table_name  = module.aws.table_name
  demos_policy_json = module.aws.policy_json
  jwt_issuer        = module.sign_in.issuer
  jwt_audience      = [module.sign_in.client_id]
}

module "scheduled_worker" {
  source = "../Demos.ScheduledWorker/Infrastructure"

  name_prefix       = local.name_prefix
  demos_table_name  = module.aws.table_name
  demos_policy_json = module.aws.policy_json
  timezone          = var.timezone
  schedules = {
    charity-collection = {
      expression = "cron(0 6 * * ? *)"
      link       = jsonencode({ "$feature" = "${local.service}/HandleCharityCollectionDue" })
    }
  }
}

module "queue_worker" {
  source = "../Demos.QueueWorker/Infrastructure"

  name_prefix       = local.name_prefix
  demos_table_name  = module.aws.table_name
  demos_policy_json = module.aws.policy_json
  stream_arn        = module.aws.stream_arn
}

# The service's module for the UI shell, in the shell's bucket, when the environment has a shell.
module "ui" {
  source = "../Demos.Ui/Infrastructure"
  count  = var.shell_bucket == null ? 0 : 1

  shell_bucket = var.shell_bucket
  service_id   = local.service
}
