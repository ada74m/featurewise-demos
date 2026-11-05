variable "name_prefix" {
  description = "Prefixes every resource's name."
  type        = string
}

variable "shell_url" {
  description = "The UI shell, such as https://d123.cloudfront.net, which the sign-in sends visitors back to."
  type        = string
}
