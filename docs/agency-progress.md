# Agency publishers — progress

Work on roadmap item 3 ("Agency publishers"), specified in `docs/agency-publishers-spec.md`.
**PR 1 (steps 1–5, branch `feat/agencies`) was merged on 2026-10-06:
https://github.com/Bl1tzz18/imova/pull/32.** **PR 2 (steps 6–7, `feat/agency-management`) was merged
on 2026-10-09: https://github.com/Bl1tzz18/imova/pull/33.** **PR 3 (statistics + the 3-photo rule) was
merged** (#34). **PR 4 (CSV import) was merged** (#35, `a994bb2`). **Then the product owner decided
(2026-10-12): importing agencies' listings is a manual service by IMOVA** — the CSV import is a site
admin's tool only, and an agency just writes to us (branch `feat/admin-only-import`, see "Import becomes
a manual service" below); automating it for agencies is an after-launch item.
Last updated 2026-10-12. Next: the wrap-up items under "Open items" (Playwright path, CLAUDE.md).

**Pushed now:** commits are no longer amended — a fix to a pushed step is a new commit on top.

## Steps done (PR 1 = steps 1–5)

| Step | Commit | What it delivered |
|---|---|---|
| 1 | `74875aa` | `Agency` aggregate (`Imova.Domain/Agencies/`): profile, slug + `AgencySlugHistory`, `AgencyMember` (Owner/Admin/Agent), `Status` (Active/Deactivated), `IsVerified`. `Listing.AgencyId` (FK, set null on delete) and `Listing.ExternalRef` (unique per agency). `Publisher` is always the person (`PublisherType` removed). One-way migration `AddAgencies`: every agency publisher became an `Agency` with the same Id, its user the Owner; its listings moved to the owner's own publisher with `AgencyId` set. |
| 2 | `46a9e2d` | `POST/GET/PUT /api/v1/agencies[/{id}]`, `POST/DELETE /api/v1/agencies/{id}/logo`. `AgencyAccess` (who may do what; 404 for a hidden agency before any 403). Slugs: numbered on a clash, move on rename (old one kept as a redirect target), reclaimed when renamed back; a slug taken at the same moment is retried once (`IDatabaseErrors`). Logo: checked by its bytes, JPEG/PNG/WebP, max 5 MB, made into 512 and 128 px square JPEGs on white (Magick). Confirmed email required to create; at most `Agencies:MaxOwnedPerUser` (3) owned agencies. |
| 3 | `e701985` | Members: list, change role, remove/leave with the leaver's agency listings handed to an Owner/Admin who stays; last-Owner rule. Invitations: by email (7-day link, only the SHA-256 of the token stored), resend/revoke, accept/decline by link or from "my invitations", failed email recorded (`EmailFailedAt`). Page `/invitations/[token]`. Account deletion follows the authorship rule. Migrations `AddAgencyInvitations`, `AddAgencyInvitationEmailFailedAt`. |
| — | `2386dcc` | Not part of the agency work: a content-filter regex timeout now delivers the message flagged "Filter timeout" instead of failing the send with a 500. |
| — | `aea2f0c` | The spec committed, with a note that this file takes precedence. |
| — | `90a2190` | Not part of the agency work: content-filter rules run `NonBacktracking` and are warmed up at API start-up (the cold first match of the SMS/card rule took 71 ms idle and passed 200 ms under load — the cause of both load-sensitive test failures). Slowest rule now ~0.16 ms on a 2,000-character message; first match after warm-up ≤ 0.6 ms; timeout unchanged at 200 ms. Full suite green 3 runs in a row. |
| 4 | `4f4a4f5` | `agencyId` replaces `publisherId` on create (author = the caller; member of an active agency only — `listing.notAgencyMember` 403, `listing.agencyInactive`/`agencyUnknown` 400). On edit `agencyId` is **required** (null = private; missing → 400) and only the author changes it (`listing.agencyChangeAuthorOnly`). `ListingAccess`: an agency's Owners/Admins manage all its listings (and see their stats), Agents only their own. `ListingDto.Agency` (id, name, slug, 128 px logo, verified — no phone). `GET /api/v1/users/me/agencies`. Web: "Publică în numele" picker in step 5 (remembered per browser, `?agencyId=` on `/properties/new`, hidden field keeps the agency when the picker isn't shown), edit page open to the agency's Owners/Admins. Conversations show "Agent · Agency". |
| 4 | `9580c66` | Follow-up: taking a listing **out of** an agency (to private, or to another agency) needs an Owner or Admin of the agency it leaves (`listing.agencyLeaveManagerOnly`, 403) — an Agent can't take agency listings with them before leaving; putting one **into** an agency stays with its author, a member of that active agency (`listing.agencyChangeAuthorOnly`). The edit form's picker follows: on an agency listing only its Owners/Admins see it (other agencies offered only to the author); on a private listing only the author. |
| — | `0dc2330` | Docs: the step 4 end-to-end browser check. |
| 5 | `404262e` | Public pages. API: `GET /api/v1/agencies/by-slug/{slug}` (current slug → `AgencyDto`; a former slug → **301** + `Location` + `{slug}`; unknown, or deactivated for a non-member → 404; slugs matched lower-cased), the directory `GET /api/v1/agencies` (`?q=&raionId=&verified=&page=&pageSize=` ≤ 50 → `PagedResult<AgencyCardDto>`; Active agencies only; verified first, then most Active listings, then name; `q` matches the name case-insensitively **or**, spelled like a slug (`AgencySlug.SearchKey`), the slug — so "agentia", "Ușor"/"usor" and Cyrillic all find it), `POST /api/v1/agencies/{id}/contact/phone` (`{phone}`; shares the listing phone-reveal rate limit `RateLimiting:PhoneReveal`; not counted — stats are PR 3). Search: `agencyId` (`SearchQueryString` → `SearchListingsQuery.AgencyId` → `ListingSearch`). `ListingAgencyDto.ActiveListingCount` on the listing detail view only (null on cards). Web: `/agencies` (GET form: name, city select, "Doar verificate"; select/checkbox submit by themselves; cards with logo or initials, badge, city, "N anunțuri active"; pagination; filtered/later pages `noindex`), `/agencies/[slug]` (logo or initials, verified badge with tooltip, city · "Pe IMOVA din {lună an}", N active listings, bio folded after ~4 lines, phone reveal via the shared `ContactActions`, email, website; listings: Toate/Vânzare/Chirie tabs + type + sort + pagination through search `agencyId`; empty states; former slug → Next `permanentRedirect` (308) keeping the filters; title "{Name} — agenție imobiliară în {City} \| IMOVA", description from the bio, Open Graph with the logo, canonical, JSON-LD `RealEstateAgent` without a phone; deactivated → notice + noindex for members). Listing contact card: agency box (logo, name, badge, "Vezi toate anunțurile agenției (N)") linking to the page. Header nav (desktop) + footer link. Moderation rows and the first-message page say "Agent · Agency" (`authorLabel` in `lib/listing/contactCard.ts`). `Avatar` gained `shape="square"`; new `components/agency/` (VerifiedBadge, AutoSubmitForm, ExpandableText, LinkPagination); `lib/agency/publicPage.ts` (Vitest). |
| — | `0638f38` | Not part of the agency work: the header fits one row on phones down to 360 px (logo never shrinks, compact language select and a person icon for "Autentificare" below 640 px, "Adaugă anunț" on one line; Russian uses the short "Разместить" there). |
| 5 | `76f352b` | Follow-up: Russian "На IMOVA: {date}" (the month comes in its standalone form, which reads wrong after "с"). |

