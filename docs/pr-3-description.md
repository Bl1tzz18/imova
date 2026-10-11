# PR 3 — Agencies: statistics, and at least 3 photos on every listing

**Branch:** `feat/agency-stats` → `main`
**Title:** Agencies (3/4): agency statistics (Statistici tab), and every listing needs at least 3 photos

Roadmap item 3 ("Agency publishers"), the statistics part of `docs/agency-publishers-spec.md`. Where
this PR departs from the spec, `docs/agency-progress.md` records the decision. CSV import comes in
PR 4. The weekly statistics email to Owners was planned for this PR and moved to after launch.

## Summary

- **Every listing needs at least 3 photos**, for private sellers and agencies alike. It's one server
  rule, checked wherever a listing is created, sent to review or put back on the site. Listings
  already live with fewer stay live, and their next change needs the photos.
- **Statistics are recorded per day:** views and phone reveals per listing per Moldovan day, and the
  "Arată numărul" (show number) button on the agency page is now counted too.
- **`GET /api/v1/agencies/{id}/stats`** puts contacts first, compares each period with the one before
  it, and also returns a daily series, the top listings and the listings that need attention.
- **A Statistici tab** in the agency's management pages shows all of it, with a one-click fix for each
  listing that needs attention.

## The 3-photo rule

`ListingPhotoRules` (`MinPhotos = 3`) is checked in these places:

| Action | Fewer than 3 photos |
|---|---|
| Create (the form uploads under the new id first) | 400 `listing.notEnoughPhotos {min, count}` |
| Submit / resubmit for review | 400 |
| Edit while in review, live, rejected or suspended | 400 |
| Re-publish (Archived/Expired → Active), renew | 400 |
| Admin approval (safety net) | 400 |
| Delete a photo from a live / in-review listing, leaving < 3 | 400 `listing.lastPhotos {min}` |
| A draft waiting for email confirmation | stays a draft until it has the photos |

**Existing listings** were kept live (product decision: enforce on the next change).
- In "Anunțurile mele", the agency's Anunțuri tab and the owner bar, they show "Adaugă fotografii"
  (add photos) as the next step and count as needing attention.
- The link opens the edit form on its photos step (`?step=photos`).

**Photos step:**
- It states the rule and shows a live counter ("2 din 3 · mai adaugă 1 fotografie") with a 3-segment
  bar; once the rule is met, it recommends 8+.
- It won't continue, or save from a later step, with fewer photos or while uploads are still running.

The CSV import (PR 4) will go through the same rule, so agency listings need no separate check.

## Data model and migrations

One migration: **`AddListingDailyStatsAndAgencyPhoneReveals`**.

- **`ListingDailyStats`**
  - One row per listing and Moldovan day (`MoldovaTime`, Europe/Chisinau): `Views` and `PhoneReveals`.
  - Key `(ListingId, Day)`, FK cascade, index on `Day`.
  - Written by `ListingCounters` in the same SQL statement as the visitor mark and the listing's own
    totals (mark → raise the total → upsert the day), so the totals and the daily series can't drift
    apart. The once-per-visitor-per-24-hours rule is unchanged.
- **`AgencyPhoneReveals`**
  - One row per agency, day and visitor (the same SHA-256 visitor hash as listings), key
    `(AgencyId, Day, VisitorHash)`, written with `INSERT … ON CONFLICT DO NOTHING`
    (`IAgencyCounters` / `AgencyCounters`).
  - The agency's own members are never counted.
  - The Worker's visitor cleanup deletes rows older than 400 days.
- **Messages and saves** per day come from their own tables (`Conversations.CreatedAt`,
  `Favorites.CreatedAt`), so no new columns are needed.

No backfill: daily numbers start on the day this is deployed. Listings' all-time `ViewCount` /
`PhoneRevealCount` are untouched.

## API

| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `/api/v1/agencies/{id}/stats` | members, site admins | `?from=&to=` (yyyy-MM-dd, Moldovan days, both included; default the last 30 ending today; at most 365, `from ≤ to`, else 400) → `AgencyStatsDto`. 403 non-member, 404 unknown/hidden. |

`AgencyStatsDto` contains:
- **`Totals` and `Previous`** (the period of the same length just before):
  - `Contacts` = listing phone reveals + agency-page phone reveals + messages (conversations started);
  - also `Views`, `ListingPhoneReveals`, `AgencyPhoneReveals`, `Messages`, `Favorites`.
- **`Daily`:** views and contacts for every day of the period, zeros included.
- **`TopListings`:** up to 10, ordered by contacts and then views, each with its cover thumbnail,
  views, reveals, messages and saves.
- **`NeedsAttention`:** Active listings only, at most 10, each with one reason:
  - `FewPhotos`: fewer than `MinPhotos`;
  - `NoContacts`: 20+ views in the period and nobody got in touch;
  - `ExpiringSoon`: within `ListingExpiry.ReminderBefore`, 7 days.

Owners, Admins and site admins see all the agency's listings. An Agent sees only their own, without
the agency page's reveals.

Unchanged route, newly counted: `POST /api/v1/agencies/{id}/contact/phone` now records the reveal (the endpoint
passes `X-Imova-Visitor`).

## The Statistici tab (web)

`/account/agencies/[id]/stats`, the fifth tab (Profil · Membri · Anunțuri · Statistici · Setări). On a
phone the tab labels drop to `text-xs` and still fit at 360 px.

- **Period:** 7 / 30 / 90 days in the address (`?days=`, 30 by default), with the dates shown. The days
  are Moldova's.
