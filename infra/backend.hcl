# Where every stack except bootstrap keeps its state (created by bootstrap/). Not a secret: access
# is by your Azure sign-in (Entra ID), the account has no keys. Each stack sets its own `key`.
#   terraform init -backend-config=../../backend.hcl   (from envs/<env>)
#   terraform init -backend-config=../backend.hcl      (from shared)
resource_group_name  = "rg-imova-tfstate"
storage_account_name = "stimovatfstate"
container_name       = "tfstate"
use_azuread_auth     = true
