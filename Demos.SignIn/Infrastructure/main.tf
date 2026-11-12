# The demo sign-in: an OpenID Connect provider at its own HTTP API, which the UI shell sends visitors to
# and the UI backend trusts. It signs tokens with a KMS key, RSA for API Gateway's JWT authorizers, whose
# private half never leaves KMS. It stores nothing.

resource "aws_kms_key" "tokens" {
  description              = "${var.name_prefix}-sign-in: signs the demos' tokens"
  key_usage                = "SIGN_VERIFY"
  customer_master_key_spec = "RSA_2048"
  deletion_window_in_days  = 7
}

resource "aws_kms_alias" "tokens" {
  name          = "alias/${var.name_prefix}-sign-in"
  target_key_id = aws_kms_key.tokens.key_id
}

data "aws_iam_policy_document" "function" {
  statement {
    actions   = ["kms:Sign", "kms:GetPublicKey"]
    resources = [aws_kms_key.tokens.arn]
  }
}

module "function" {
  source = "../../Common/Infrastructure/modules/lambda-function"

  name        = "${var.name_prefix}-sign-in"
  package_zip = "${path.module}/../../functions/Demos.SignIn.zip"
  handler     = "Demos.SignIn::Demos.SignIn.Function::Handle"
  policy_json = data.aws_iam_policy_document.function.json
  environment = {
    KEY_ID       = aws_kms_key.tokens.key_id
    CLIENT_ID    = local.client_id
    REDIRECT_URI = "${var.shell_url}/_shell/callback"
    LOGOUT_URI   = "${var.shell_url}/"
  }
}

locals {
  # The shell is the sign-in's one client, which has no secret.
  client_id = "shell"

  routes = ["GET /.well-known/openid-configuration", "GET /jwks.json", "GET /authorize", "POST /token", "GET /logout"]
}

resource "aws_apigatewayv2_api" "sign_in" {
  name          = "${var.name_prefix}-sign-in"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "function" {
  api_id                 = aws_apigatewayv2_api.sign_in.id
  integration_type       = "AWS_PROXY"
  integration_uri        = module.function.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "routes" {
  for_each = toset(local.routes)

  api_id    = aws_apigatewayv2_api.sign_in.id
  route_key = each.key
  target    = "integrations/${aws_apigatewayv2_integration.function.id}"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.sign_in.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "api" {
  action        = "lambda:InvokeFunction"
  function_name = module.function.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.sign_in.execution_arn}/*/*"
}
