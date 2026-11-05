# The queue worker: one function, which the demos table's stream invokes with each event committed to the
# outbox. Only inserted outbox rows reach it; the stream's other changes are filtered out before Lambda
# is invoked.

module "function" {
  source = "../../Infrastructure/modules/lambda-function"

  name        = "${var.name_prefix}-queue-worker"
  package_zip = "${path.module}/../../functions/Demos.QueueWorker.zip"
  handler     = "Demos.QueueWorker::Demos.QueueWorker.Function::Handle"
  policy_json = data.aws_iam_policy_document.function.json
  timeout     = 60
  environment = {
    DEMOS_TABLE = var.demos_table_name
  }
}

# What the facilities do, and reading the stream.
data "aws_iam_policy_document" "function" {
  source_policy_documents = [var.demos_policy_json]

  statement {
    actions   = ["dynamodb:DescribeStream", "dynamodb:GetRecords", "dynamodb:GetShardIterator", "dynamodb:ListStreams"]
    resources = [var.stream_arn]
  }
}

# The function reports the first event that fails, and the stream delivers it and those after it again,
# up to five times, before moving on.
resource "aws_lambda_event_source_mapping" "outbox" {
  event_source_arn                   = var.stream_arn
  function_name                      = module.function.function_arn
  starting_position                  = "LATEST"
  batch_size                         = 10
  maximum_batching_window_in_seconds = 0
  maximum_retry_attempts             = 5
  function_response_types            = ["ReportBatchItemFailures"]

  filter_criteria {
    filter {
      pattern = jsonencode({
        eventName = ["INSERT"]
        dynamodb  = { Keys = { Key = { S = [{ prefix = "outbox#" }] } } }
      })
    }
  }
}
