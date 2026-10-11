# PR 4 — Agencies: CSV import of listings

**Branch:** `feat/agency-import` → `main`
**Title:** Agencies (4/4): import listings from a CSV file

Roadmap item 3 ("Agency publishers"), the import part of `docs/agency-publishers-spec.md`. Where this
PR departs from the spec, or settles something it left open, `docs/agency-progress.md` records the
product owner's decision.

## Summary

- **A verified agency can bring its whole portfolio in one CSV file.** Its listings go live at once,
  with no review queue (admins can still moderate them afterwards). A re-import updates the listings
  it already created, matched by the agency's own `external_ref`.
- **There is one template per property type, and one file may mix types.** Every value is a
  language-neutral code. A plain-text guide is generated per type from the same column definitions, so
  the guide can't drift from what the importer accepts.
- **Every row goes through the listing form's own rules** (validator, required details, 3-photo rule,
  price history), and problems are reported per row and column. A bad row never stops the others.
- **The Worker does the work in the background** (a queue in the database): it downloads photos
  through a hardened downloader, places addresses on the map, emails the uploader when it's done, and
  the web app shows progress and a translated list of problems.
- **Geocoding is shared and cached:** one rate limit for the API and the Worker together, and a cache
  of looked-up addresses.

## Who can import

| Who | Templates / guide | Upload |
|---|---|---|
| Agency Owner or Admin | yes, also before verification | verified, active agency |
| Agent | no | no |
| Site admin (onboarding) | any agency | any verified agency, without being a member |

- **Unverified agency:** the import page explains it and lets the agency prepare its file meanwhile.
- **Deactivated agency:** 403 `import.agencyInactive`.
- **One import at a time per agency:** a second upload gets 409 `import.alreadyRunning`. A partial
  unique index (`Status` Pending/Processing) also covers two uploads at the same moment.

**Author of each listing:**
- by default, the uploader;
- for a site admin's upload, the agency's longest-standing Owner;
- `agent_email` naming a current member overrides either;
- an `agent_email` that isn't a member: the row is imported under the default author, with the
  warning `import.agentNotMember`.

On re-import, an existing listing changes author only when `agent_email` names a member.

## The file

- **Size:** at most 5 MB and 2,000 rows.
- **Encoding:** UTF-8 (with or without BOM), UTF-16 with BOM, or Windows-1250 / 1251. 1250 is what
  Romanian Excel saves and 1251 is Russian Excel; the importer tells them apart by their letters.
- **Separator:** `;`, `,` or tab, detected from the header.
- **Header:**
  - case-insensitive, and every column must be known;
  - a typo fails the whole file (`import.unknownColumns`), as do duplicate columns;
  - `external_ref` and `property_type` are mandatory.
- **Rows:**
  - blank lines are skipped but counted, so row numbers match the spreadsheet;
  - each row is read against its own `property_type`'s columns (`import.columnNotForType` otherwise).

**Columns (`ImportColumns`)**, one list per property type:
- the listing's fields;
- the type's details, read by reflection off its attributes record, with required-ness taken from
  `PropertyAttributesValidator` itself (required / conditional / optional);
