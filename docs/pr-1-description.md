# PR 1 — Agencies: model, management API, members & invitations, publishing as an agency, public pages

**Branch:** `feat/agencies` → `main`
**Title:** Agencies (1/4): agency model, members and invitations, publishing as an agency, public agency pages

Roadmap item 3 ("Agency publishers"), steps 1–5 of `docs/agency-publishers-spec.md`. Where this PR
departs from the spec, `docs/agency-progress.md` records the decision (it takes precedence). The
management UI, deactivation and admin verification come in PR 2 (steps 6–7), statistics in PR 3 and
CSV import in PR 4.

## Summary

- An agency is now its own aggregate (`Agency`) with members (Owner / Admin / Agent), instead of a
  kind of publisher. A listing always has a person as its author; `Listing.AgencyId` says which agency
  it is published under, and its author must be a current member.
- Agencies can be created, edited and given a logo through the API; members are invited by email,
  change roles, leave or are removed, and their agency listings are handed to someone who stays.
- The listing form can publish under an agency ("Publică în numele"); an agency's Owners and Admins
  manage all its listings, Agents their own.
- Every agency has a public page (`/agencies/{slug}`) and there is a directory (`/agencies`); a
  listing's contact card links to its agency.
- Two fixes outside the agency work ride along (content-filter timeouts, the phone header), listed
  at the end.

## Data model and migrations

| Migration | What it does |
|---|---|
| `AddAgencies` (one-way) | New tables `Agencies` (name, unique `Slug`, logo blob name, bio, phone, email, website, address, `RaionId`, `IsVerified`/`VerifiedAt`, `Status` Active/Deactivated, created/updated, `CreatedByUserId`), `AgencyMembers` (agency, user, role, joined), `AgencySlugHistory` (former slugs → agency). `Listings` gains `AgencyId` (FK, `ON DELETE SET NULL`, indexed) and `ExternalRef` (unique per agency, for the CSV import later). `PublisherType` is removed — a publisher is always the person. Every existing agency publisher became an `Agency` **with the same Id**, its user the Owner; its listings moved to that user's own Individual publisher with `AgencyId` set. `Down()` throws — restore a backup instead. |
| `AddAgencyInvitations` | `AgencyInvitations`: agency, lower-cased email, role (Admin/Agent), SHA-256 of the token (the raw token is only in the email), expiry (7 days), accepted/declined/revoked timestamps, inviter, last sent. |
| `AddAgencyInvitationEmailFailedAt` | `AgencyInvitations.EmailFailedAt` — set when the invitation email couldn't be sent, cleared on a successful resend. |

Step 5 adds no migration.

## API

All routes are under `/api/v1`. Errors carry language-neutral codes (`ErrorCodes`), translated in the
web app (`Errors.codes.*` in ro/en/ru).

**Agencies**

| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `/agencies` | signed in, confirmed email | Creates the agency with the caller as Owner; slug from the name (`-2`, `-3`… on a clash; a slug taken at the same moment is retried once). At most `Agencies:MaxOwnedPerUser` (3) owned agencies. Codes: `agency.emailNotConfirmed`, `agency.limitReached`, `agency.websiteInvalid`, `agency.raionUnknown`. |
| GET | `/agencies/{id}` | public | `AgencyDto` with `memberCount`, `activeListingCount`, `isVerified`, the caller's `myRole`. The phone only for members/admins; others get its shape (`phonePrefix`, `phoneHiddenDigits`). Deactivated → 404 for non-members. |
| GET | `/agencies/by-slug/{slug}` | public | Same DTO by slug (case-insensitive). A slug the agency used before a rename → **301** with `Location` and `{ "slug": current }`. |
| GET | `/agencies` | public | Directory: `?q=&raionId=&verified=&page=&pageSize=` (≤ 50). Active agencies only; verified first, then most active listings, then name. `q` matches the name, or (spelled like a slug) the slug — so it works without diacritics and in Cyrillic. Returns `PagedResult<AgencyCardDto>`. |
| PUT | `/agencies/{id}` | Owner/Admin, site admin | Full profile update. A rename moves the slug and keeps the old one as a redirect; renaming back reclaims it. |
| POST / DELETE | `/agencies/{id}/logo` | Owner/Admin, site admin | Multipart upload, checked by its bytes (JPEG/PNG/WebP, ≤ 5 MB, refused only if the longer side is under 200 px — `agency.logoType`, `agency.logoTooSmall`); stored as 512 and 128 px square JPEGs on white; the previous blobs are deleted. |
| POST | `/agencies/{id}/contact/phone` | public | The full phone number, one request at a time; shares the listing phone-reveal rate limit (`RateLimiting:PhoneReveal`). |
| GET | `/users/me/agencies` | signed in | The caller's agencies with their role and status (deactivated ones too). |

