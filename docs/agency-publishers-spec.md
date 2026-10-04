# IMOVA — Agency Publishers (full implementation spec)

> docs/agency-progress.md records later decisions and takes precedence where they differ.

Roadmap item 3 (also item 10 on the listing-page list). Implement **everything** below in one feature branch. Nothing is deferred.

Stack: ASP.NET Core API, EF Core + PostgreSQL, JWT + Google auth, Azure Blob Storage (Azurite locally), existing web frontend. **Before writing code, read the existing Agency entity, its create/read endpoints, the listing entity, the listing create flow, the image upload flow, the /account page and the listing-page contact card. Follow their existing patterns (folder layout, DTO style, validation, error format, auth attributes, UI components, i18n).** Where this spec conflicts with an existing convention, keep the convention and note it in the PR.

All user-facing text in Romanian (and Russian too, if the app already has RU strings).

---

## 0. Definition of done

- An agency can be created, edited, verified, deactivated and deleted entirely from the UI. Nothing requires calling the API by hand.
- An agency owner can invite other agents, manage their roles and remove them.
- A listing can be published as a private person or as an agency the user belongs to.
- Every agency has a public page at `/agentii/{slug}` with its active listings, and a public directory at `/agentii`.
- Listing pages link to the agency from the contact card.
- Agency members see a stats dashboard for their agency.
- Agencies can bulk-import listings from CSV.
- Migrations, tests and the PR description are complete; the build and all tests pass.

---

## 1. Data model (EF Core migration)

### Agency (extend the existing entity)
| Field | Type | Notes |
|---|---|---|
| Id | existing | |
| Name | string(120), required | |
| Slug | string(140), required, **unique index** | generated from Name (see §2.4) |
| LogoUrl | string?, | blob URL of the processed logo |
| Bio | string(2000)? | plain text, rendered with line breaks, no HTML |
| Phone | string(32)? | normalised to E.164 (+373…) |
| Email | string(254)? | validated |
| Website | string(254)? | must be http(s) |
| Address | string(250)? | free text |
| City | string(80)? | |
| IsVerified | bool, default false | **only admins can change it** |
| VerifiedAt | DateTime? | |
| Status | enum: Active, Deactivated | default Active |
| CreatedAt / UpdatedAt | DateTime (UTC) | |
| CreatedByUserId | FK → User | |

### AgencyMember (new)
| Field | Type | Notes |
|---|---|---|
| AgencyId | FK | composite PK (AgencyId, UserId) |
| UserId | FK | |
| Role | enum: Owner, Admin, Agent | |
| JoinedAt | DateTime | |

Rules: every agency has **at least one Owner**. A user can belong to several agencies.
Migration: for every existing agency, insert an Owner row for `CreatedByUserId` (or whatever field currently stores the creator). If the creator is unknown, log it and leave the agency for an admin to assign an owner.

### AgencyInvitation (new)
| Field | Type | Notes |
|---|---|---|
| Id | Guid | |
| AgencyId | FK | |
| Email | string | lower-cased |
| Role | Admin or Agent | never Owner |
| TokenHash | string | SHA-256 of a random 32-byte token; the raw token only goes in the email link |
| ExpiresAt | DateTime | 7 days |
| AcceptedAt / RevokedAt | DateTime? | |
| InvitedByUserId | FK | |

### Listing (extend)
- `AgencyId` (nullable FK → Agency, `ON DELETE SET NULL`), index on it.
- `PublishedByUserId` stays the actual author.
- If the existing listing already has an agency relation, reuse it instead of adding a new column.

### Stats
If the app does not already track listing views and contact actions, add:
- `ListingEvent` (Id, ListingId, AgencyId?, Type: View | PhoneReveal | Message | Favorite, CreatedAt, VisitorHash?) with indexes on (AgencyId, CreatedAt) and (ListingId, CreatedAt).
- Count a view at most once per visitor per listing per 30 minutes (dedupe by hashed IP + user agent, or by user id when logged in). Do not store raw IPs.

### Import
- `ListingImportJob` (Id, AgencyId, UploadedByUserId, FileName, Status: Pending | Processing | Completed | Failed, TotalRows, CreatedCount, UpdatedCount, FailedCount, ErrorReportUrl?, CreatedAt, FinishedAt).
- `Listing.ExternalRef` (string(100)?, unique per AgencyId) so re-importing the same file updates instead of duplicating.

---

## 2. Backend