Tests at the end of step 4 (with the follow-up): backend 1,253 unit + 254 integration + 5
architecture, frontend 386 Vitest; `tsc` clean. Before the follow-up: 1,250 unit + 253 integration, 385
Vitest; `tsc` clean. Checked in a browser at 390 px and 1280 px: the invitation page (step 3), the
picker on the create form (shown for an agency member, choice remembered across a reload) and on
the edit form of an agency listing (starts on the agency; the submitted form carries it). The picker
was checked by un-hiding step 5 with a script rather than filling steps 1–4.

**Step 4 end-to-end browser check (2026-10-06, rebuilt containers, no scripts):** as Elena, filled all
five steps of the form by hand (Garage for sale, Chișinău/Ciocana, one photo, 9,500 €), chose "Casa Ta
Imobiliare" under "Publică în numele" → saved PendingReview with `AgencyId` = Casa Ta. Approved in
`/admin/moderation` by Ion with a temporary Admin role (removed right after, Ion signed out and in
again). Listing page (1280 and 390 px, no horizontal scroll): contact card "Elena Ciobanu · Agent
imobiliar · Casa Ta Imobiliare". Ion wrote from "Scrie mesaj": his inbox and thread header say
"Elena Ciobanu · Casa Ta Imobiliare"; Elena's inbox and thread (390 px) show "Ion Popescu" with the
unread badge. All as specified. Cleaned up: the listing deleted through the API as Elena (204, photo
blobs with it), the conversation and its message deleted in SQL (deleting a listing keeps its
conversations), Ion has no Admin role. Noticed then, and what became of it:
- The moderation row showed only the author, not the agency → fixed in step 5 ("Agent · Agency").
- The first-message page said "Mesajul ajunge la Elena Ciobanu" → fixed in step 5 ("… · Casa Ta Imobiliare").
- The header was cramped at 390 px (existing, not from this branch) → fixed in `0638f38`.
- An Active listing can't be deleted from "Anunțurile mele" (only marked sold / deactivated) — existing
  behaviour, kept on purpose; see "Product decisions for later".

Tests at the end of step 5: backend 1,279 unit + 262 integration + 5 architecture, frontend 401
Vitest; `tsc` clean (new: `AgencyPublicPageTests` 20, `AgencySlugTests.SearchKey` 4, 2 in
`SearchQueryStringTests`, `AgencyPublicEndpointsTests` 8, `publicPage.test.ts` 13, `authorLabel` 2).