- **Contacts tile:** the largest number on the page, outlined in the accent colour. It shows:
  - the split into phone numbers shown and new conversations;
  - "N% dintre vizualizări au devenit contacte" (the contact rate);
  - for Owners/Admins, how many numbers came from the agency page;
  - the change against the period before ("↑ 12%" / "↓ 3%" / "nou" (new) / "la fel" (same)), shown
    as an arrow plus words, never colour alone.
- **Views and Saves tiles:** the same comparison.
- **Two daily bar charts**, "Contacte pe zi" and "Vizualizări pe zi":
  - one series each, so no legend; thin bars with rounded tops and a recessive scale (top and zero);
    first, middle and last day named underneath;
  - hover, tap or the arrow keys show that day's number in a fixed line above the chart;
  - "Vezi tabelul" (show table) shows every day as a table, and the chart's accessible label points
    to it;
  - all 90 bars fit on a 360 px phone.
- **"De îmbunătățit"** (what to improve): each listing with its reason and the one action that fixes
  it:
  - FewPhotos → "Adaugă fotografii", which opens the photos step;
  - NoContacts → "Revizuiește" (review), which opens the edit form;
  - ExpiringSoon → "Prelungește" (renew), which renews the listing on the spot, refreshes the page,
    and the listing leaves the list.
- **"Anunțurile cu cele mai multe contacte"** (most contacts):
  - on wider screens, a table: listing (thumbnail, title, ID, status if not Active), contacts (with the
    phone / message split), views, contact rate, saves;
  - on phones, one card per listing with the same numbers in a sentence.
- **Empty state** ("Încă nu sunt cifre pentru această perioadă", no numbers for this period yet) with
  "Adaugă anunț" (add listing).
- Helpers in `lib/agency/stats.ts` (period, API query, Moldova's today, change, contact rate, the fix
  per reason, axis days, scale), covered by Vitest. New i18n namespace `AgencyStats` and
  `AgencyManage.section.stats` (ro/en/ru).

## Decisions taken over the spec

- **Contacts lead, not views.** An agency pays attention to leads, so views and saves come second.
- **The weekly email to Owners is not in this PR.** "Weekly stats email to Owners, with opt-out" is
  recorded as an after-launch item, for once agencies are active.
- **Daily rows are written in the counting SQL itself**, not by a nightly aggregation job: one place,
  no lag, and nothing to reconcile.
- **Messages and saves are read from their own tables** rather than duplicated into the daily table.
- **The 3-photo rule applies to everyone**, not only agencies, so that an agency's listings need no
  special check (asked for by the product owner).

## How it was tested

Backend: **1,338 unit + 272 integration + 5 architecture** tests, all green.

**New tests:**
- `ListingPhotoRulesTests` (14 cases), plus:
  - create refused with 0 and with 2 photos;
  - confirm-email keeps a short draft as a draft;
  - the agency list's attention flag;
  - integration: create with 2 photos → 400, and removing a photo from an in-review listing → 400.
- `AgencyStatsHandlerTests` (7): totals and the previous period, default range, top order, attention
  reasons, Agent scope, access, validator.
- `AgencyStatsEndpointsTests`: real SQL; a counted view and reveal show up; 403/401/400. It caught a
  500, because Npgsql needs UTC instants for the period's start.
- The real counting SQL:
  - `ListingVisitorTests`: one daily row with 2 views and 1 reveal;
  - `AgencyPublicEndpointsTests`: 2 visitors counted, a repeat and a member not.
- The cleanup job.

**Test helpers:** listings get their photos (`ListingTestData.AddListing(photos:)`, `AddPhotos`;
`ListingApi.ValidBodyAsync` uploads 3 under a fresh id).

Frontend: **444** Vitest tests (new: `stats.test.ts`, `photos.test.ts`, owner actions/groups); `tsc`
clean.

**In the browser** (compose stack rebuilt; 1280 px, 390 px, 360 px at 3×), as Elena, the Casa Ta
Owner:
- **3-photo rule:** 4 demo listings show "Adaugă fotografii"; the link opens the photos step at
  "2 din 3"; "Continuă" (continue) refuses.
- **Statistici, with 60 days of temporary daily numbers in the dev DB:**
  - KPIs with their comparisons; both charts at 30 and 90 days; the hover readout; the table view;
  - the improve list with all three reasons, where "Prelungește" renewed the listing and it left the
    list;
  - the top-listings table on desktop and cards on a phone;
  - no horizontal scroll at 360 px; all five tabs fit.
- Fixed during the check: the chart total printed "3300" next to the tile's "3 300".
- The temporary numbers were deleted and the renewed listing's expiry put back.

## Known issues and follow-ups

- **No history before deployment:** the daily numbers start empty, so the first weeks compare against
  zeros ("nou" (new) instead of a percent).
- **Live listings with 1–2 photos** stay live until their next change. There's no reminder email about
  it; they only show in the owner's lists and the improve list.
- **Weekly stats email to Owners** (with opt-out): after launch, once agencies are active.
- **Data size:** the stats endpoint reads the period's rows (and the previous period's) into memory and
  groups them there. That's fine for an agency's hundreds of listings × 365 days; move it into SQL if
  agencies grow much larger.
- **Contacts are counted per visitor per day:** a phone reveal counts once per visitor per 24 hours (as
  before). A conversation counts on the day it started; later messages don't add contacts.
- **Tests and Azurite:** integration tests still upload into the dev Azurite container (logos,
  pictures, now also listing photos for the 3-photo rule); clean it by hand now and then.
