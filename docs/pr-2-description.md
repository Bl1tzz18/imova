# PR 2 — Agencies: management UI, paged agency listings, deactivate / delete, admin verification

**Branch:** `feat/agency-management` → `main`
**Title:** Agencies (2/4): agency management in /account, deactivate / delete, admin verification

Roadmap item 3 ("Agency publishers"), steps 6–7 of `docs/agency-publishers-spec.md`. Where this PR
departs from the spec, `docs/agency-progress.md` records the decision (it takes precedence).
Statistics come in PR 3 and CSV import in PR 4. After this PR an agency can be created, edited,
staffed, deactivated, deleted and verified entirely from the UI.

## Summary

- `/account` has an "Agențiile mele" tab: invitations waiting for the account (accept / decline), the
  agencies it belongs to, and "Creează o agenție".
- Each agency has management pages under `/account/agencies/[id]`: Profil (logo, profile with a live
  preview), Membri (roles, removal with a choice of who takes over the listings, invitations), Anunțuri
  (every listing of the agency, paged by the API), Setări (public link, deactivate / reactivate, leave,
  delete).
- Deactivating an agency hides it and every one of its listings from the public, through one shared
  rule; reactivating brings them back. Deleting keeps its listings as their authors' private listings.
- Site admins verify agencies at `/admin/agencies`; the owners get an email.
- Outside the agency work: profile pictures up to 5 MB now really upload (a server action stopped them
  at 1 MB), and the docs name `https://imova.md` as the production address.

## Data model and migrations

No migrations. `Agency` gains `Deactivate` / `Reactivate` / `Verify` / `Unverify` (the columns existed
since PR 1). `Listing.ChangeAgency` now clears `ExternalRef` when the agency changes — an external
reference belongs to the agency's own system.

## API

| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `/api/v1/agencies/{id}/listings` | members, site admins | One page of one tab: `?group=active\|unpublished\|ended&q=&sort=recommended\|newest\|priceAsc\|priceDesc&page=&pageSize=` (default 24, max 60) → `AgencyListingsPageDto` with every tab's count and needs-attention flag (after the search). Owners/Admins: all listings; an Agent: their own. 403 non-member, 404 unknown/hidden. |
| POST | `/api/v1/agencies/{id}/deactivate` | Owner, Admin, site admin | → `AgencyDto`. Idempotent. |
| POST | `/api/v1/agencies/{id}/reactivate` | Owner, Admin, site admin | → `AgencyDto`. |
| DELETE | `/api/v1/agencies/{id}` | Owner, site admin | Body `{confirmName}` = the exact name, else 400 `agency.confirmNameMismatch`. Listings become private (AgencyId and ExternalRef cleared); members, invitations, former slugs and the logo files are removed. 204. |
| GET | `/api/v1/admin/agencies` | site admins (`RequireAdmin`) | `?q=&verified=&page=&pageSize=` (≤ 50) → `PagedResult<AdminAgencyDto>`; not yet verified first, then newest. |
| POST | `/api/v1/admin/agencies/{id}/verify` | site admins | 204; emails the Owners (confirmed addresses, best effort) only when it changes. |
| POST | `/api/v1/admin/agencies/{id}/unverify` | site admins | 204; no email. |

