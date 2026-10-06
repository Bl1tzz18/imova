# Agency publishers — progress

Work on roadmap item 3 ("Agency publishers"), specified in `docs/agency-publishers-spec.md`.
Branch `feat/agencies` (from `main` at `593b393`), pushed to GitHub up to `231ffab` (no PR yet; the
commits from `0dc2330` on are local only). Last updated 2026-10-06 (after step 5 — PR 1 is complete).

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
  action is needed; the product owner decides.
- **Never use sed, awk or any other shell command to edit files — not even one line. Edit/Write
  only** (so every change goes through the hook's checks and shows up as a reviewable edit). Shell
  commands may read files, build, test, and run git — never change file contents.
- No Claude attribution in commits or PRs.

## Product decisions for later

- **Deleting an Active listing.** "Anunțurile mele" offers an Active listing only "Marchează ca
  vândut/închiriat" and "Dezactivează" — no delete (the API's `DELETE /api/v1/listings/{id}` exists).
  Kept as it is on purpose (2026-10-06); whether owners should be able to delete a live listing
  outright (and what happens to its conversations, favorites and reports) is to be decided later.

## Open items for later steps

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
- The compose containers were rebuilt at step 5 (all migrations applied; step 5 added none); rebuild
  `backend`, `worker` and `frontend` again after the next code change, before a browser check. A
  frontend build once failed in `next/font` (fetching Google Fonts) and passed on a plain retry.
- Until step 7, a deactivated agency's listings still appear in search, on the map and through
  `?agencyId=` — only its page, directory card and phone reveal are hidden.
- The directory's `q` is a substring match (on the name, case-insensitive, and on the slug, which
  covers diacritics and Cyrillic) — words typed in another order ("imobiliare casa") don't match.
- Agency phone reveals are rate-limited but not counted (agency statistics are PR 3).
- The agency page's sort form submits `sort=Newest` explicitly when changed (harmless; links built by
  the page leave it out).
- Dev database: 36 agencies named "Imobil Grup" (`imobil-grup`, `-2` … `-36`) and one "Maria Imobil",
  all without listings — left over from the agency publishers that old test runs wrote into the dev
  database before 2026-09-29. They fill the directory after Casa Ta; delete them when convenient.

## Exact next steps (in this order)

**a) ~~End-to-end browser check of step 4~~** — done 2026-10-06, see "Steps done" above.

**b) ~~Step 5 of PR 1: public pages~~** — done 2026-10-06 (`404262e`, `76f352b`; header `0638f38`).

**c) ~~The PR 1 description~~** — written into `docs/pr-1-description.md` (not pushed, no PR opened).

**d) Next:** the product owner pushes the branch and opens PR 1 (title and body from
`docs/pr-1-description.md`). Then **step 6 (PR 2): the management UI** — see "Open items" above:
the `/account` "Agențiile mele" tab with my agencies and my pending invitations, then
`/account/agencies/new` and `/account/agencies/[id]` (Profil with logo upload, Membri, Anunțuri,
Setări), each with tests and a browser check at 360/390/1280 px; stop after it with build + tests
green, commit, update this file.