Access rules (`AgencyAccess`): a deactivated agency doesn't exist for outsiders (404 before any 403);
a signed-in member whose role isn't enough gets 403. Owners/Admins edit; only Owners add, remove,
promote or demote Owners and Admins; anyone may step down or leave; an agency always keeps an Owner
(`agency.lastOwner`).

**Members and invitations**

| Method | Route | Who |
|---|---|---|
| GET | `/agencies/{id}/members` | members |
| PATCH | `/agencies/{id}/members/{userId}` | Owner/Admin (rules above) — change role |
| DELETE | `/agencies/{id}/members/{userId}` | Owner/Admin, or the member leaving — their agency listings go to the Owner/Admin named in the request (default: the remover; when leaving: the longest-standing Owner, then Admin; `agency.reassignInvalid`) |
| POST | `/agencies/{id}/invitations` | Owner/Admin (Admins invite Agents only) — `{email, role}`; emails a 7-day link |
| GET | `/agencies/{id}/invitations` | Owner/Admin — waiting invitations, with `emailFailedAt` |
| DELETE | `/agencies/{id}/invitations/{invitationId}` | Owner/Admin — revoke |
| POST | `/agencies/{id}/invitations/{invitationId}/resend` | Owner/Admin — new token, new expiry |
| GET | `/invitations/{token}` | public — agency, role, inviter, status |
| POST | `/invitations/{token}/accept` | signed in; the account email must match (`agency.invitationWrongAccount`) |
| POST | `/invitations/{token}/decline` | public (no sign-in needed to say no) |
| GET | `/users/me/invitations` | signed in — invitations to the caller's email |
| POST | `/users/me/invitations/{invitationId}/accept` · `/decline` | signed in |

Limits: 50 waiting invitations per agency (`agency.tooManyInvitations`), 20 sent per hour per agency
(`agency.invitationRateLimit`), 10 minutes between resends of one invitation
(`agency.invitationResendTooSoon`); inviting a member → 409 (`agency.alreadyMember`); expired or
closed links → `agency.invitationExpired` / `agency.invitationClosed`. A failed email is recorded on
the invitation instead of failing the request.

**Listings**

- `POST /listings` takes `agencyId` (null = private) instead of `publisherId`; the author is always
  the caller, who must be a member of an active agency (`listing.notAgencyMember` 403,
  `listing.agencyInactive` / `listing.agencyUnknown` 400).
- `PUT /listings/{id}` requires `agencyId` (missing → 400). Putting a listing into an agency is the
  author's call (`listing.agencyChangeAuthorOnly`); taking it out (to private or another agency)
  needs an Owner or Admin of the agency it leaves (`listing.agencyLeaveManagerOnly`), so an Agent
  can't take agency listings with them.
- An agency's Owners and Admins manage all its listings and see their statistics; Agents their own.
- `ListingDto.Agency` (`id, name, slug, logoUrl (128 px), isVerified`, never a phone) on every view,
  plus `activeListingCount` on the detail view only.
- `GET /listings/search` and `/search/map` accept `agencyId`.
- `POST /publishers/agency` is gone (agencies are created through `/agencies`).

**Account**

- Account deletion follows the authorship rule: agency listings are handed to an Owner/Admin who
  stays; only personal listings are deleted. The last Owner can't delete their account while the
  agency has other members (`account.lastAgencyOwner`, also checked before a deletion link is
  emailed); a sole member's agency is deleted with the account.
- The personal-data export lists the user's agency memberships.

## Screens (web)

- **Listing form, step 5 "Contact":** "Publică în numele" — Persoană fizică or each active agency the
  user belongs to (with its logo); remembered per browser, `?agencyId=` preselects. Shown only to
  agency members. On edit: shown on an agency listing to its Owners/Admins (other agencies only to
  the author), on a private listing to the author.
- **Edit listing:** open to the agency's Owners/Admins.
- **`/invitations/[token]`:** agency, role, inviter; sign in / register and come back; wrong account
  explained; expired/revoked; "agency currently inactive" notice; accept/decline.
- **`/agencies`:** directory — name search, city, "Doar verificate", cards (logo or initials,
  verified badge, city, active listings), pagination; filtered pages are `noindex`.
