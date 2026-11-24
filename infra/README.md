# IMOVA infrastructure (Azure, Terraform)

Two environments, **dev** (created only while you work on it) and **prod**, built from one module, in **Poland Central** (Warsaw —
inside the EU, as the privacy policy says), at the lowest cost that still runs the app properly.

```
bootstrap/            once per subscription, local state: the storage account holding every other state
shared/               one Container Registry (images built once, promoted dev → prod) + the imova.md DNS zone
modules/environment/  everything one environment has (below)
envs/dev, envs/prod   the module with each environment's settings — the only difference between them
scripts/deploy.sh     build the images once, release a tag to dev, then the same tag to prod
backend.hcl           where shared/ and envs/* keep their state
```

Each environment (`rg-imova-<env>`):

| Piece | What | Notes |
| --- | --- | --- |
| Container Apps environment `cae-imova-<env>` | Consumption plan, no VNet | billed per second used; 180k vCPU-s / 360k GiB-s / 2M requests free per month per subscription |
| `imova-web` | Next.js, port 3000, public | |
| `imova-api` | ASP.NET Core, port 8080, public (the browser calls it: uploads, typeaheads, the messaging hub) | max 1 replica until SignalR has a Redis backplane; runs the migrations at startup |
| `imova-worker` | scheduled jobs + Telegram bot, no ingress | 0 or 1 replica, never more |
| PostgreSQL Flexible Server 16 | B1ms (1 vCore, 2 GiB), 32 GB, PostGIS allow-listed | backups 7 days (the privacy policy's promise), TLS required and verified |
| Storage account | `listing-images` (public read), `message-attachments` (private) | deleted blobs recoverable for 7 days; CORS = the site's origin |
| Key Vault | every secret the apps read, through their managed identity | |
| Log Analytics + Application Insights | 30 days' retention, daily cap | prod: error alert by email (uptime: a free outside monitor) |
| User-assigned identity | pulls images (AcrPull), reads secrets (Key Vault Secrets User) | no registry passwords |

## Costs (USD, Poland Central retail prices, November 2026)

| | Monthly | With the free account (first 12 months) |
| --- | --- | --- |
| **Shared**: Container Registry (Standard: free for 12 months; then switch to Basic, $5.07), DNS zone $0.50 | **~$5.60** | **$0.50** |
| **Prod until launch** (asleep: apps scale to zero, Worker off): Postgres B1ms $14.53 + 32 GB $4.38; error alert ~$0.50; storage/Key Vault cents | **~$20** | **~$1** (the database is free) |
| **Prod after launch**: the same + apps always on (1 vCPU, 2 GiB in total) ~$22 while idle, more under load; logs within the free 5 GB | **~$40–45** | **~$22–27** |
| **Dev**: only exists while you work on it (destroyed otherwise) | **$0** when destroyed, ~$0.70/day while up | same |
| **Uptime**: a free outside monitor | $0 | $0 |
| **Total until launch** | **~$26** | **~$1.50** |
| **Total after launch** | **~$46–51** | **~$23–28** |

The free account: the first 30 days are covered by the $200 credit; for 12 months 750 hours a month
of one B1ms server with 32 GB (prod's database) and one Standard Container Registry are free.
**Before the 12 months end** (the subscription was created in November 2026): set
`container_registry_sku = "Basic"` in `shared/terraform.tfvars` and apply — Standard would then
cost ~$20/month. The same month the MediatR license (Key Vault `mediatr-license-key`) expires
(2027-11-23): renew it.

**DNS is in Azure** ($0.50/month for the whole zone): imova.md stays registered at tophost.md, only
its nameservers point at Azure. That's what lets dev have its own domain: dev is destroyed after
every working session and comes back with a new address, so Terraform has to rewrite its records
each time — by hand at tophost.md that would be four records per session.

**Why Poland Central:** West Europe refuses new free-trial subscriptions ("not accepting new
customers", November 2026), and it was also among the dearest EU regions. Poland Central is the
closest Azure region to Moldova (lowest latency for visitors), inside the EU (as the privacy policy
says), has every service used here, and costs the same as the cheapest EU regions within ~$2/month.

**No cloud dev on the free trial.** The free-trial subscription allows only **one Container Apps
environment in the whole subscription** (`MaxNumberOfGlobalEnvironmentsInSubExceeded` — prod has
it), and PostgreSQL wasn't available to it in Germany West Central either. Until the upgrade,
prod (asleep, no real users yet) is the test environment and `docker compose` is dev.
**Upgrade to Pay-As-You-Go before the trial ends (~2026-12-23)** or prod stops: the remaining
credit and the 12 months of free services carry over, but the spending limit goes — add budget
alerts first. Then move dev back to `polandcentral` (`envs/dev/main.tf`) and create it.

**Decided against, to keep it cheap** (revisit when traffic or budget justify it):
- **A virtual network** (private database, fixed outbound IP): it brings a standard load balancer
  and two static public IPs, ~$25/month **per environment**. Instead the database has a public
  endpoint with TLS (`SSL Mode=VerifyFull`), a 32-character random password and a firewall
  allowing only Azure services (+ the IPs in `postgres_admin_ip_ranges`).
- **Azure's availability test** (~$5.60/month every 15 minutes, ~$17 every 5): a free outside
  monitor instead (see "Uptime"). The module still has it (`availability_test_enabled`).
- **An always-on dev**: created and destroyed on demand instead.
- Zone redundancy, geo-redundant backups, a second API replica, dedicated workload profiles,
  private endpoints.
- Separate subscriptions for dev and prod (best practice; both run in one so the free credits
  cover both). Each environment has its own state and provider, so splitting later is a matter of
  pointing `ARM_SUBSCRIPTION_ID` elsewhere and re-applying.

**Cost levers:** `apps` in `envs/prod/main.tf` (CPU/memory; `min_replicas = 0` makes an app scale to
zero — how prod runs until launch — at the price of a few seconds' cold start for the first visitor
after a quiet spell), and `log_daily_cap_gb`.

## First-time setup

Tools: [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), Terraform ≥ 1.11, Docker.

```bash
az login
export ARM_SUBSCRIPTION_ID=$(az account show --query id -o tsv)   # every Terraform run needs it

# 1. State storage (once). It starts with local state, then moves its own state into the account
#    it created (see the comment in bootstrap/versions.tf).
cd infra/bootstrap && terraform init -backend=false && terraform apply   # backend block commented out
terraform init -migrate-state -backend-config=../backend.hcl            # backend block back in

# 2. The image registry + the DNS zone.
cd ../shared && terraform init -backend-config=../backend.hcl && terraform apply
terraform output dns_name_servers    # → set these 4 at tophost.md (Domains → imova.md → Nameservers)

# 3. Build and push the images (from a clean, committed tree; the tag is the commit).
cd .. && scripts/deploy.sh build     # prints the tag

# 4. Each environment, dev first.
cd envs/dev && terraform init -backend-config=../../backend.hcl
terraform apply -var image_tag=<tag>
terraform output web_url             # the site
```

Names that must be globally unique: `stimovatfstate` (bootstrap) and `acrimova` (shared). If Azure
says one is taken, change it in `bootstrap/variables.tf` + `backend.hcl`, or in
`shared/terraform.tfvars` + `envs/*/variables.tf`. The rest get a random suffix.

## Everyday work

**Deploy:** `scripts/deploy.sh build`, then `scripts/deploy.sh release dev <tag>`, check dev, then
`scripts/deploy.sh release prod <tag>` — the same images, never rebuilt for prod. Terraform only
sets the image when an app is created (`ignore_changes`), so an apply never rolls a deploy back.

**Dev on demand.** Dev is disposable: create it when you need it, destroy it when you're done (its
database and photos go with it; nothing in dev is worth keeping).

```bash
cd infra/envs/dev
terraform apply -var image_tag=<tag>      # ~10 minutes; then scripts/deploy.sh release dev <tag> for newer tags
terraform destroy -var image_tag=<tag>    # back to $0
```

Each new dev gets fresh random names (Key Vault, storage, database), so a destroyed one never
blocks the next. If you only pause for a day or two, stopping the database is enough
(`az postgres flexible-server stop -g rg-imova-dev -n <postgres_server_name>`, ~$0.15/day left for
its disk; Azure starts it again by itself after 7 days).

**Uptime.** A free outside monitor — [Better Stack](https://betterstack.com/uptime) (free: 10
monitors, every 3 minutes, email alerts, commercial use allowed; alternative: HetrixTools, 15
monitors every minute). Two monitors on prod:
1. the home page (`web_url` output), expecting 200 — what visitors see; it calls the API and the
   database behind it;
2. the API's `/health/ready` (`api_url` output + `/health/ready`), expecting 200 — 503 means the
   API is up but can't reach the database.
Update them when the custom domains arrive (KAN-5). Errors inside the apps are covered by the Azure
error alert (`error_alert_enabled`, prod).

**Infrastructure change:** edit, then `terraform plan` in `envs/dev`, apply, check, then the same in
`envs/prod`. A change to the module reaches both — apply dev first.

**Secrets set by hand** (Terraform creates them with the value `set-me` and never overwrites them):

| Secret | Turned on by | Value |
| --- | --- | --- |
| `google-client-id`, `google-client-secret` | `google_auth_enabled = true` | the environment's Google OAuth client (KAN-8) |
| `telegram-bot-token` | `telegram_bot_username = "..."` | that environment's own bot (one bot per environment) |
| `mediatr-license-key` | `mediatr_license_enabled = true` (prod) | the Lucky Penny MediatR Community license — **valid until 2027-11-23**, renew it at luckypennysoftware.com |

```bash
az keyvault secret set --vault-name <key_vault_name> --name telegram-bot-token --value '<token>'
# then restart so the apps read it:
az containerapp revision restart -g rg-imova-<env> -n imova-worker \
  --revision $(az containerapp show -g rg-imova-<env> -n imova-worker --query properties.latestRevisionName -o tsv)
```

**Generated secrets** (database password, JWT signing key) are write-only: Terraform sends them to
Azure but they are never in the state. To rotate both, bump `secrets_version` in the environment's
module call and apply — every session is signed out (new JWT key). If an apply fails half way
after a bump, bump again so the database and Key Vault get the same new password.

**Database access from your machine** (e.g. granting the first admin). No psql needed — Docker has
it. Prod's delete lock on the server also covers its firewall rules, so closing access again means
lifting the lock for a moment; Terraform puts it back:

```bash
SERVER=psql-imova-prod-wpq4p   # terraform output postgres_server_name
KV=kv-imova-prod-wpq4p         # terraform output key_vault_name
MYIP=$(curl -s https://api.ipify.org)
az postgres flexible-server firewall-rule create -g rg-imova-prod --server-name $SERVER \
  --name temp-access --start-ip-address $MYIP --end-ip-address $MYIP
docker run --rm -e PGPASSWORD="$(az keyvault secret show --vault-name $KV --name postgres-connection-string \
  --query value -o tsv | sed -n 's/.*Password=\([^;]*\).*/\1/p')" postgres:16-alpine \
  psql "host=$SERVER.postgres.database.azure.com dbname=imova user=imova_admin sslmode=require" -c "…"
# close it again:
az lock delete --name do-not-delete -g rg-imova-prod --resource-name $SERVER \
  --resource-type Microsoft.DBforPostgreSQL/flexibleServers
az postgres flexible-server firewall-rule delete -g rg-imova-prod --server-name $SERVER --name temp-access --yes
cd infra/envs/prod && terraform apply -var image_tag=<current>   # recreates the lock
docker image rm postgres:16-alpine
```

**First admin** (2026-11-23): granted that way — a row in `AspNetUserRoles` for the Admin role plus a
`GrantAdmin` row in `AdminAuditEntries` (actor = target, since nobody could grant it). Later admins
are added from `/admin/admins`.

**Protection:** prod has CanNotDelete locks on the database, storage and Key Vault, and Key Vault
purge protection (90 days). Removing prod on purpose = remove `protect_data`, apply, then destroy.

## Custom domain (KAN-5)

| Environment | Site | API |
| --- | --- | --- |
| prod | `imova.md` (`www.imova.md` redirects to it) | `api.imova.md` |
| dev | `dev.imova.md` | `api.dev.imova.md` |

The `domains` setting of an environment (`envs/<env>/main.tf`) does it all
(`modules/environment/domains.tf`): per name an A record (the apex, to the environment's static IP)
or a CNAME (to the app's own hostname) plus the `asuid.<name>` TXT proof, the name added to the
app, a free managed certificate that Container Apps renews by itself (HTTP validation for the
apex, CNAME otherwise), and the binding — an `az containerapp hostname bind` run by Terraform, since
the provider has no resource for it (so Terraform needs the Azure CLI, signed in). Issuing takes a
few minutes per name. Only then set `web_url`/`api_url` to the new addresses and apply again
(links in emails, CORS, canonical URLs, the browser's API address).

The web app redirects `www.` to the bare domain (`lib/seo/canonicalHost.ts`, 308).

**Client IP.** The API rate-limits per visitor, so it must see the visitor's address behind the
Container Apps ingress: the web server calls the API by app name (`API_URL = http://imova-api` —
the public address would resolve to the environment's public IP even from inside and come back
in with Azure's outbound address), and the API trusts private addresses and two
`X-Forwarded-For` hops (`ForwardedHeaders:KnownNetworks` + `ForwardLimit = 2`;
`AuthRateLimiting.ConfigureForwardedHeaders`, tests in `ForwardedHeadersTests`). Email records
(SPF, DKIM, DMARC, MX) are in the same zone — see "Email".

## Email (KAN-14)

**Outgoing** (confirmations, password resets, alerts) — Azure Communication Services Email, from
`no-reply@imova.md`, data in Europe:
- `shared/email.tf`: the email service and the `imova.md` domain, verified through DNS records
  Terraform writes in the zone (the Azure domain proof and SPF on the apex TXT, two DKIM CNAMEs,
  a `_dmarc` TXT with `p=none`), then asks Azure to check them.
- `modules/environment/email.tf` (`acs_email` in `envs/<env>/main.tf`): the environment's own
  Communication Service linked to that domain, an Entra application `imova-<env>-smtp` (ACS
  authenticates SMTP through it), its client secret in Key Vault as `email-password` (2 years,
  rotated with `secrets_version`), and the SMTP username `imova-<env>`. The apps send through
  `smtp.azurecomm.net:587` with STARTTLS — the same SmtpEmailSender as with Mailpit locally.
- Limits for a custom domain: 30 emails a minute, 100 an hour per subscription. Raise them with a
  quota request (Azure portal → the email domain) once there's some sending history.

**Incoming** (`contact@imova.md`, promised in the Terms and the Privacy policy) — a Zoho Mail
mailbox (free plan, EU data centre): the admin mailbox `catalinfilip@imova.md` with `contact@` as an
alias. Its DNS records are in `shared/terraform.tfvars`: `mx_records` (mx/mx2/mx3.zoho.eu),
`spf_includes = ["zoho.eu"]` (one SPF record per domain, so Zoho is added to Azure's —
`v=spf1 include:spf.protection.outlook.com include:zoho.eu -all`), `apex_txt_extra` (Zoho's domain
check) and `txt_records` (Zoho's DKIM key on `zmail._domainkey`). Zoho may show its own SPF as
not matching: ours is the combined one on purpose.

## Launch checklist (prod)

0. Prod was the test environment before launch: empty the test data (accounts, listings, photos,
   messages) — e.g. drop and recreate the `imova` database (the API recreates the tables and
   reference data on start) and empty both blob containers.
1. Wake prod up: in `envs/prod/main.tf`, web `min_replicas = 1`, api `min_replicas = 1`, worker
   `replicas = 1`; `terraform apply`.
2. The uptime monitors (see "Uptime") on the final addresses.
3. Everything under "Still to do" below that's marked for launch.

## Still to do (next tickets)

- **Dev's domains** (`dev.imova.md`, `api.dev.imova.md`): set `domains` in `envs/dev/main.tf` once
  dev exists (after the Pay-As-You-Go upgrade). **Before launch:** the API must see the visitor's real IP behind the Container
  Apps ingress (`ForwardedHeaders:KnownNetworks` + a forward limit), or every visitor shares one
  rate-limit bucket.
- **KAN-8** — production Google OAuth client and origin, the prod Telegram bot.
- **contact@imova.md** — the Zoho mailbox (see "Email"), before launch (KAN-34).
- **KAN-15** — tested restore of the database (point-in-time) and the blobs.
- A CD workflow (GitHub Actions with OIDC, no stored secrets) running `deploy.sh`.
