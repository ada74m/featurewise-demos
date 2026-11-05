output "issuer" {
  description = "The issuer tokens name, which the UI backend's JWT authorizer trusts: the API's URL, without a slash."
  value       = aws_apigatewayv2_api.sign_in.api_endpoint
}

output "client_id" {
  value = local.client_id
}

output "endpoints" {
  description = "The endpoints, as the UI shell's sign_in input takes them."
  value = {
    authorize_url = "${aws_apigatewayv2_api.sign_in.api_endpoint}/authorize"
    token_url     = "${aws_apigatewayv2_api.sign_in.api_endpoint}/token"
    logout_url    = "${aws_apigatewayv2_api.sign_in.api_endpoint}/logout"
    client_id     = local.client_id
  }
}
