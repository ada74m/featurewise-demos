# The lost property office: its table and each entrypoint's resources, from the modules in
# Common/Infrastructure. This is the module its package carries, with its functions' zip in the
# package's functions folder. An environment supplies the provider, the state, the sign-in its UI backend
# trusts, and the inputs that differ between environments.

locals {
  # The service's id, as Service.Id has it in the code: it qualifies every link's feature, as in
  # lost-property/ClaimItem, and names the shell's folder for its module.
  service = "lost-property"

  # Every resource is named <environment>-<service>-<what it is>, so services sharing an environment,
  # and the UI shell beside them, never take each other's names.
  name_prefix = "${var.name_prefix}-${local.service}"

  # Every function is in one zip, each with its own handler.
  package_zip = "${path.module}/../../functions/Demos.LostProperty.Functions.zip"
  handler     = "Demos.LostProperty.Functions::Demos.LostProperty.Functions"
}

module "table" {
  source = "../../Common/Infrastructure/modules/table"

  name_prefix = local.name_prefix
}

module "ui_backend" {
  source = "../../Common/Infrastructure/modules/ui-backend"

  package_zip = local.package_zip
  handler     = "${local.handler}.UiBackend::Handle"

  name_prefix       = local.name_prefix
  demos_table_name  = module.table.table_name
  demos_policy_json = module.table.policy_json
  jwt_issuer        = var.jwt_issuer
  jwt_audience      = var.jwt_audience
}

module "scheduled_worker" {
  source = "../../Common/Infrastructure/modules/scheduled-worker"

  package_zip = local.package_zip
  handler     = "${local.handler}.ScheduledWorker::Handle"

  name_prefix       = local.name_prefix
  demos_table_name  = module.table.table_name
  demos_policy_json = module.table.policy_json
  timezone          = var.timezone
  schedules = {
    charity-collection = {
      expression = "cron(0 6 * * ? *)"
      link       = jsonencode({ "$feature" = "${local.service}/HandleCharityCollectionDue" })
    }
  }
}

module "queue_worker" {
  source = "../../Common/Infrastructure/modules/queue-worker"

  package_zip = local.package_zip
  handler     = "${local.handler}.QueueWorker::Handle"

  name_prefix       = local.name_prefix
  demos_table_name  = module.table.table_name
  demos_policy_json = module.table.policy_json
  stream_arn        = module.table.stream_arn
}

# The service's module for the UI shell, in the shell's bucket, when the environment has a shell.
module "ui" {
  source = "../../Common/Infrastructure/modules/ui-module"
  count  = var.shell_bucket == null ? 0 : 1

  dist = "${path.module}/../Demos.LostProperty.Ui/dist"

  shell_bucket = var.shell_bucket
  service_id   = local.service
}
