import { getLocale, getTranslations } from "next-intl/server";
import { SECTION_ICONS } from "@/components/property/sectionIcons";
import {
  isHighlightSection,
  keyFacts,
  listingDetailSections,
  rentalTermFacts,
  type DetailFact,
  type DetailSectionView,
  type KeyFact,
} from "@/lib/listing/detailSections";
import { squareMetersToAri } from "@/lib/listing/view";
import { formatDate, formatPrice } from "@/lib/utils/format";
import type { Listing } from "@/types/listing";

type Translate = Awaited<ReturnType<typeof getTranslations>>;

function formatNumber(value: number) {
  // Same grouping as prices (a space), whatever the language.
  return new Intl.NumberFormat("ru-RU", { maximumFractionDigits: 2 }).format(value);
}

function Icon({ children, className = "h-5 w-5" }: { children: React.ReactNode; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden>
      {children}
    </svg>
  );
}

// Yes / No as a small mark plus the word, so it reads at a glance and still says it in words.
function YesNo({ value, t }: { value: boolean; t: Translate }) {
  return value ? (
    <span className="inline-flex items-center gap-1 text-emerald-700">
      <Icon className="h-4 w-4">
        <path d="m5 12.5 4.5 4.5L19 7.5" />
      </Icon>
      {t("yes")}
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 text-ink-500">
      <Icon className="h-4 w-4">
        <path d="M6 12h12" />
      </Icon>
      {t("no")}
    </span>
  );
}

// The listing page's key facts and details, grouped like the form the owner filled in.
export async function ListingKeyFacts({ listing }: { listing: Listing }) {
  const [t, tAttr] = await Promise.all([getTranslations("PropertyDetail"), getTranslations("Attributes")]);
  const facts = keyFacts(listing.property);
  if (facts.length === 0) return null;

  const valueOf = (fact: KeyFact) => {
    switch (fact.id) {
      case "area":
        return listing.property.propertyType === "Land"
          ? `${formatNumber(squareMetersToAri(Number(fact.value)))} ari`
          : `${formatNumber(Number(fact.value))} m²`;
      case "landArea":
        return `${formatNumber(squareMetersToAri(Number(fact.value)))} ari`;
      case "floor":
        return fact.ofFloors !== undefined ? `${fact.value} / ${fact.ofFloors}` : String(fact.value);
      case "plotType":
      case "spaceType":
      case "parkingType":
      case "bathroomType":
        return tAttr(`${fact.id}.options.${String(fact.value)}`);
      default:
        return String(fact.value);
    }
  };

  return (
    <dl className="mt-5 grid grid-cols-2 gap-2.5 sm:grid-cols-4">
      {facts.map((fact) => (
        <div key={fact.id} className="rounded-2xl border border-ink-100 bg-white px-4 py-3">
          <dd className="font-display text-xl font-semibold leading-tight text-ink-950">{valueOf(fact)}</dd>
          <dt className="mt-0.5 text-xs text-ink-500">
            {t(`keyFacts.${fact.id}`, { count: typeof fact.value === "number" ? fact.value : 0 })}
          </dt>
        </div>
      ))}
    </dl>
  );
}