- location: city (= raion), locality, sector, street, number, latitude / longitude;
- `image_urls`: 3–20 addresses, separated by `|`;
- amenities and proximities, by key;
- rental terms (rent only);
- contact (defaults to the agency's);
- `agent_email`;
- `status` (`active` / `inactive`).

**Values:**
- choices are code names, matched ignoring case, spaces, `_` and `-`;
- yes/no is `da`/`nu` (also yes/no, true/false, 1/0);
- numbers take `.` or `,`, with spaces as thousands separators;
- dates are `yyyy-mm-dd` or `dd.mm.yyyy`.

**Places** use the same matching as the location search: case and diacritics are ignored, and so are
prefixes such as "mun.", "or.", "s.", "r-nul". A town wins over a village of the same name; otherwise
the row gets `import.ambiguousLocality`.

**Coordinates** are used only when they are inside Moldova and within 10 km of the row's sector,
locality or city. Otherwise they're ignored, with the warning `import.coordinatesIgnored {reason}`, and
the address is geocoded as the form would do.

## What a row does

- **New `external_ref`:** creates the listing.
  - `active` → Active at once (`Listing.PublishImported`, which skips review for a verified agency);
  - `inactive` → deactivated (Archived) and never published.
- **Known `external_ref`:** updates the listing through the form's own steps, including price history.
  - `inactive` takes an Active listing down;
  - `active` puts a deactivated or expired one back;
  - listings in review, rejected, suspended, sold or rented keep their status.
- **Rows missing from a re-import:** nothing happens to them. The result says "N live listings
  weren't in this file — nothing was changed on them".
- **Photos (`ImportPhotoSync`):**
  - the addresses are taken in order, and the first is the cover;
  - an address already imported for that listing is kept, not downloaded again (`Photo.SourceUrl`);
  - new ones are downloaded and checked like an upload (file signature, the three display sizes);
  - imported photos the row no longer lists are removed; hand-uploaded ones are kept after them;
  - an address that fails is a warning (`import.photoFailed {url, reason}`);
  - **fewer than 3 usable photos fails the row**, and nothing about the listing changes;
  - if the row's save fails after downloads, the new blobs are deleted (best effort, logged).
- **Each row is its own save:** a failing row is reported with every problem it has at once, and the
  rows before and after it are unaffected.

## Photo downloader (`ImportImageFetcher`)

**Destinations:**
- Every connection's destination is checked when it is made, on the IP actually connected to
  (`SocketsHttpHandler.ConnectCallback`), which also stops DNS rebinding.
- Refused: private, loopback, link-local (incl. 169.254.169.254), carrier-grade NAT, reserved,
  documentation, benchmarking and multicast addresses. IPv6 is checked too, and IPv4 inside IPv6
  (mapped, 6to4, NAT64) is judged as IPv4.

**Requests:**
- Redirects are followed by hand, at most 3, each re-checked.
- Only http/https, with no `user:password@`.
- No proxy, cookies or credentials.

**Limits:** 10 MB (checked on the declared size and while reading) and 20 s for the whole download.

## Geocoding: one limit, one cache (`SharedGeocodingService`)

- **The limit:**
  - The API and the Worker share one rate limit. A one-row table `GeocoderThrottle` holds the next
    free slot.
  - Each lookup reserves a slot with one atomic `UPDATE … RETURNING` and waits for it outside any lock.
  - This holds for any number of processes. Nominatim's in-process throttle is gone.
- **The cache:**
  - `GeocodedAddresses`, keyed by the normalised address (`GeocodingKey`).
  - Found places are kept 180 days and "no such place" 7 days (`Geocoding:CacheDays` /
    `NotFoundCacheDays`).
  - Failures are never cached.
  - It also serves the listing form.

## API

All routes are under `/api/v1/agencies/{id}/import`, for Owners, Admins and site admins.

| Method | Route | Notes |
|---|---|---|
| GET | `/template?propertyType=` | CSV: header + a valid example row; `;`, UTF-8 with BOM (opens right in Excel) |
| GET | `/columns?propertyType=` | `ImportColumnDto[]`: kind, requirement, scope, accepted values (incl. the amenity / proximity keys for the type), example |
| POST | (the base route; multipart `file`) | → 202 + the job. Size, emptiness and header are checked at once; 403 not verified / inactive; 409 already running |
| GET | (the base route) | the latest 20 imports |
| GET | `/{jobId}` | one import: status, processed of total rows, counts |
| GET | `/{jobId}/rows?problems=&page=&pageSize=` | rows with their problems (code, params, column, severity) |
| GET | `/{jobId}/report.csv` | one line per problem: row, external_ref, outcome, severity, column, code, English message |

Unknown or hidden agency → 404, non-member → 403, unknown property type → 400. All errors carry
`import.*` codes, translated in the web app.

## Worker

Job **`ListingImports`** (`ListingImports:IntervalSeconds`, 15 s):
- claims the oldest waiting import (`Status` is a concurrency token);
- processes it row by row (parse → place → photos → write → save), updating the progress;
- records the counts and emails the uploader (`ListingImportEmails`: counts, problems, a link to
  the result);
- fails imports stuck in Processing for 2 hours;
- deletes uploaded files after 30 days.

The file waits in private storage under `imports/{agency}/`.

## Data model and migrations

- **`AddListingImports`:**
  - `ListingImportJobs`: agency, uploader (set null when the account is deleted), file name, private
    blob name, status Pending → Processing → Completed | Failed (+ failure code), the counts, timestamps;
  - `ListingImportRows`: row number, external_ref, outcome, listing, problems as JSONB; cascades with
    the job;
  - `Photos.SourceUrl`.
- **`AddOneListingImportAtATime`:** the partial unique index `IX_ListingImportJobs_AgencyId_Active`.
- **`AddSharedGeocodingCache`:** `GeocoderThrottle` (with its seeded row) and `GeocodedAddresses`.

`Listings.ExternalRef` and its unique index per agency already existed (PR 1).

**Data lifecycle:**
- Deleting an agency deletes its kept import files.
- The personal data export lists the imports the account uploaded (`listingImportsYouUploaded`;
  format `imova.personal-data.v3`).

## Web

- **Entry point:** "Importă din CSV" (import from CSV) on the agency's Anunțuri tab, for those who can
  import. There's no sixth tab, and the Anunțuri tab stays marked while on the import pages.
- **`/account/agencies/[id]/import`:**
  - notices for an unverified agency (prepare the file meanwhile) and a deactivated one;
  - **1. Pregătește fișierul** (prepare the file): the template and the guide for each property type,
    and the guide for all types;
  - **2. Încarcă** (upload): checked in the browser (.csv, not empty, ≤ 5 MB), sent through a route
    handler (a server action stops at 1 MB), and disabled while an import runs, with a link to it;
  - the history: file, date, uploader, counts, status.
- **`/account/agencies/[id]/import/[jobId]`:**
  - a status badge, and a progress bar polled every 3 s while the import waits or runs; the page
    refreshes on every status change;
  - the counts (new, updated, with errors, with warnings) and "N live listings weren't in this file";
  - every row with problems (first 200): row number, external_ref, outcome, a link to the listing,
    and each problem with its severity, column and the message in the reader's language;
  - the report CSV to download.
- **The guide (README):**
  - a plain-text file generated in the web app from `GET …/columns` and the site's own translations
    (property types, attributes, conditions, amenities, proximities), so every code is listed next to
    its Romanian label;
  - Romanian by default (`?locale=` for en/ru); without `propertyType` it covers all six types.
- **Pure logic in Vitest:** `lib/agency/importReadme.ts`, `lib/agency/importJobs.ts` (incl. a message
  for every import code the API sends).
- **i18n:** `AgencyImport.page`, `AgencyImport.readme`, `Errors.codes.import.*` (ro/en/ru).

## Decisions taken over the spec

- **Values are language-neutral codes**, not Romanian words. The guide shows each code next to its
  Romanian label. Onboarding is done by the site's team, so no Romanian input values are needed.
- **No Import tab:** a button on the Anunțuri tab.
- **`inactive` = deactivated (Archived)**, and it takes an Active listing down on re-import.
- **Rows missing from a re-import are left alone**, only counted in the result.
- **Fewer than 3 usable photos fails the row**, the same rule as every listing.
- **Site admins can import for any verified agency** (onboarding); the author is then the
  longest-standing Owner.
- **Coordinates are trusted only within 10 km** of the place the row names.
- **The geocoding limit lives in Postgres**, not in memory, so it holds across the API, the Worker
  and future replicas, with no Redis needed.

## How it was tested

Backend: **1,500 unit + 276 integration + 5 architecture** tests, all green.

**Unit tests:**
- `ListingImportJobTests`
- `ImportColumnsTests`: every attribute is a column, every example row is valid
- `ImportTemplateHandlerTests`
- `ImportReadingTests`: encodings, separators, row numbers, header errors, limits, cell values
- `ImportLocationTests`
- `ImportRowParserTests`
- `ImportListingLifecycleTests`
- `ImportPhotoSyncTests`
- `ListingImportsJobTests`:
  - the job end to end, re-import, and a site admin importing under the Owner;
  - the orphaned-download cleanup when a save fails;
  - housekeeping, reads and the report
- `PublicAddressTests`
- `ImportImageFetcherTests`: a local HTTP server; redirects, size caps, a redirect to an internal
  address, loops, timeout; loopback, metadata and private addresses are never contacted
- `SharedGeocodingServiceTests` and `NominatimGeocodingServiceTests`
- agency deletion and data export additions

**Integration tests:**
- `AgencyImportTemplateEndpointsTests`
- `AgencyImportEndpointsTests`: the real stack, a site admin's import run in-process with a real
  JPEG → an Active listing under the Owner with 3 sized photos; 403/400/409, history, rows, report
- `GeocodingStoreTests`: real Postgres. Slots reserved at once from four connections come out
  spaced, and answers are upserted. This test caught a `DBNull` bug, fixed with typed parameters.
  - It failed once under full-suite load: waits measured from cold connections were skewed.
  - It now opens the connections first and compares absolute slot times. Five runs on their own and
    the full suite: green.

Frontend: **455** Vitest tests (new: `importReadme.test.ts`, `importJobs.test.ts`); `tsc` clean.

**Real run** (compose stack, Mailpit, real internet), as Elena, Owner of Casa Ta Imobiliare. Casa Ta
was verified temporarily for this.

- **First file, 5 rows of mixed types, real Wikimedia photo addresses:**
  - an apartment with coordinates, which were kept;
  - a garage without coordinates, geocoded;
  - a commercial space with an `agent_email` that isn't a member and one photo address that 404s;
  - a land plot with coordinates 10+ km off, ignored and geocoded instead;
  - a house row broken on purpose (no `rooms`, price "o sută mii").
- **Result of the first file:**
  - 4 Active listings, each with 3 sized photos, and 1 failed row;
  - the warnings: `agentNotMember`, `photoFailed (NotFound)` and `coordinatesIgnored (TooFar)`;
  - the result page and the report CSV showed every problem;
  - the email "Import finalizat … 1 rânduri cu erori" (import finished … 1 row with errors) arrived
    in Mailpit;
  - the listing page showed the real photos;
  - checked at 1280 px and 390 px, with no horizontal scroll.
- **Re-import with one price changed (79 000 → 76 500):**
  - 1 updated, 0 created;
  - "3 anunțuri active … nu erau în acest fișier" (3 active listings weren't in this file);
  - a price-history row was written; the photos were kept, not downloaded again;
  - a second email arrived.
- **Fixed during the run:** the status badge stayed "În așteptare" (waiting) while the import
  processed, because the page only refreshed at the end. It now refreshes on every status change.
- **Undone afterwards:**
  - the 4 listings were deleted through the API, photo blobs included;
  - both import jobs and their kept files were deleted;
  - Casa Ta is unverified again, with its 10 listings.

The 5 real geocoding answers were left in the dev cache.

## Also in this PR

- `91132cc`, a small fix to PR 3's Statistici tab: the "De îmbunătățit" (what to improve) list now
  comes in its intended order: few photos, then no contacts, then expiring soon.

## Known issues and follow-ups

- **Nominatim's usage policy discourages bulk geocoding.** At hosting time, consider our own instance
  or a paid geocoder if large imports become frequent.
- **Geocoding runs at about 1 address a second** (Nominatim's limit), shared with the listing form. A
  2,000-row file with no coordinates and many new addresses takes about half an hour; cached and
  repeated places are instant. Coordinates in the file avoid it.
- **The report CSV is in English** (codes + English messages, meant for scripts); the result page is
  translated. The email is Romanian only, like every email.
- **Kept files:** uploaded files are kept 30 days (for support), then deleted. They share the private
  message-attachments container (under `imports/`); a dedicated container can come with hosting.
- **`Photo.SourceUrl`** stores the address an imported photo came from. It isn't shown anywhere.
- **Imports are sequential:** one import per agency at a time, processed one at a time by the Worker.
- **Tests and Azurite:** integration tests leave small import files in the dev Azurite container
  (`imports/…`), like their other uploads; clean them by hand now and then.