**Step 5 browser check (2026-10-06, rebuilt containers):** `/agencies` at 1280, 390 and 360 px (search
"casa" + "Doar verificate", count "1 agenție", reset link; no horizontal scroll); `/agencies/casa-ta-imobiliare`
at 1280, 390 and 360 px signed out and as Elena (a member sees the full number at once; the public
reveals it with "Arată"); the type select submits itself (5 apartments), "Chirie" tab (2); verified badge
and city checked by setting them in SQL for a moment and setting them back; a former slug inserted in
SQL answered `308 → /agencies/casa-ta-imobiliare?transactionType=Rent` (removed after); the listing
page's contact card shows the agency box "Vezi toate anunțurile agenției (10)"; as Ion with a
temporary Admin role (removed after): `/admin/moderation?tab=active` rows read "Elena Ciobanu · Casa Ta
Imobiliare · Chișinău, Botanica" and `/messages/new` reads "Mesajul ajunge la Elena Ciobanu · Casa Ta
Imobiliare". Header at 360/390 px in ro/en/ru, signed out and in (button ends 16 px from the edge),
and 1280 px (unchanged); the phone language select switches language. Dev data left as it was.

## Steps done (PR 2 = steps 6–7, branch `feat/agency-management`)

| Step | Commit | What it delivered |
|---|---|---|
| — | `e5dac11` | Not part of the agency work: the imova.md domain is bought. `docs/development.md` now names `https://imova.md` as the production `SITE_URL`, `App:WebBaseUrl` and CORS origin (the stale "CORS is locked in Program.cs" line points to the `Cors:AllowedOrigins` section); the `GeocodingOptions` comment no longer calls the domain a placeholder. The committed `UserAgent` defaults (code and `appsettings.json`, Nominatim and Photon) and the email sender `no-reply@imova.md` already used the domain — nothing else to change. The gitignored `appsettings.Development.json` was changed locally only: `IMOVA/1.0 (dev; +https://imova.md)`. |
| 6 | `423a062` | Management UI. API: `GET /api/v1/agencies/{id}/listings` (every status; Owners/Admins/site admins all, an Agent their own; 403 non-member, 404 unknown/hidden; newest first). Web: `/account?tab=agencies` "Agențiile mele" (invitations with Accept/Decline + a count badge on the tab, my agencies, "Creează o agenție" only with a confirmed email; the account tabs now keep `?tab=` in the address). `/account/agencies/new` (profile form → `/account/agencies/{id}?created=1`, which asks for a logo). `/account/agencies/[id]` layout (logo, role, status, public link) + tabs as routes: Profil (logo via the route handler `/account/agencies/[id]/logo` — 5 MB logos don't fit a server action's 1 MB; form with live preview, bio counter, Save off until changed, leave-page warning incl. in-app links; Agents read-only), `/members` (role select per `AgencyAccess`, last Owner fixed, stepping down confirmed, remove with heir choice defaulting like the API, invite form, open invitations with resend/revoke and the `emailFailedAt` warning), `/listings` (`OwnerListingsList` with `showAuthor` + agency empty texts, "Adaugă anunț" → `/properties/new?agencyId=`), `/settings` (public link + copy, leave with heir choice; last Owner told why not). Invitation page "accepted" → the agencies tab; login/register "Ai o agenție?" → `/account/agencies/new`. Shared `components/ui/ConfirmDialog`; rules in `lib/agency/manage.ts`; namespace `AgencyManage` (ro/en/ru); `agency.logoTooSmall` text now says "the longer side". |