// Turns a DetailFact into a translated label and a formatted value — shared by the detail
// sections and the price card's lease terms, so a value reads the same wherever it appears.
async function factDescriber(listing: Listing) {
  const [locale, t, tAttr, tCondition] = await Promise.all([
    getLocale(),
    getTranslations("PropertyDetail"),
    getTranslations("Attributes"),
    getTranslations("Condition"),
  ]);
  const { property } = listing;

  // Form labels carry their unit ("Living area (m²)"); here the value shows it.
  const attributeLabel = (name: string) =>
    (t.has(name) ? t(name) : tAttr(`${name}.label`)).replace(/\s*\((m²|m|м²|м|kW|кВт)\)$/, "");

  // Below-ground levels read better by name: -1 = basement (subsol), 0 = semi-basement (demisol).
  const formatFloor = (floor: unknown) => {
    const n = Number(floor);
    if (n < 0) return `${tAttr("floorLevels.basement")} (${n})`;
    if (n === 0) return `${tAttr("floorLevels.semiBasement")} (0)`;
    return String(floor);
  };

  const describe = (fact: DetailFact): { label: string; value: React.ReactNode } => {
    if (fact.kind === "core") {
      if (fact.field === "totalAreaM2") {
        const area = Number(fact.value);
        return {
          label: t("area"),
          value: property.propertyType === "Land" ? `${formatNumber(area)} m² (${formatNumber(squareMetersToAri(area))} ari)` : `${formatNumber(area)} m²`,
        };
      }
      if (fact.field === "yearBuilt") return { label: t("yearBuilt"), value: String(fact.value) };
      return { label: t("condition"), value: tCondition(String(fact.value)) };
    }

    if (fact.kind === "rental") {
      switch (fact.field) {
        case "minLeasePeriodMonths":
          return { label: t("minLeasePeriod"), value: t("months", { count: Number(fact.value) }) };
        case "securityDepositAmount":
          return { label: t("securityDeposit"), value: formatPrice(Number(fact.value), listing.price.currency) };
        case "availableFrom":
          return { label: t("availableFrom"), value: formatDate(String(fact.value), locale) };
        case "utilitiesIncluded":
          return { label: t("utilitiesIncluded"), value: <YesNo value={fact.value === true} t={t} /> };
        case "petsAllowed":
          return { label: t("petsAllowed"), value: <YesNo value={fact.value === true} t={t} /> };
      }
    }

    const { field, value } = fact;
    const label = attributeLabel(field.name);
    if (field.name === "floor") {
      return {
        label,
        value: fact.ofFloors !== undefined ? t("floorOf", { floor: formatFloor(value), totalFloors: fact.ofFloors }) : formatFloor(value),
      };
    }
    if (field.kind === "enum") return { label, value: tAttr(`${field.name}.options.${String(value)}`) };
    if (field.kind === "yesno") return { label, value: <YesNo value={value === true} t={t} /> };
    if (field.name.endsWith("AreaM2")) return { label, value: `${formatNumber(Number(value))} m²` };
    if (field.name === "ceilingHeightM") return { label, value: `${formatNumber(Number(value))} m` };
    if (typeof value === "number") return { label, value: formatNumber(value) };
    return { label, value: String(value) };
  };

  return describe;
}

