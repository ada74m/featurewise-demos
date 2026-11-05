# What the AWS facilities need, whichever function uses them: one table for every demo's data and its
# outbox. Each row's partition key is its visitor's sandbox, and TTL removes a sandbox's rows a week after
# it first used them. The stream delivers the outbox's rows to the queue worker.

resource "aws_dynamodb_table" "demos" {
  name         = var.name_prefix
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Sandbox"
  range_key    = "Key"

  attribute {
    name = "Sandbox"
    type = "S"
  }

  attribute {
    name = "Key"
    type = "S"
  }

  ttl {
    attribute_name = "Expires"
    enabled        = true
  }

  stream_enabled   = true
  stream_view_type = "NEW_IMAGE"
}

# What the facilities do, for each function's policy: read a sandbox's rows, and write them in transactions.
data "aws_iam_policy_document" "demos" {
  statement {
    actions   = ["dynamodb:GetItem", "dynamodb:Query", "dynamodb:Scan", "dynamodb:PutItem", "dynamodb:ConditionCheckItem"]
    resources = [aws_dynamodb_table.demos.arn]
  }
}
