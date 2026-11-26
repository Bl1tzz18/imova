# IMOVA infrastructure (Azure, Terraform)

Two environments, **dev** (created only while you work on it) and **prod**, built from one module, in **Poland Central** (Warsaw —
inside the EU, as the privacy policy says), at the lowest cost that still runs the app properly.

```
bootstrap/            once per subscription, local state: the storage account holding every other state
shared/               one Container Registry (images built once, promoted dev → prod) + the imova.md DNS zone
modules/environment/  everything one environment has (below)
envs/dev, envs/prod   the module with each environment's settings — the only difference between them
scripts/deploy.sh     build the images once, release a tag to dev, then the same tag to prod (by hand)
scripts/setup-cd.sh   one-time setup of the GitHub deploy (merge → CI → images → Approve → prod)
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

**Deploy:** from GitHub — see "Deploying from GitHub" below. By hand it's still
`scripts/deploy.sh build`, then `scripts/deploy.sh release dev <tag>`, check dev, then
`scripts/deploy.sh release prod <tag>` — the same images, never rebuilt for prod. Terraform only
sets the image when an app is created (`ignore_changes`), so an apply never rolls a deploy back.

## Deploying from GitHub

**Merge to `main` → CI → images → your Approve → prod.** Every green CI run for a push to `main`
starts `.github/workflows/deploy.yml`:
1. **Images** (automatic): api, worker and web built once, tagged with the commit (first 12
   characters), pushed to `acrimova`; a tag already in the registry isn't rebuilt.
2. **Deploy to prod** waits for you: GitHub → Actions → the run → **Review deployments** →
   production → **Approve and deploy** (GitHub also emails you). Then, in one go:
   - **Terraform** `plan` + `apply` of `shared`, then `envs/prod`, each plan listed in the run's
     summary. A plan that **deletes or replaces** anything is refused (see below);
   - **release:** the apps moved to the new images, API first (it runs the migrations); the summary
     shows the database **restore point** taken just before (docs/backup-restore.md);
   - **check:** `https://api.imova.md/health/ready` and `https://imova.md` must answer 200 (waits up
     to 3 minutes for a sleeping prod to wake).

Not approving is fine — the run just waits (GitHub drops it after 30 days); approve the newest one.
Pull requests (and forks) never start it: only a push to this repo's `main`.

**By hand:** Actions → Deploy → Run workflow (branch `main`) — for a redeploy of the current commit,
or with `what`:

| `what` | Does |
| --- | --- |
| `everything` (default) | images → (approve) → Terraform → release → checks |
| `app` | images → (approve) → release → checks |
| `infrastructure` | (approve) → Terraform only |

and **allow_destroy** for a plan that is meant to delete or replace something (read the plan in the
refused run's summary first).

**Sign-in, no stored passwords:** GitHub's OIDC token, accepted by two Entra applications only for
this repo's runs in the matching GitHub environment (both: deployments from `main` only):
- **imova-github-build** (environment **build**): AcrPush + Reader on the registry, nothing else;
- **imova-github-deploy** (environment **production**, which requires your approval): Owner of
  `rg-imova-prod` and `rg-imova-shared` (Terraform manages role assignments and delete locks there),
  blob data on the state storage, Key Vault Secrets Officer on prod's vault (Terraform,
  `key_vault_secret_officers`), and in Microsoft Graph only the applications it owns (it owns
  `imova-prod-smtp`; Terraform ignores that service principal's `owners` so it stays one).

**One-time setup** (running it again is harmless):
1. `infra/scripts/setup-cd.sh` (signed in with `az login` as the subscription owner and `gh auth
   login`): both applications, their rights and federated credentials, both GitHub environments
   with `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` as variables (IDs, not
   secrets) and you as production's required reviewer. It prints the deploy application's object id.
2. That id in `key_vault_secret_officers.github` (`envs/prod/main.tf`, `envs/dev/main.tf`).
3. One `terraform apply` on prod from your machine (it gives the deploy application its Key Vault
   role — the workflow can't read the vault before that). After that: merge, Approve.

Dev isn't in the workflow yet (it doesn't exist on the free trial): once it does, add a `dev`
environment and deploy there before prod.

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
   With the Worker on, check `@imova_md_bot`: connect it from /account → Notificări and send
   `/cautari` (the bot only answers while the Worker runs).
3. Everything under "Still to do" below that's marked for launch.

## Still to do (next tickets)

- **Dev's domains** (`dev.imova.md`, `api.dev.imova.md`): set `domains` in `envs/dev/main.tf` once
  dev exists (after the Pay-As-You-Go upgrade). **Before launch:** the API must see the visitor's real IP behind the Container
  Apps ingress (`ForwardedHeaders:KnownNetworks` + a forward limit), or every visitor shares one
  rate-limit bucket.
- ~~A CD workflow (GitHub Actions with OIDC, no stored secrets)~~ — done: "Deploying from GitHub".
