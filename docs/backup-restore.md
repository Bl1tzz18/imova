# Backups and restore (KAN-15)

What protects prod's data, how to see it, and how to get it back. Tested on 2026-11-24 (below).

## What is protected

| Data | Protection | Window |
| --- | --- | --- |
| PostgreSQL (`psql-imova-prod-*`) | Azure's automatic backups: a full snapshot about once a day plus the transaction log, so a restore can go to **any second** in the window | 7 days (`backup_retention_days` — the privacy policy promises at most 7) |
| Blobs (`listing-images`, `message-attachments`) | soft delete: a deleted blob (or container) stays recoverable | 7 days |
| Database, storage account, Key Vault | `CanNotDelete` locks (`protect_data`); Key Vault purge protection | — |

Not covered, on purpose (cost): copies in another region (geo-redundant backup is off — a lost
Poland Central region means lost data), blob versioning (an *overwritten* blob can't be brought
back; the app never overwrites an original — every upload gets a new name — only the resized
copies, which are rebuilt from the originals), and on-demand database backups (**Burstable servers
can't take them**: `CustomerOnDemandBackupCannotBePerformedOnBurstableServer`).

## Seeing the backups

Portal: the PostgreSQL server → **Settings → Backup and restore** (list of full backups,
"Earliest restore point", the Restore button). Or:

```bash
az postgres flexible-server backup list -g rg-imova-prod --server-name psql-imova-prod-wpq4p -o table
az postgres flexible-server show -g rg-imova-prod -n psql-imova-prod-wpq4p --query backup
```

## Before every production release

`scripts/deploy.sh release prod <tag>` prints **"Restore point before this release: <UTC time>"**
before it touches anything (the API runs the migrations as it starts). Keep that line: it is the
time to restore to if the migration goes wrong. That replaces "take a manual backup first", which
B1ms can't do. You have 7 days to notice.

## Restoring the database

A restore always creates a **new server**; prod is never overwritten.

```bash
az postgres flexible-server restore -g rg-imova-prod --name psql-imova-restore \
  --source-server psql-imova-prod-wpq4p --restore-time "2026-11-23T14:34:00Z"   # UTC
```

- Takes about **6 minutes** (2026-11-24, a nearly empty database).
- The new server has the same admin user and password as prod (the Key Vault connection string's
  password works), the same size and settings — but **no firewall rules**: add one for your IP to
  look at it (`firewall-rule create`, as in `infra/README.md`, "Database access").
- It is billed like prod's server until deleted:
  `az postgres flexible-server delete -g rg-imova-prod -n psql-imova-restore --yes`.

**Putting the data back into prod** (not rehearsed yet — rehearse it on dev once dev exists):
stop the API and the Worker from writing (scale them to 0 replicas), then copy the restored
`imova` database over prod's with `pg_dump -Fc` from the restored server and
`pg_restore --clean --if-exists` into prod's server (Docker's `postgres:16-alpine` has both), then
start the apps again with the *previous* image tag (`deploy.sh release prod <old tag>`), or the
bad migration runs again. Copying keeps the server name, Key Vault secret and Terraform state as
they are; pointing the apps at the restored server instead would leave Terraform managing the
wrong one.

## Restoring blobs

One blob (portal: container → "Show deleted blobs" → Undelete, or):

```bash
A=stimovaprodwpq4p; C=listing-images
az storage blob list --account-name $A --auth-mode key -c $C --include d \
  --query "[?deleted].{name:name,deletedOn:properties.deletedTime}" -o table
az storage blob undelete --account-name $A --auth-mode key -c $C -n <name>
```

Everything deleted after a moment (e.g. after a bad cleanup job): the same list filtered on
`properties.deletedTime > '<UTC time>'`, then `undelete` each name. A deleted container: portal →
Containers → "Show deleted containers" → Undelete.

## Restore test — 2026-11-24

**Database.** Prod at 15:07 UTC had 7 accounts, 1 listing, 3 photos, 30 refresh tokens. Restored
to **2026-11-23 14:34:00 UTC**, chosen between two events 20 seconds apart: the first photos
(14:33:52–53) and the first listing (14:34:14). The restored server had:

- 4 accounts / 4 publishers, **0 listings**, 1 admin audit entry — exactly what prod had at 14:34;
- the newest row at 14:33:53 — nothing after the restore time;
- one photo and three refresh tokens that existed at 14:34 and have since been **deleted** in prod
  (that is what a restore is for);
- the same last migration (`AddTermsAcceptance`) and reference data (40 amenities, 1684 localities).

Restore took 6 minutes (15:07:55 → 15:14:02). The test server was deleted afterwards.

**Blobs.** A probe file uploaded to `listing-images`, deleted (gone from the container, listed as
deleted with 6 days left), undeleted, downloaded — identical to the original — and deleted again.
