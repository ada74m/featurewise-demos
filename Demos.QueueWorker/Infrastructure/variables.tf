variable "name_prefix" {
  description = "Prefixes every resource's name."
  type        = string
}

variable "demos_table_name" {
  type = string
}

variable "demos_policy_json" {
  description = "Grants the function what the facilities do."
  type        = string
}

variable "stream_arn" {
  description = "The demos table's stream."
  type        = string
}
