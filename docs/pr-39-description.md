# PR 39 — Fixes after the pre-launch polish

**Branch:** `fix/login-redirect-message-images` → `main`
**Title:** Sign-in redirect, image viewer, map clusters and spot panel, photo step, 40-photo cap, message image sizes

These are fixes reported while using the site, plus two pre-launch hardening items found along the way.
Nothing from the launch list (KAN-5, 8, 10, 13, 14, 15, 16, 21, 22) is in here. Each change is its own commit.

## 1. Signed-in visitors skip `/login` and `/register`; message images open in a viewer (`2a3a547`)

- **Sign-in pages:** `/login` and `/register` now send a signed-in visitor to `?next=`, or to the home
  page when there isn't one.
  - The session is checked through the API, not just by looking at the cookie. A visitor whose session
    has ended still sees the form.
- **Open redirect closed:** every sign-in redirect goes through one helper, `lib/auth/nextPath.ts`
  (`safeNextPath`, Vitest).
  - The old check accepted `//evil.com`, which browsers treat as another site. A crafted link could
    send someone off IMOVA right after they signed in.
  - The helper also refuses `/\host` and control characters.
- **Shared image viewer:** the listing gallery's full-screen viewer is now `components/ui/ImageViewer`,
  rendered into `<body>`.
  - Message images open in it instead of a new tab.
  - It closes with Esc, the × button or a tap on the backdrop. Arrows, swipe and the thumbnail strip
    step through the message's images, and focus goes back to the image that opened it.

## 2. Map: listings on one spot open a compact comparison panel (`ea340a4`, `eacefa9`)

- **New panel:** `ClusterOverflowPanel` (a tall column of full listing cards, the same pattern as
  casamare.md) is replaced by `MapSpotPanel`.
  - **Header:** the shared address, or the place when only the pin is shared, plus the price range.
  - **Sort:** by price, area or floor. Only the sorts that tell these listings apart are shown.
  - **Rows:** one compact row per listing: photo, price, per-m² price, then rooms · area · floor.
  - **Mixed spots:** when a spot mixes kinds, the kind ("Apartament · Chirie") sits on its own line.
  - **Placement:** it floats at the map's bottom-left on desktop and is a bottom sheet on phones.
- **Rules:** in `lib/map/spotListings.ts` (Vitest).
- **Cleanup:** `PropertyCard` loses the two props only the old panel used.

## 3. Photo step: 3 is the minimum, not the number to add (`5ccf90c`, `113fbd6`)

- **The problem:** three segments next to "0 din 3" read as "add 3 photos".
- **Now:** a running count ("3 adăugate") and one open-ended bar.
  - The bar has "minim 3" and "ideal 8+" marks along it and fades out past its end, so there's no
    visible end.
  - One status line under it: "mai adaugă N" (add N more) → "gata de publicare" (ready to publish) →
    "excelent".
  - The upload area says "up to 40 · you can pick several at once".
- **Rules:** `photoMeter` in `lib/property/photos.ts` (Vitest).

## 4. Map: clicking a cluster keeps its listings on screen (`bc559c6`)

- **The bug:** a click centred the map on the click point and jumped to a fixed zoom (13, then 18).
  Pins about 1 km apart ended up off screen, so the map showed empty streets.
- **Zoom:** it now centres on the cluster's own listings and zooms as close as still shows all of
  them (`lib/map/clusterZoom.ts`, Vitest). It always goes in at least one level and never past the
  maximum.
- **Same spot:** a cluster whose listings all share one spot goes straight to the panel from section 2.

## 5. A listing takes at most 40 photos (`5059fb4`)

- **API:**
  - `ListingPhotoRules.MaxPhotos = 40`, checked when an upload URL is requested and again on confirm.
    Over the cap → 400 `upload.tooManyPhotos {max}`, translated in ro/en/ru.
  - A refused upload's file is deleted from storage.
  - Confirm now only accepts the exact blob name shape the server hands out
    (`{listingId}/{guid}.{ext}`). The old prefix check let `{listingId}/../…` through, and confirm can
    now delete a file.
- **Web:**
  - The uploader takes only the files that fit and says how many it skipped.
  - Once the listing is full, it says why there's no picker.
  - In the edit form it counts removed photos until the form is saved, because the server deletes
    them only then.
  - An API refusal is shown in the user's language instead of a generic "upload failed".

## 6. Message images: resized copies (`a08999e`)

- **API:** `GET /api/v1/messaging/attachments/{id}?size=thumbnail|large` returns a JPEG of at most
  800 or 1600 px.
  - Any other size → 400. No size → the original, as before.
  - Access is checked exactly as before.
- **How the copies are made:** on the first request, both from one decode of the original. They're
  stored next to the original in the private container (`MessageImageSizes`, `MessageImageCopies`).
  - Images sent before this change get copies too. There's no migration and no backfill.
  - An image that can't be resized is served as the original.
- **Account deletion:** a kept image keeps its copies; a deleted image's copies go with it.
- **Web:**
  - The proxy forwards `?size=`.
  - Conversation thumbnails ask for the small copy, and the viewer for the large one.
  - Measured in the browser: about 10 KB per thumbnail instead of the 46–57 KB originals.

## Tests

All passing:
- backend: 1585 unit, 280 integration (real Postgres and Azurite), 5 architecture
- frontend: 485 Vitest

New:
- `safeNextPath`, `spotListings`, `photoMeter`/`photoRoom`/`takeWhatFits`, `clusterClickZoom`,
  `attachmentSrc`/`attachmentSizeParam`
- the 40-photo cap: handlers, the validator's blob name shape, and an integration test through the
  real upload
- `MessageImageCopies`, the account-deletion cases for the copies, and an integration test that
  decodes the copies served by the API

Checked in the browser at 390px and 1280px:
- sign-in redirects, including `//evil.com`
- the message image viewer
- the map panel, including its sorts
- cluster zoom
- the photo step at 0, 3 and 40 photos, plus a removal waiting for save
- message thumbnails and the viewer getting the resized copies

## Not in here

- **Package vulnerabilities:** the Worker's `System.Security.Cryptography.Xml` / `.Pkcs` 10.0.11 have
  known high-severity advisories (NU1903 at build). This needs a package bump.
- **Profile pictures:** still not resized.