**What a deactivated agency hides** — one rule, `ListingVisibility` (Active, and not under a
deactivated agency): search and map, `GET /listings`, the listing page (404 to outsiders; the author,
the agency's managers and admins still see it), similar listings, view counting, phone reveal,
reports, starting a conversation; favorite alerts wait until the agency is back (saved-search alerts go
through search). The agency's own page and directory card were already hidden (PR 1).

The Anunțuri tab's grouping, search and order — the same rules as "Anunțurile mele" — run on the
server over a light row per listing (`AgencyListingGroups`); only the page shown is built into
`ListingDto`s, so an agency with hundreds of listings stays cheap.

## Screens (web)

- **`/account?tab=agencies`** — invitations with Accept / Decline and a count badge on the tab; my
  agencies (logo, name, role, verified, deactivated); "Creează o agenție" (needs a confirmed email). The
  account tabs now keep `?tab=` in the address.
- **`/account/agencies/new`** — the profile form; on success to the agency's page, which asks for a logo.
- **`/account/agencies/[id]`** — header (logo, role, status, public link) and four tabs as routes:
  - **Profil** — logo upload / replace / remove (route handler, up to 5 MB); name, phone, email,
    website, city, address, bio with a counter; a live preview of how the agency shows; Save off until
    something changed; a warning before leaving with unsaved changes (closing the tab or following a
    link). Agents see the details read-only.
  - **Membri** — role per member as `AgencyAccess` allows (the last Owner stays Owner; stepping down
    asks first), removal with "who takes over the listings" (default as the API), invite form, open
    invitations with resend / revoke and a warning when the email failed.
  - **Anunțuri** — tabs Active / Nepublicate / Încheiate with counts and an attention dot, search, sort,
    "Anunțurile mele"'s rows with each listing's author, 24 per page with numbered pages, "Adaugă anunț"
    on this agency. Everything in the address (`?tab=&q=&sort=&page=`).
  - **Setări** — public link with copy; deactivate (confirmed) / reactivate; leave (with the heir
    choice; the last Owner is told why not); delete (type the name; the button stays off until it
    matches).
- **`/admin/agencies`** — search, all / unverified / verified, verify (at once) / remove verification
  (confirmed), pages; in the account menu for admins ("Verificare agenții").
- The invitation page's "accepted" now links to the agencies tab; "Ai o agenție?" on the login and
  register pages goes to `/account/agencies/new`.
- Shared: `components/ui/ConfirmDialog` (native `<dialog>`). New i18n namespaces `AgencyManage`,
  `AdminAgencies` (ro/en/ru).

## Decisions taken over the spec

- Spec routes `/account/agentii/...`, `/admin/agentii` → English routes (`/account/agencies/...`,
  `/admin/agencies`), as everywhere else.
- The agency's tabs are separate routes, not `?tab=`, so the listing list keeps its own `?tab=`.
- No separate public `GET /agencies/{id}/listings?status=all`: the management list is members-only;
  the public page uses search's `agencyId` (PR 1).
- Spec puts "Părăsește agenția" under Membri; it's under Setări, next to the other "this agency and me"
  actions.
- The Anunțuri tab is paged on the server (24 per page). "Anunțurile mele" itself stays unpaged.
- Verification isn't written to `AdminAuditEntries` (that table's target is a user); it's logged.
- No toast component exists in the app; confirmations are inline notices, as elsewhere.

## How it was tested

Backend: 1,312 unit + 269 integration + 5 architecture tests. New:
`AgencyListingsHandlerTests` (17: access, paging without overlap, past-the-end, page size cap, default
tab, recommended order, expiring soon, diacritic search with counts), `AgencyLifecycleTests` (16:
domain, deactivate/reactivate access, visibility incl. the listing page, delete with name / roles /
private listings / blobs, verify email once, admin list), `AgencyLifecycleEndpointsTests` (4: deactivate
hides from search / listing page / agency page / phone reveal and reactivate restores; outsiders 403/401;
delete; admin-only verify and list), `ProfilePictureEndpointsTests` (2). Frontend: 430 Vitest
(`manage.test.ts`, `uploadSize.test.ts`, `admin/agencies.test.ts`); `tsc` clean.

In the browser (compose stack, 1280 / 390 / 360 px): created an agency, logo errors and upload, profile
edits; invited Ion (cooldown message), Ion accepted from /account at 390 px, the remove dialog with the
heir choice, Ion left at 360 px; the Anunțuri tab with 40 listings over two pages, search and sort;
deactivated Casa Ta (public 404s, search 0, owner still sees the listing) and reactivated it; deleted a
throwaway agency with a logo (both files gone); `/admin/agencies` as a temporary admin, verified Casa Ta
(email in Mailpit) and removed the verification; profile picture of 4.35 MB uploaded and removed. Three
bugs found that way were fixed before committing: React's reset after an `action=` submit put the
controlled selects back to their first option, the phone field's `<label>` opened the country menu,
and the profile picture's 1 MB limit.

## Known issues and follow-ups

- Integration tests use their own database but the same Azurite container as local development, so
  their uploads (agency logos, profile pictures) pile up there. 24 such logo files were removed by hand.
- The Anunțuri tab loads a light row for every listing of the agency to group and sort them; fine for
  hundreds or a few thousand, but if agencies grow to tens of thousands it should move into SQL.
- Listing expiry reminders still go out for listings of a deactivated agency.
- A deactivated agency's ended listings still show their 410 summary page.
- Profile pictures are still stored as uploaded (not resized), as before.

## Also in this branch (not agency work)

- `e5dac11` — the imova.md domain: `docs/development.md` names `https://imova.md` as the production
  `SITE_URL`, `App:WebBaseUrl` and CORS origin; a geocoding comment no longer calls it a placeholder.
- `12c81c3` — profile pictures through a route handler (5 MB, like the API) instead of a server action
  (1 MB); a failed upload now shows an error instead of hanging.