### 2.1 Authorization
Add a policy/helper `IAgencyAuthorization` with:
- `CanView(agency)` — anyone, if Active; members and admins also if Deactivated.
- `CanEdit(agency)` — Owner or Admin member, or site admin.
- `CanManageMembers(agency)` — Owner or Admin. Only Owners can add/remove/demote Owners or Admins.
- `CanDelete(agency)` — Owner or site admin.
- `CanPublishAs(agency)` — any member, agency Active.
- `CanVerify` — site admin only.

Return 404 (not 403) for deactivated agencies to non-members. Return 403 for authenticated members missing the right role.

### 2.2 Endpoints

**Agencies**
| Method | Route | Who | Notes |
|---|---|---|---|
| POST | /api/agencies | logged-in user | exists — update it: generate slug, create Owner membership in the same transaction |
| GET | /api/agencies/{id} | public | exists — also add `memberCount`, `activeListingCount`, `isVerified` |
| GET | /api/agencies/by-slug/{slug} | public | used by the public page; if the slug was changed, return 301 info (see §2.4) |
| PUT | /api/agencies/{id} | CanEdit | **new** — full update of editable fields (not IsVerified, not Slug unless Name changed) |
| POST | /api/agencies/{id}/logo | CanEdit | multipart upload (see §2.3) |
| DELETE | /api/agencies/{id}/logo | CanEdit | removes logo + blob |
| POST | /api/agencies/{id}/deactivate | CanEdit | hides page and its listings from search |
| POST | /api/agencies/{id}/reactivate | CanEdit | |
| DELETE | /api/agencies/{id} | CanDelete | requires `{ "confirmName": "<exact name>" }`; listings stay but become private-person listings of their authors (`AgencyId = null`); memberships, invitations and logo blob removed |
| GET | /api/agencies | public | directory: `?q=&city=&verified=&page=&pageSize=` (max 50), sort verified first, then by activeListingCount desc |
| GET | /api/me/agencies | logged-in | agencies I belong to, with my role |

**Members & invitations**
| Method | Route | Who |
|---|---|---|
| GET | /api/agencies/{id}/members | members |
| PATCH | /api/agencies/{id}/members/{userId} | CanManageMembers — change role |
| DELETE | /api/agencies/{id}/members/{userId} | CanManageMembers, or the user leaving themselves |
| POST | /api/agencies/{id}/invitations | CanManageMembers — `{ email, role }`; sends email |
| GET | /api/agencies/{id}/invitations | CanManageMembers — pending ones |
| DELETE | /api/agencies/{id}/invitations/{invId} | CanManageMembers — revoke |
| POST | /api/agencies/{id}/invitations/{invId}/resend | CanManageMembers — new token, new expiry |
| GET | /api/invitations/{token} | public — agency name, role, inviter, expired? |
| POST | /api/invitations/{token}/accept | logged-in; email must match the account email (case-insensitive) |

Rules:
- Cannot remove or demote the last Owner. Error message: "Agenția trebuie să aibă cel puțin un proprietar."
- When a member leaves or is removed, their listings published under the agency stay with the agency (the agency owns the inventory). Document this in the UI confirmation.
- Ownership transfer: an Owner can promote another member to Owner, then step down.
- Max 50 pending invitations per agency; rate-limit invitations to 20/hour per agency.
- Inviting an existing member returns 409.

**Admin**
| Method | Route |
|---|---|
| POST | /api/admin/agencies/{id}/verify |
| POST | /api/admin/agencies/{id}/unverify |
| GET | /api/admin/agencies | list with filters (unverified first) |

**Listings**
- Create/update listing accepts `agencyId?`. Validate `CanPublishAs`. Null = private person.
- Listing read DTO includes `agency: { id, name, slug, logoUrl, isVerified, phone }` or null.
- Changing a listing's agency later is allowed for the author if they are a member of both (or moving to null).
- GET /api/agencies/{id}/listings — public, active listings only, `?type=sale|rent&category=&page=&pageSize=&sort=newest|price_asc|price_desc`. Members also get `?status=all` to see drafts/inactive.
- Search: exclude listings of Deactivated agencies.

**Stats**
- POST /api/listings/{id}/events — `{ type }` for PhoneReveal / Message / Favorite if not already tracked server-side; views are recorded server-side when the listing is served.
- GET /api/agencies/{id}/stats?from=&to= — members only. Returns totals (views, phone reveals, messages, favorites), per-day series, and top 10 listings by views. Default range: last 30 days; max 365.

