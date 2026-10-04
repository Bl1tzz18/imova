# Agency publishers — progress

Work on roadmap item 3 ("Agency publishers"), specified in `docs/agency-publishers-spec.md`.
Branch `feat/agencies` (from `main` at `593b393`), not pushed yet. Last updated 2026-10-05 (after step 4).

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

Tests at the end of step 4: backend 1,250 unit + 253 integration + 5 architecture, frontend 385
Vitest; `tsc` clean. Checked in a browser at 390 px and 1280 px: the invitation page (step 3), the
picker on the create form (shown for an agency member, choice remembered across a reload) and on
the edit form of an agency listing (starts on the agency; the submitted form carries it). The picker
was checked by un-hiding step 5 with a script rather than filling steps 1–4.

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

**Import (PR 4)**
- CSV import only for verified agencies; their imported listings go live without the review queue
  (admins can still moderate after). Unverified agencies see the Import tab in an "available after
  verification" state.
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
  action is needed; the product owner decides. Edit files with Edit/Write (not Bash) so the hook's
  first-touch check applies.
- No Claude attribution in commits or PRs.

## Open items for later steps

**Step 5 — public pages (next)**
- `GET /api/v1/agencies/by-slug/{slug}` (old slug → current slug for a 301), directory
  `GET /api/v1/agencies`, agency phone reveal endpoint (rate-limited like the listing one).
- `agencyId` filter in `SearchQueryString`/`ListingSearch`.
- Pages `/agencies` and `/agencies/[slug]` (SEO: title, description, Open Graph, canonical, JSON-LD
  `RealEstateAgent`); contact-card link and "Vezi toate anunțurile agenției (N)"; header/footer link.
  No sitemap (see decisions).

**Step 6 — management UI (PR 2)**
- `/account` "Agențiile mele" tab (`?tab=agencies`): my agencies, create button, my pending invitations
  with Accept/Decline (`GET /api/v1/users/me/invitations`, accept/decline by id exist). The invitation
  page's "accepted" state should link to this tab (it links to `/account` for now).
- `/account/agencies/new` and `/account/agencies/[id]`: Profil (with logo uploader, confirm-email hint
  on `agency.emailNotConfirmed`), Membri (roles, remove with the heir choice, invite, pending
  invitations with resend/revoke and the `emailFailedAt` warning), Anunțuri, Setări.

**Step 7**
- Deactivate/reactivate/delete endpoints (`Agency` has no domain methods for these yet) and the
  hiding listed under decisions; admin verify/unverify + `/admin/agencies`; "verification granted" email.

**Later PRs and wrap-up**
- PR 3: stats (daily aggregate, `GET /api/v1/agencies/{id}/stats`, Statistici tab).
- PR 4: CSV import (see decisions).
- Playwright end-to-end path: create agency → upload logo → publish as agency → public page → back
  from the listing's contact card.
- Update `CLAUDE.md` "Current status" and the PR descriptions (every endpoint, migration and screen).

## Known issues and notes

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
- The compose containers were rebuilt at step 4 (all migrations applied); rebuild `backend`,
  `worker` and `frontend` again after the next code change, before a browser check.

## Exact next step

Step 5 of PR 1: public pages. Start with `GET /api/v1/agencies/by-slug/{slug}` (an old slug answers
with the current one, for a 301), the directory `GET /api/v1/agencies` (`?q=&raionId=&verified=&page=`,
verified first, then by active listing count) and a rate-limited agency phone reveal; add the
`agencyId` filter to `SearchQueryString`/`ListingSearch`; then the pages `/agencies` and
`/agencies/[slug]` (SEO metadata and JSON-LD, no sitemap), the contact-card link with "Vezi toate
anunțurile agenției (N)", and the header/footer link — with unit, integration and Vitest tests and a
browser check at 390/360 px and 1280 px; then stop with build + tests green, commit, update this
file and summarize.