| — | `12c81c3` | Not part of the agency work: profile pictures up to 5 MB really upload. A 4.35 MB phone photo failed ("Body exceeded 1 MB limit" — server actions stop at 1 MB) and the page hung on "Se încarcă...". Now `/account/profile-picture` (route handler, POST/DELETE) like the agency logo, both through `lib/api/imageUploadProxy.ts` with the limits in `lib/api/uploadSize.ts`; a failed request shows an error. Tests: `uploadSize.test.ts`, `ProfilePictureEndpointsTests` (4.35 MB → 200, 5 MB + 1 → 400). |
| 6 | `d9cbe5c` | Follow-up: the Anunțuri tab is paged by the API, 24 per page. `GET /api/v1/agencies/{id}/listings?group=&q=&sort=&page=&pageSize=` → `AgencyListingsPageDto` (items + every tab's count and needs-attention, after the search). "Anunțurile mele"'s rules moved to the server (`AgencyListingGroups`) over a light row per listing; DTOs only for the page. Web: `AgencyListingsList` (server-rendered; tabs as links, search + sort as a GET form, `OwnerListingRow` exported, `LinkPagination`). "Anunțurile mele" itself has no paging (it never had) and is unchanged. |
| 7 | `2c0360b` | Deactivate / reactivate / delete and admin verification. API: `POST /api/v1/agencies/{id}/deactivate|reactivate`, `DELETE /api/v1/agencies/{id} {confirmName}`, `GET /api/v1/admin/agencies`, `POST /api/v1/admin/agencies/{id}/verify|unverify` (email to the Owners). `ListingVisibility` (Active and not under a deactivated agency) used by search/map, `GET /listings`, the listing page, similar listings (SQL), views, phone reveal, reports, starting a conversation, favorite alerts. `Listing.ChangeAgency` clears `ExternalRef`. Web: Setări (deactivate confirmed / reactivate, delete by typing the name), `/admin/agencies` (+ account menu "Verificare agenții"). Error code `agency.confirmNameMismatch`; namespace `AdminAgencies`. |

Tests at the end of step 7 (PR 2 complete): backend 1,312 unit + 269 integration + 5 architecture,
frontend 430 Vitest; `tsc` clean.

**Step 7 browser check (2026-10-09, rebuilt containers):** Casa Ta deactivated from Setări as Elena
(confirm dialog; header badge "Dezactivată"; public: listing 404, agency page 404, search 0; Elena still
opens the listing) and reactivated (10 back in search). A throwaway agency created through the UI, logo
uploaded (2 files in Azurite), a misspelled name kept Delete off, the exact name deleted it → back on
the agencies tab, both logo files gone. `/admin/agencies` at 1280 and 390 px as Ion with a temporary
Admin role: Casa Ta verified (email in Mailpit to Elena), verification removed again (confirm dialog);
the role removed after. Paging (step 6 follow-up) with 40 listings (30 temporary copies, deleted): 24 +
16, search "botanica" (20), price order, 390 px. Azurite: 24 unreferenced agency-logo files (12 agency
ids — "Test Pas Șase Imobil" and earlier test runs) deleted by hand; nothing under `agencies/` now.
Dev data as before: Casa Ta active, not verified, 10 listings; Ion has only the User role.

Tests at the end of step 6: backend 1,286 unit + 263 integration + 5 architecture, frontend 418 Vitest;
`tsc` clean (new: `AgencyListingsHandlerTests` 7, one integration test in `AgencyListingEndpointsTests`,
`manage.test.ts` 17).

**Step 6 browser check (2026-10-09, rebuilt containers):** as Elena at 1280 px: the agencies tab, created
"Test Pas Șase Imobil" through the form (the leave warning fired on the back link and was dismissed),
landed on Profil with the logo prompt; a 100×50 PNG → the translated "longer side" error; a 600×300 PNG
uploaded; bio + city edits saved (Save off before and after). Found and fixed during the check: after a
save the city select showed "Nespecificat" while the state kept the city — React's automatic reset after
an `action=` submit; forms now submit through `onSubmit` + `startTransition` (verified: the city stays).
Also fixed: the phone field was wrapped in a `<label>` (a click on the label opened the country menu),
and the hidden file input was announced as a second upload button. Invited Ion as Administrator
(resend at once → the 10-minute cooldown message); as Ion at 390 px: the tab badge and invitation card,
accepted, then his Membri page (only his own role selectable, invite limited to Agents). With one of
Ion's listings put into the agency in SQL for a moment: Elena's remove dialog offered "Elena Ciobanu ·
Proprietar" as heir (cancelled), and the Anunțuri tab showed it with "de Ion Popescu" and its stats
(reverted right after). Ion at 360 px: Setări, no horizontal scroll, left the agency → back on his
agencies tab. Elena's Setări: public link + copy, "you're the only owner". Profil at 390 px. Cleaned up:
the test agency deleted in SQL (members/invitations/slug history cascade; no listings) — its two logo
JPEGs remain in Azurite (dev only). Casa Ta Imobiliare is again the only agency.

## Steps done (PR 3 = statistics + the 3-photo rule, branch `feat/agency-stats`)

| Step | Commit | What it delivered |
|---|---|---|
| — | `c28be5a` | Every listing needs at least 3 photos (`ListingPhotoRules.MinPhotos`), private sellers and agencies alike: create, submit/resubmit, edit while in review/live/rejected/suspended, re-publish, renew, admin approval → 400 `listing.notEnoughPhotos`; deleting below 3 on a live/in-review listing → 400 `listing.lastPhotos`; drafts waiting for email confirmation only go to review with the photos. Existing live listings stay live (decided: enforce on next change) and show "Adaugă fotografii" (→ edit `?step=photos`) as their next step / needing attention. Photos step: rule, live counter, 3-segment bar, recommends 8+, blocks Continue/Save with fewer or with uploads running. |
| 1/4 | `17e3b4e` | `ListingDailyStats` (listing × Moldovan day: Views, PhoneReveals), upserted in the same SQL as the visitor mark and the totals (`ListingCounters`); `AgencyPhoneReveals` (agency × day × visitor, ON CONFLICT DO NOTHING; members not counted; cleanup after 400 days). `MoldovaTime`. Migration `AddListingDailyStatsAndAgencyPhoneReveals`. |
| 2/4 | `ee0a95b` | `GET /api/v1/agencies/{id}/stats?from=&to=` → `AgencyStatsDto` (Totals + Previous period, Daily, TopListings 10, NeedsAttention: FewPhotos / NoContacts (20+ views, no contact) / ExpiringSoon). Contacts = listing reveals + agency-page reveals + conversations started. Agents: own listings, no agency reveals. |
| 3/4 | `ba326d6` | Statistici tab (`/account/agencies/[id]/stats`, fifth tab; tab labels `text-xs` on phones): period 7/30/90 (`?days=`), contacts tile first (split, contact rate, change vs previous), views + saves tiles, two single-series daily bar charts (hover/tap/arrow keys readout, "Vezi tabelul"), "De îmbunătățit" with one fix per reason (add photos / review / renew in place), top listings (table on desktop, cards on phones), empty state. `lib/agency/stats.ts` (Vitest); namespace `AgencyStats`. |
| 4/4 | — | **Skipped for now** (product owner, 2026-10-11): the weekly stats email to Owners — see "Product decisions for later". |

Tests at PR 3 complete: backend 1,338 unit + 272 integration + 5 architecture, frontend 444
Vitest; `tsc` clean.

**Statistici browser check (2026-10-09, rebuilt containers):** as Elena (Owner of Casa Ta) at 1280 px,
390 px and 360 px (3×), with 60 days of made-up `ListingDailyStats` rows for Casa Ta's 10 listings, one
listing's expiry moved to 4 days ahead and another's reveals zeroed: KPIs and comparisons, both charts at
30 and 90 days (90 bars fit at 360 px), hover readout, table view, the improve list with all three
reasons — "Prelungește" renewed the listing and it left the list — top-listings table / phone cards, five
tabs fit at 360 px, no horizontal scroll. Fixed: the chart total's number grouping. Reverted after: all
`ListingDailyStats` rows deleted (the table is empty in dev), listing 100012's `ExpiresAt` back to
2027-03-30 22:53:28.781741+00.

## Steps done (PR 4 = CSV import, branch `feat/agency-import`)

First on the branch, as its own small commit: `91132cc`, the "De îmbunătățit" list in its intended
order (few photos, then no contacts, then expiring soon).

| Step | Commit | What it delivered |
|---|---|---|
| 1/6 | `c485129` | `ListingImportJob` / `ListingImportRow` (migration `AddListingImports`), `Photo.SourceUrl`; `ImportColumns` per property type (attributes by reflection, required-ness from `PropertyAttributesValidator.RequirementOf`), templates with a valid example row, `GET …/import/template` and `…/import/columns`. CsvHelper. |
| 2/6 | `58cd3ac` | `ImportCsvReader` (UTF-8/16, Windows-1250/1251, `;`/`,`/tab, strict header, 2,000 rows), `ImportValues`, `ImportRowParser` → `ImportListingCommand` checked by the form's `ListingWriteValidator`; `ImportLocationIndex`; coordinates within 10 km else geocoded (`ImportGeolocator`); author rule (`ImportContext`). `import.*` error codes. |
| 3/6 | `41e63d0` | Upload (`POST …/import`, 5 MB, 409 one at a time — `AddOneListingImportAtATime`), history / job / rows / report.csv; Worker job `ListingImports` (row by row, own save per row, counts, "not in this file", email, stuck → Failed after 2 h, files deleted after 30 days); `ImportListingWriter` (`PublishImported`, `ImportDeactivated`, `AssignExternalRef`), `ImportPhotoSync`; site-admin onboarding (author = longest-standing Owner); agency deletion removes import files; data export v3; the README per type. |
| 4/6 | `83b01d8` | `ImportImageFetcher` (destination checked at connect time — `PublicAddress`; 3 redirects re-checked; 10 MB; 20 s); orphaned downloads deleted when a row's save fails; `SharedGeocodingService` — one limit in Postgres for API + Worker (`GeocoderThrottle`) and a cache (`GeocodedAddresses`, 180 / 7 days), migration `AddSharedGeocodingCache`. |
| 5/6 | `39653f7` | Web: "Importă din CSV" on the Anunțuri tab, `/account/agencies/[id]/import` (prepare, upload, history) and `/import/[jobId]` (progress, counts, translated problems, report). |
| 6/6 | (this commit) | The result page refreshes on every status change (the badge stayed "În așteptare" while processing); `GeocodingStoreTests` made load-independent (cold connections skewed the waits — it failed once in a full run); docs + PR description. |

Tests at PR 4 complete: backend 1,500 unit + 276 integration + 5 architecture, frontend 455 Vitest;
`tsc` clean.

**Real run (2026-10-11, compose + Mailpit + real internet), as Elena:** Casa Ta verified temporarily.
File 1 (5 rows: apartment with coordinates — kept; garage without — geocoded; commercial with a
non-member `agent_email` and a 404 photo; land with coordinates 10+ km off — ignored and geocoded;
a house broken on purpose) → 4 Active listings (#100051–#100054) with 3 sized photos each, 1 failed
row, 3 warnings; result page, report CSV, Mailpit email ("… 1 rânduri cu erori"), listing page with
the real photos, 1280 px and 390 px. File 2 (one price 79 000 → 76 500) → 1 updated, "3 anunțuri
active … nu erau în acest fișier", a `ListingPriceChanges` row, photos kept (not re-downloaded),
second email. **Undone:** the 4 listings deleted through the API (photo blobs too), both import jobs and
their files in Azurite deleted, Casa Ta back to `IsVerified = false` / `VerifiedAt = NULL` with its 10
listings. Left on purpose: the 5 real answers in `GeocodedAddresses` (dev cache).

## Import becomes a manual service (branch `feat/admin-only-import`, from `main` at `a994bb2`)

Decided 2026-10-12: we don't know yet how agencies keep their data, so importing is something IMOVA does
by hand; automating it waits until after launch.

- **Agencies:** no import page, upload, template, guide or import email any more. The Anunțuri tab (for
  Owners/Admins) shows only "Importă anunțurile tale" — three steps and a "Scrie-ne" `mailto:` button to
  `IMPORT_CONTACT_EMAIL` (`lib/agency/importContact.ts`, the one place the address lives; the subject
  names the agency). Every `/api/v1/agencies/{id}/import/*` route is `RequireAdmin()` (members get 403,
  `ImportAccess` checks again); `/account/agencies/[id]/import` is gone (404).
- **Site admins:** the whole CSV import stays — upload, the Worker job, photo downloads (SSRF-safe),
  the shared geocoding limit and cache, templates and the README per type, results and report — now at
  `/admin/agencies/[id]/import` (own admin layout), reached from `/admin/agencies` ("Import CSV").
- **Removed:** the "Import finalizat" email (`ListingImportEmails`) — the admin follows the import on its
  result page. The personal data export keeps `listingImportsYouUploaded` (now an admin's uploads).
- **Never shipped (branch `feat/import-requests`, deleted):** import requests (agency upload form,
  statuses, `/admin/imports`, their files and emails, the 90-day clean-up) and the "draft waiting for
  photos" idea. The dev database was rolled back to `AddSharedGeocodingCache`.
- **Tests:** the import tests run as a site admin; new checks that the agency's Owner, Admin, Agent and
  strangers get 403 on the import routes and on the templates/columns, that `/import-requests` doesn't
  exist (404), and `importContact.test.ts` (the `mailto:` link). Fixed in passing: a date time bomb in
  `ImportRowParserTests`.

## Before launch

- Change the import contact email (`IMPORT_CONTACT_EMAIL`, now `catalinfilip47@gmail.com`) to an
  `@imova.md` address — „schimbă emailul de contact pentru import cu o adresă @imova.md”.

## Decisions (made by the product owner in this session)

**Model and authorship**
- Separate `Agency` aggregate, migrated from agency publishers with the same Id; `PublisherType.Agency` removed.
- Authorship rule: an agency listing's author must always be a current member of that agency.
  Owners/Admins manage every listing of the agency; Agents only their own.
- When a member is removed or leaves, their agency listings go to an Owner/Admin chosen in the UI
  (default: the person doing the removal; when someone leaves: the longest-standing Owner, then Admin).
  Existing conversations stay where they are; new ones go to the new author.
- Account deletion: agency listings are reassigned by the same rule; only personal listings are
  deleted. The last Owner cannot delete their account while the agency has other members
  (`account.lastAgencyOwner`, also checked before a deletion link is emailed); if they are the only
  member, the agency and its listings are deleted with the account.
- A listing's agency can be changed on edit (private ↔ an agency the author belongs to) — step 4.
  Taking it OUT of an agency (to private or another agency) requires an Owner or Admin of that
  agency, not just the author — so an Agent can't take agency listings with them before leaving.
  Putting a private listing INTO an agency stays with the author, who must be a member.
- Conversations show "Agent name · Agency name" for agency listings, like the listing page — step 4.

**Agencies**
- Phone and email are required on an agency. Email is public (business contact); the phone is only
  for members/admins and otherwise behind a rate-limited reveal (agency-level endpoint, step 5).
- Creating an agency needs a confirmed email (`agency.emailNotConfirmed`, with a hint in the UI — the
  create form is step 6).
- An account owns at most 3 agencies, configurable as `Agencies:MaxOwnedPerUser` (`agency.limitReached`).
- City is `RaionId` (same raion list as listings).
- Logos: upload only (no outside URLs), JPEG, padded square, 512 and 128 px, existing Magick
  pipeline; max 5 MB; refused only when the LONGER side is under 200 px (a 600×150 text logo is fine).
- Slugs for agencies migrated with non-Latin names ("agentie-N"): fine, there is no production data.

**Invitations**
- Limits: 50 waiting per agency, 20 sends per hour per agency (both counted in the database), plus a
  10-minute cooldown per invitation (proposed by Claude, accepted). Inviting a member → 409.
- Declining by the emailed link needs no sign-in. Accepting into a deactivated agency is allowed, and
  the invitation page says the agency is currently inactive.
- A failed invitation email is recorded on the invitation and shown in the members UI (step 6) with
  a resend action.

**Import (PR 4, then 2026-10-12)**
- **Importing is a manual service by IMOVA (2026-10-12):** agencies write to us; only site admins run
  the CSV import. Automatic import for agencies: after launch (see "Product decisions for later").
- CSV import only for verified agencies; their imported listings go live without the review queue
  (admins can still moderate after).
- DB-backed queue processed by the Worker (no in-memory channel).
- Locations resolved with the location typeahead's normalisation; per-property-type CSV columns (one
  template per type is fine); Contact defaults to the agency's contact details when a row has none.
- Image URLs: block private/loopback/link-local addresses, re-check every redirect, enforce
  size/type/timeout limits.

**Stats (PR 3)**
- Daily aggregate written in the same statement as the existing counters; keep the 24-hour dedupe
  (not the spec's 30 minutes). Favorites and messages come from their existing tables.

**Conventions over the spec**
- `/api/v1/...` routes, `/users/me/...` for the caller's own things, English page routes
  (`/agencies`, `/invitations/{token}`, `/account/agencies/...`, `/admin/agencies`).
- Error codes translated in the web app (`ErrorCodes` + `Errors.codes.*` in ro/en/ru), not Romanian
  backend messages.
- Agency listings come through search with an `agencyId` filter (no separate listings endpoint).
- `DateTimeOffset` everywhere; UI checked at 390 px and 360 px as well as desktop.
- Deactivation hides the agency's listings everywhere: search, map, listing page, similar listings,
  saved-search and favorite alerts.
- Reuse `PhoneNumberRules`.
- "Publică ca" choice remembered per browser (localStorage), no database column.
- Playwright for the one end-to-end path only; no component-test tooling; pure logic in Vitest.
- Do NOT touch the sitemap or KAN-21 in this work; agency pages are added to the sitemap later, when
  KAN-21 is done separately.

**Process**
- Four PRs: steps 1–5, 6–7, 8 (stats), 9 (import).
- One commit per step; fixes to a step are folded into that step's commit while it's unpushed.
- Stop after each step with build + tests green and a short summary.
- Never work around hooks: if a hook blocks something, stop and report what it blocked and why the
  action is needed; the product owner decides.
- **Never use sed, awk or any other shell command to edit files — not even one line. Edit/Write
  only** (so every change goes through the hook's checks and shows up as a reviewable edit). Shell
  commands may read files, build, test, and run git — never change file contents.
- No Claude attribution in commits or PRs.

## Product decisions for later

- **Automatic import for agencies — after launch,** once we know how agencies keep their data (a
  spreadsheet, a CRM export, a feed from their site). Until then IMOVA imports by hand with the admin
  CSV import. Ideas tried and dropped on 2026-10-12 (never shipped): agencies uploading the CSV
  themselves; "import requests" (upload any file + status + replies); listings imported without enough
  photos as drafts waiting for photos.
- **Weekly stats email to Owners, with opt-out — after launch, once agencies are active.** Planned as
  PR 3's step 4 and skipped (2026-10-11). Intended shape: a Worker job, once a week, to each Owner
  (confirmed email) of an active agency — the week's contacts / views / saves against the week before
  and the "De îmbunătățit" list with links; opt-out as an email preference like
  `EmailFavoriteUpdates` (account "Notificări" tab + a tokened unsubscribe link). The stats endpoint's
  handler already computes everything it needs.
- **The 3-photo rule for listings already live** (decided 2026-10-09): they stay live; the rule applies
  on their next edit, renewal or re-publishing.

- **Deleting an Active listing.** "Anunțurile mele" offers an Active listing only "Marchează ca
  vândut/închiriat" and "Dezactivează" — no delete (the API's `DELETE /api/v1/listings/{id}` exists).
  Kept as it is on purpose (2026-10-06); whether owners should be able to delete a live listing
  outright (and what happens to its conversations, favorites and reports) is to be decided later.

## Open items for later steps

**Step 6 — done** (`423a062`), see above.

**Step 7 — done** (`2c0360b`), see above.

**Later PRs and wrap-up**
- ~~PR 3: stats~~ — done (`feat/agency-stats`), without the weekly email (later, see above).
- ~~PR 4: CSV import~~ — done (`feat/agency-import`), see above.
- Playwright end-to-end path: create agency → upload logo → publish as agency → public page → back
  from the listing's contact card.
- Update `CLAUDE.md` "Current status" and the PR descriptions (every endpoint, migration and screen).

## Known issues and notes

**CSV import (PR 4)**
- **Nominatim's usage policy discourages bulk geocoding.** At hosting time, consider our own instance or
  a paid geocoder if large imports become frequent.
- Geocoding is ~1 address a second (shared with the form): a 2,000-row file without coordinates and
  with many new addresses takes about half an hour. The cache makes repeats instant.
- The report CSV is English (codes + English messages, for scripts); the result page is translated.
- Uploaded files are kept 30 days, in the private message-attachments container under `imports/`
  (a dedicated container can come with hosting).
- `Photo.SourceUrl` (where an imported photo came from) isn't shown anywhere.
- Integration tests leave small import files in the dev Azurite container (`imports/…`).

- The Anunțuri tab's API loads a light row for every listing of the agency to group, search and sort in
  memory (only the page's DTOs are built); fine for hundreds or a few thousand — move it into SQL if
  agencies get much bigger.
- Integration tests use their own database but the dev Azurite container, so their uploads (agency
  logos, profile pictures) pile up there; clean by hand now and then (a Shared Key script against the
  public Azurite dev key was used on 2026-10-09).
- Listing expiry reminders still go out for listings of a deactivated agency; its ended listings still
  show their 410 summary page.
- Verifying and unverifying an agency are written to `AdminAuditEntries` (since 2026-10-12: action
  `VerifyAgency` / `UnverifyAgency`, the admin, `TargetAgencyId`, the time — migration
  `AddAgencyVerificationAudit`; `TargetUserId` is now nullable). No UI reads them yet (SQL). The client
  IP isn't recorded for these (the verify endpoint doesn't pass it). Earlier verifications — e.g. Casa
  Ta's on 2026-10-11 at 20:02 — left only a log line, now gone with the recreated container.
  The migration's `Down()` would fail once agency entries exist (their `TargetUserId` is null).

- The two load-sensitive tests (`MessageContentFilterTests.SuspiciousMessages_AreFlaggedWithAReason`,
  `MessagingEndpointsTests.BlockedVisitor_Gets403_AndAReportReachesAdmins`) shared one cause — a cold
  content-filter regex passing its 200 ms timeout — fixed in `90a2190`; the full suite then passed 3
  runs in a row. (The second test's exact failing status wasn't captured: the cause is inferred from
  its two `Created` assertions both going through the filter, which threw on a timeout before
  `2386dcc`.) `RequestUploadUrlHandlerTests` was made timing-independent in step 2.
- `POST /api/v1/listings` ignores a stray `publisherId` (no longer part of the API); the author is
  always the caller — `ListingEndpointsTests.CreateListing_IsAlwaysTheCallersOwn_WhateverPublisherTheBodyNames`.
- `GET /api/v1/publishers/mine` and `lib/api/publishers.ts` still exist (the create form uses the
  caller's publisher for the contact step's phone default).
- A rename that races another agency for the same new slug is not retried (create is); it would 500.
- The per-agency hourly invitation count can't see repeated resends of one invitation; the 10-minute
  cooldown covers that (at most ~6 emails an hour to one address).
- The step 2 commit message says "at least 200x200"; the rule is now the longer side (step 3 commit).
- Dev database: one accepted invitation row for `ion.popescu@demo.imova.md` → Casa Ta Imobiliare is
  left as history (Ion was removed from the agency again). A dump taken before the `AddAgencies`
  migration was saved in the session scratchpad and may not survive.
- The compose containers were rebuilt at step 7 (all migrations applied; steps 5–7 added none); rebuild
  `backend`, `worker` and `frontend` again after the next code change, before a browser check. A
  frontend build once failed in `next/font` (fetching Google Fonts) and passed on a plain retry.
- The directory's `q` is a substring match (on the name, case-insensitive, and on the slug, which
  covers diacritics and Cyrillic) — words typed in another order ("imobiliare casa") don't match.
- Agency phone reveals are counted since PR 3 (`AgencyPhoneReveals`); daily statistics start empty on
  deployment (no backfill), so the first weeks compare against zeros.
- The stats endpoint groups the period's rows in memory (fine for hundreds of listings × 365 days).
- The agency page's sort form submits `sort=Newest` explicitly when changed (harmless; links built by
  the page leave it out).
- Dev database: the 36 empty "Imobil Grup" agencies (`imobil-grup`, `-2` … `-36`) and "Maria Imobil"
  — leftovers of old test runs, no listings, invitations or logo, only their creator as member — were
  deleted on 2026-10-06 (37 agencies with their member rows). Casa Ta Imobiliare is the only agency left.

## Exact next steps (in this order)

**a) ~~End-to-end browser check of step 4~~** — done 2026-10-06, see "Steps done" above.

**b) ~~Step 5 of PR 1: public pages~~** — done 2026-10-06 (`404262e`, `76f352b`; header `0638f38`).