**Import**
- GET /api/agencies/{id}/import/template — CSV template with headers + one example row.
- POST /api/agencies/{id}/import — multipart CSV (max 5 MB, max 2,000 rows), CanEdit. Creates a job, processes it in a background service (`IHostedService` + `Channel<T>` queue is enough; no new infra).
- GET /api/agencies/{id}/import/{jobId} — status + counts.
- GET /api/agencies/{id}/import/{jobId}/errors — CSV of failed rows with an `error` column.
- CSV columns: `external_ref, title, description, deal_type (sale|rent), property_type, price, currency (MDL|EUR|USD), area_m2, rooms, floor, total_floors, city, sector, address, latitude, longitude, image_urls (separated by |), status (active|inactive)`. Map to existing listing fields; extend the list if the listing has more required fields.
- Upsert by `(AgencyId, ExternalRef)`. Images: download from URLs (http/https only, max 20 per listing, max 10 MB each, image MIME types only, 10 s timeout), push through the existing image pipeline. One failed image does not fail the row; note it in the error report.
- Accept UTF-8 (with or without BOM) and Windows-1250/1251; `,` or `;` delimiters (auto-detect).

### 2.3 Logo upload
- Accept JPEG, PNG, WebP; max 5 MB; validate by magic bytes, not just extension.
- Process with the same image library the listing images use (or ImageSharp): strip metadata, resize to fit 512×512 keeping aspect ratio, pad to square on white, save as WebP; also a 128×128 version.
- Blob path: `agencies/{agencyId}/logo-{timestamp}.webp`. Delete the previous logo blob after the new one is saved.

### 2.4 Slugs
- Lowercase, transliterate Romanian/Russian (ă→a, â→a, î→i, ș/ş→s, ț/ţ→t, Cyrillic → Latin), non-alphanumerics → `-`, collapse/trim dashes, max 140 chars.
- On collision append `-2`, `-3`, ….
- Reserved: `new`, `edit`, `admin`, `api`, `invitatie`.
- When Name changes, regenerate the slug and store the old one in an `AgencySlugHistory` table (AgencyId, Slug). `by-slug` lookups of an old slug return the current slug so the frontend can 301-redirect.

### 2.5 Validation & errors
- FluentValidation (or whatever the project uses) for all inputs; Romanian error messages.
- Phone: accept `0xx xxx xxx`, `+373…`, `373…`; store E.164.
- Trim all strings; empty strings → null.
- Uniform error format matching existing endpoints.

### 2.6 Emails
Use the existing email service; if none exists, add an `IEmailSender` interface with a dev implementation that logs to console and a real one behind configuration.
- Invitation: "{Inviter} te-a invitat să te alături agenției {Agency} pe IMOVA", button → `/invitatie/{token}`.
- Verification granted: "Agenția {Agency} a fost verificată."
- Import finished: counts + link to the error report if any.

---

## 3. Frontend

### 3.1 /account → "Agențiile mele"
- Section listing the user's agencies (logo, name, role, verified badge, link to manage) and a "Creează agenție" button.
- If the user has no agency: short explanation + create button.
- Pending invitations for the user's email are shown here with Accept / Decline.

### 3.2 Create / edit agency (`/account/agentii/nou`, `/account/agentii/{id}`)
Tabs or sections:
1. **Profil** — name, bio (character counter), phone, email, website, address, city; logo uploader with preview, crop-free (server pads it), remove button; live preview card showing how the contact card will look. Save button disabled while unchanged; unsaved-changes warning on navigation.
2. **Membri** (Owner/Admin) — table: name, email, role (dropdown), joined; remove button with confirm. Invite form (email + role). Pending invitations with resend/revoke. "Părăsește agenția" for non-last-owners.
3. **Anunțuri** — the agency's listings incl. inactive/drafts, with filters, edit links, and a "Adaugă anunț" button pre-selecting this agency.
4. **Statistici** — date-range picker (7/30/90 days, custom), four KPI tiles, a daily line chart (views) and a top-listings table. Empty state when there is no data yet.
5. **Import** — download template, upload CSV, job progress (poll every 3 s until done), results, error-report download, history of past jobs.
6. **Setări** — public page link with copy button, deactivate/reactivate, delete (type the agency name to confirm).

After creation, redirect to the edit page with a success toast and a prompt to add a logo.

