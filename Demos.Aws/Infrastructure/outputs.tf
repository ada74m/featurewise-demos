output "table_name" {
  value = aws_dynamodb_table.demos.name
}

output "stream_arn" {
  description = "The table's stream, which delivers the outbox's rows to the queue worker."
  value       = aws_dynamodb_table.demos.stream_arn
}

output "policy_json" {
  description = "Grants what the facilities do to the table."
  value       = data.aws_iam_policy_document.demos.json
}