**c) ~~The PR 1 description~~** — `docs/pr-1-description.md` (also mentions the header fix and the
home page's "Anunțuri moderate", `06be883`).

**d) ~~Open PR 1~~** — https://github.com/Bl1tzz18/imova/pull/32, against `main`, not merged.

**e) ~~PR 2, step 6: the management UI~~** — done 2026-10-09 (`423a062`, on `feat/agency-management`,
not pushed).

**f) ~~Step 7~~** — done 2026-10-09 (`2c0360b`), with the profile picture fix (`12c81c3`) and the paged
Anunțuri tab (`d9cbe5c`) before it.

**g) ~~The PR 2 description~~** — `docs/pr-2-description.md`.

**h) ~~Push and open PR 2~~** — https://github.com/Bl1tzz18/imova/pull/33, merged 2026-10-09.

**i) ~~PR 3: statistics + the 3-photo rule~~** — done 2026-10-11 (`c28be5a`, `17e3b4e`, `ee0a95b`,
`ba326d6`; description `docs/pr-3-description.md`), opened against `main`, not merged.

**j) ~~PR 4: CSV import~~** — done 2026-10-11 (`c485129` … step 6; description
`docs/pr-4-description.md`), opened against `main`, not merged.

**k) ~~Import becomes a manual service~~** — 2026-10-12, branch `feat/admin-only-import` (see above).

**l) Next — wrap-up:** the Playwright end-to-end path and the `CLAUDE.md` "Current status" update (see
"Open items"), then the launch items (KAN-10, KAN-21, hosting, and "Before launch" above).
