variable "environment" {
  description = "Environment name: dev, test or prod."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "westeurope"
}

variable "container_image" {
  description = "Image of the leasing host in GHCR."
  type        = string
  default     = "ghcr.io/albertomitroi/lab-leasing:latest"
}

variable "postgres_admin_login" {
  description = "Administrator login of the PostgreSQL server."
  type        = string
  default     = "labadmin"
}

variable "postgres_admin_password" {
  description = "Administrator password of the PostgreSQL server."
  type        = string
  sensitive   = true
}