- **`/agencies/[slug]`:** logo or initials, verified badge ("Agenție verificată de IMOVA"), city,
  "Pe IMOVA din {lună an}", active listing count, bio (folded when long), phone behind "Arată",
  email, website; the agency's listings with Toate / Vânzare / Chirie, property type, sort and
  pagination; empty states; a former slug redirects permanently (308) keeping the filters; title
  "{Name} — agenție imobiliară în {City} | IMOVA", description from the bio, Open Graph with the
  logo, canonical URL, JSON-LD `RealEstateAgent`. Unknown or deactivated → 404 (members see a
  "deactivated" notice instead, `noindex`).
- **Listing page contact card:** "Agent imobiliar" + an agency box (logo, name, badge, "Vezi toate
  anunțurile agenției (N)") linking to its page.
- **Messaging:** inbox, thread header and the first-message page say "Agent · Agency".
- **Moderation:** queue and reported-listings rows say "Agent · Agency".
- **Header and footer:** an "Agenții" link (desktop nav and footer).

All UI text in ro/en/ru.

## Decisions taken over the spec

- `/api/v1/...` routes and English page routes (`/agencies`, `/invitations/{token}`), not `/agentii`.
- Agency listings come through search with `agencyId` — no separate `/agencies/{id}/listings`.
- Error codes translated in the web app rather than Romanian backend messages.
- Logos: upload only, JPEG output (not WebP), refused only when the longer side is under 200 px.
- When a member leaves or is removed, their agency listings go to an Owner/Admin who stays (the
  authorship rule), rather than "staying with the agency" without an author.
- Phone and email required on an agency; the email is public, the phone behind a reveal.
- Agency pages are not in a sitemap yet (KAN-21 is separate).
- `DateTimeOffset` throughout; the 24-hour view dedupe stays (stats come in PR 3).

## How it was tested

- **Backend:** 1,279 unit + 262 integration + 5 architecture tests, all passing (the integration tests
  run against the real compose Postgres + Azurite). New in this PR: agency domain and slug rules,
  create/update/logo handlers, access rules, members, invitations (limits, cooldown, tokens, wrong
  account, expiry), listing authorship and agency moves, account deletion with agencies, the data
  export, the public page / directory / phone reveal handlers, and endpoint tests for every route
  above (including the 301 after a rename and the `agencyId` search filter).
- **Frontend:** 401 Vitest tests, `tsc --noEmit` clean — pure logic for "Publică ca", the invitation
  page states, the public pages' URLs/filters/SEO/JSON-LD, and "Agent · Agency".
- **Browser** (compose stack, rebuilt): the listing form filled in by hand as an agency Owner and
  published under the agency, approved, contact card and messaging showing the agency; the
  invitation page; `/agencies` and `/agencies/[slug]` at 1280, 390 and 360 px (search, filters,
  phone reveal, verified badge, the 308 from a former slug, the listing card link); the moderation
  queue and first-message page; the header at 360/390 px in all three languages, signed in and out.
  Demo data created for the checks was removed afterwards.

## Known issues and follow-ups

- Until PR 2 (step 7), a deactivated agency's listings still show in search and on the map; only its
  page, directory card and phone reveal are hidden. Deactivate/reactivate/delete and admin
  verification have no endpoints yet.
- No management UI yet (create/edit agency, members, invitations) — PR 2. The invitation page's
  "accepted" state links to `/account` until the "Agențiile mele" tab exists.
- Agency phone reveals aren't counted (statistics — PR 3).
- The directory search is a substring match; words in another order don't match.
- A rename that races another agency for the same new slug isn't retried (create is) and would 500.
- Repeated resends of one invitation aren't counted by the hourly limit; the 10-minute cooldown caps
  them at about 6 an hour.
- `GET /publishers/mine` still exists (the form uses it for the contact phone default).
- Product decision pending (not in this PR): whether an owner may delete an Active listing from
  "Anunțurile mele" (today only "mark as sold/rented" and "deactivate").

## Also in this branch (not agency work)

- Messaging: a content-filter rule that times out no longer fails the send — the message is
  delivered and flagged "Filter timeout" for moderation; the rules now run `NonBacktracking` and are
  warmed up at start-up (the cold first match was what made two tests flaky under load).
- Header: fits one row on phones down to 360 px — the logo no longer shrinks, the language is a
  compact select and "Autentificare" a person icon below 640 px, and "Adaugă anunț" stays on one
  line (Russian shows the shorter "Разместить" there). Desktop unchanged.
