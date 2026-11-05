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

variable "schedules" {
  description = "The jobs to run, by schedule name: each schedule's expression, and its job's link as JSON, such as jsonencode({ \"$feature\" = \"demos/HandleCharityCollectionDue\" })."
  type = map(object({
    expression = string
    link       = string
  }))
}

variable "timezone" {
  description = "The time zone schedule expressions are in."
  type        = string
  default     = "UTC"
}