export async function ListingDetails({ listing }: { listing: Listing }) {
  const [t, tForm, tAmenity, tProximity, describe] = await Promise.all([
    getTranslations("PropertyDetail"),
    getTranslations("PropertyForm"),
    getTranslations("Amenity"),
    getTranslations("Proximity"),
    factDescriber(listing),
  ]);
  const sections = listingDetailSections(listing);
  if (sections.length === 0) return null;

  const title = (section: DetailSectionView) => tForm(`detailSections.${section.id}`);
  const highlights = sections.filter(isHighlightSection);
  const factSections = sections.filter((section) => !isHighlightSection(section));

  const sectionIcon = (section: DetailSectionView) => (
    <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-700">
      <Icon>{SECTION_ICONS[section.id]}</Icon>
    </span>
  );

  const chips = (section: DetailSectionView) => (
    <>
      {section.amenities.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-2" aria-label={title(section)}>
          {section.amenities.map((a) => (
            <li key={a.id} className="inline-flex items-center gap-1.5 rounded-full bg-brand-50 px-3 py-1.5 text-sm text-brand-800">
              <Icon className="h-3.5 w-3.5">
                <path d="m5 12.5 4.5 4.5L19 7.5" />
              </Icon>
              {tAmenity.has(a.key) ? tAmenity(a.key) : a.labelRo}
            </li>
          ))}
        </ul>
      )}
      {section.proximities.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-2" aria-label={title(section)}>
          {section.proximities.map((p) => (
            <li key={p.id} className="inline-flex items-center gap-1.5 rounded-full border border-ink-100 bg-ink-50 px-3 py-1.5 text-sm text-ink-700">
              <Icon className="h-3.5 w-3.5">{SECTION_ICONS.proximities}</Icon>
              {tProximity.has(p.key) ? tProximity(p.key) : p.labelRo}
            </li>
          ))}
        </ul>
      )}
    </>
  );

  const card = (section: DetailSectionView) => (
    <section
      key={section.id}
      aria-labelledby={`details-${section.id}`}
      className="mb-4 break-inside-avoid rounded-2xl border border-ink-100 bg-white p-5"
    >
      <h3 id={`details-${section.id}`} className="flex items-center gap-2.5 text-sm font-semibold text-ink-900">
        {sectionIcon(section)}
        {title(section)}
      </h3>

      {section.facts.length > 0 && (
        <dl className="mt-3 divide-y divide-ink-100">
          {section.facts.map((fact) => {
            const { label, value } = describe(fact);
            return (
              <div key={fact.kind === "attribute" ? fact.field.name : fact.field} className="flex items-baseline justify-between gap-4 py-2 text-sm">
                <dt className="text-ink-500">{label}</dt>
                <dd className="text-right font-medium text-ink-900">{value}</dd>
              </div>
            );
          })}
        </dl>
      )}

      {chips(section)}
    </section>
  );


  return (
    <section className="mt-10" aria-labelledby="listing-details-title">
      <h2 id="listing-details-title" className="font-display text-xl font-medium text-ink-950">
        {t("propertyDetails")}
      </h2>
      <p className="mt-1 text-sm text-ink-500">{t("propertyDetailsHint")}</p>
      {/* The picks first — what the home comes with and what's nearby is what people scan for —
          in one panel, a column per section (like the form's sections, side by side). */}
      {highlights.length > 0 && (
        <div className="mt-4 grid gap-x-6 gap-y-5 rounded-2xl border border-ink-100 bg-white p-5 sm:grid-cols-2">
          {highlights.map((section) => (
            <section key={section.id} aria-labelledby={`details-${section.id}`}>
              <h3 id={`details-${section.id}`} className="flex items-center gap-2.5 text-sm font-semibold text-ink-900">
                {sectionIcon(section)}
                {title(section)}
              </h3>
              {chips(section)}
            </section>
          ))}
        </div>
      )}

      {/* Every fact section, always: at most five per type, so nothing needs hiding. CSS columns
          keep the cards' reading order (the form's order) while packing unequal heights. */}
      {factSections.length > 0 && <div className="mt-4 gap-4 sm:columns-2">{factSections.map(card)}</div>}
    </section>
  );
}

// The lease terms, for the price card: entered in the form's "Price & terms" step, read next to
// the price by anyone comparing rentals.
export async function ListingRentalTerms({ listing }: { listing: Listing }) {
  const facts = rentalTermFacts(listing.transactionType, listing.rentalDetails);
  if (facts.length === 0) return null;
  const [t, describe] = await Promise.all([getTranslations("PropertyDetail"), factDescriber(listing)]);

  return (
    <div className="mt-4 border-t border-ink-100 pt-4">
      <h2 className="text-[11px] font-medium uppercase tracking-wide text-ink-400">{t("rentalTerms")}</h2>
      <dl className="mt-1.5 space-y-1.5 text-sm">
        {facts.map((fact) => {
          const { label, value } = describe(fact);
          return (
            <div key={fact.kind === "attribute" ? fact.field.name : fact.field} className="flex items-baseline justify-between gap-3">
              <dt className="text-ink-500">{label}</dt>
              <dd className="text-right font-medium text-ink-900">{value}</dd>
            </div>
          );
        })}
      </dl>
    </div>
  );
}
