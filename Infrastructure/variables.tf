variable "name_prefix" {
  description = "The environment's prefix, such as demos. The service adds its own id, so its resources are named <name_prefix>-demos-…, apart from other services' and the UI shell's."
  type        = string
}

variable "shell_url" {
  description = "The UI shell the demos are shown through, such as https://d123.cloudfront.net, which the demo sign-in sends visitors back to."
  type        = string
}

variable "timezone" {
  description = "The time zone the job schedules are in."
  type        = string
  default     = "Europe/London"
}

variable "shell_bucket" {
  description = "The UI shell's bucket, where the service uploads its module under modules/demos/. Without one, the service has no UI, only its backends."
  type        = string
  default     = null
}
