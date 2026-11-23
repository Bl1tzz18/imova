variable "image_tag" {
  description = "Image tag the apps are created with (the first apply only; later deploys use scripts/deploy.sh)."
  type        = string
}

variable "container_registry_name" {
  description = "The shared registry (infra/shared)."
  type        = string
  default     = "acrimova"
}
