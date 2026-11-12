variable "name_prefix" {
  description = "The environment's prefix, such as demos. The service adds its own id, so its resources are named <name_prefix>-box-office-…, apart from other services' and the UI shell's."
  type        = string
}

variable "jwt_issuer" {
  description = "The issuer of visitors' tokens, the demo sign-in's."
  type        = string
}

variable "jwt_audience" {
  description = "The audiences a visitor's token may be for: the sign-in's client id."
  type        = list(string)
}

variable "shell_bucket" {
  description = "The UI shell's bucket, where the service uploads its module under modules/box-office/. Without one, the service has no UI, only its backends."
  type        = string
  default     = null
}