### 3.3 Listing create/edit
- If the user belongs to ≥1 active agency, show "Publică ca": `Persoană fizică` / each agency (with logo). Default: the last choice (remember it per user), or the `?agencyId=` query param.
- If the user has no agency, show nothing (no extra friction).

### 3.4 Public agency page `/agentii/{slug}`
- Header: logo (or initials placeholder on a coloured background), name, verified badge with tooltip "Agenție verificată de IMOVA", city, short bio (expandable), contact buttons (call, email, website). Phone hidden behind "Arată numărul" and tracked as a PhoneReveal event.
- Stats strip: number of active listings, "Pe IMOVA din {lună an}".
- Listings grid reusing the existing listing card component, with filters (vânzare/chirie, tip proprietate), sort, pagination.
- Empty state: "Agenția nu are anunțuri active momentan."
- Old slug → 301 to the current slug. Deactivated or unknown → 404 page.
- SEO: `<title>{Name} — agenție imobiliară în {City} | IMOVA</title>`, meta description from bio (first 155 chars), Open Graph with logo, canonical URL, JSON-LD `RealEstateAgent`. Add agency pages to the sitemap if one exists.

### 3.5 Agency directory `/agentii`
- Search box, city filter, "doar verificate" toggle, grid of agency cards (logo, name, badge, city, active listing count), pagination. Link it from the footer and the main nav ("Agenții").

### 3.6 Listing page contact card
- When the listing has an agency: agency logo + name (link to `/agentii/{slug}`), verified badge, "Vezi toate anunțurile agenției (N)" link. Keep the existing contact actions.
- When private: unchanged, labelled "Persoană fizică".

### 3.7 Invitation page `/invitatie/{token}`
- Shows agency, role and inviter. Not logged in → login/register then return here. Logged in with a different email → explain and offer to switch accounts. Expired/revoked → clear message.

### 3.8 Admin
- `/admin/agentii`: table with filters (unverified first), verify/unverify buttons, link to public page. Hidden from non-admins (and protected server-side).

### 3.9 General UI
- Mobile-first; works at 360 px width.
- Loading skeletons, error states and toasts consistent with the rest of the app.
- Accessible: labels on all inputs, focus states, alt text on logos ("Logo {Name}").

---

## 4. Tests

**Backend (xUnit + WebApplicationFactory, real PostgreSQL via Testcontainers if the project already uses it):**
- Create agency → creates Owner membership and unique slug; collision gets `-2`.
- Edit: Owner/Admin succeed; Agent 403; non-member 403; anonymous 401.
- Last Owner cannot leave or be demoted.
- Invitation: create, accept with matching email, reject mismatched email, expired, revoked, duplicate member 409.
- Publish listing as agency: member OK, non-member 403, deactivated agency rejected.
- Deactivated agency: public page 404, its listings excluded from search, members still see it.
- Delete agency: listings remain with `AgencyId = null`; blobs removed.
- Logo: rejects wrong type / too large / fake extension; replaces old blob.
- Slug rename → old slug resolves to new.
- Admin-only verify.
- Stats: view dedupe within 30 minutes; stats only for members.
- Import: valid file creates listings; re-import updates by external_ref; bad rows reported; `;` delimiter and Windows-1251 handled.

**Frontend:** component tests for the agency form, "Publică ca" selector and contact card; one end-to-end (Playwright) path: create agency → upload logo → publish listing as agency → open public page → see listing → click through from the listing's contact card.

---

## 5. Order of work (each step should leave the app building and tests passing)

1. Migration: Agency fields, AgencyMember (+ backfill owners), AgencySlugHistory, Listing.AgencyId/ExternalRef.
2. Authorization helper + updated create endpoint + PUT edit + logo upload/delete.
3. Members, invitations, emails, invitation page.
4. Listing `agencyId` support (API + "Publică ca" UI) and listing DTO agency block.
5. Public agency page, directory, contact-card link, SEO, sitemap.
6. /account "Agențiile mele" + full management UI (Profil, Membri, Anunțuri, Setări).
7. Deactivate/delete, admin verification UI.
8. Event tracking + stats endpoint + Statistici tab.
9. CSV import (background job, template, error report) + Import tab.
10. Tests, cleanup, PR description listing every endpoint, migration and screen.

---

## 6. Out of scope for this item

Automatic sync from external CRMs (feed URL / ImmoFlux / Bitrix24 connectors) and paid agency plans. Those are separate roadmap items that build on the tables above.
