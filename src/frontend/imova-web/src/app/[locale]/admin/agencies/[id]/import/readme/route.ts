import { getTranslations } from "next-intl/server";
import type { NextRequest } from "next/server";
import { getSessionToken } from "@/lib/auth/session";
import { isLocale, type Locale } from "@/i18n/config";
import {
  attributeKey,
  buildImportReadme,
  IMPORT_PROPERTY_TYPES,
  readmeFileName,
  type ImportReadmeLabels,
} from "@/lib/agency/importReadme";
import type { ImportColumn } from "@/types/agency";

// The CSV import's README as a download: ?propertyType=Apartment for one type, without it all six.
// Romanian unless ?locale=en|ru. Same access as the template (site admins only — the API refuses
// anyone else); signed in with the session cookie. The columns come from the API, the labels from the
// site's translations.
export async function GET(request: NextRequest, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getSessionToken();
  if (!token) {
    return new Response("Sign in first.", { status: 401 });
  }

  const search = request.nextUrl.searchParams;
  const requested = search.get("propertyType");
  const types = (IMPORT_PROPERTY_TYPES as readonly string[]).includes(requested ?? "") ? [requested!] : [...IMPORT_PROPERTY_TYPES];
  const localeParam = search.get("locale") ?? undefined;
  const locale: Locale = isLocale(localeParam) ? localeParam : "ro";

  const apiUrl = process.env.API_URL ?? "http://localhost:8080";
  const sections = [];
  for (const type of types) {
    const res = await fetch(
      `${apiUrl}/api/v1/agencies/${encodeURIComponent(id)}/import/columns?propertyType=${type}`,
      { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" },
    );
    if (!res.ok) {
      return new Response(res.status === 403 ? "Not allowed." : "Not found.", { status: res.status === 403 ? 403 : 404 });
    }

    sections.push({ type, columns: (await res.json()) as ImportColumn[] });
  }

  const text = buildImportReadme(sections, await readmeLabels(locale));
  return new Response(text, {
    headers: {
      "Content-Type": "text/markdown; charset=utf-8",
      "Content-Disposition": `attachment; filename="${readmeFileName(types)}"`,
      "Cache-Control": "no-store",
    },
  });
}

async function readmeLabels(locale: Locale): Promise<ImportReadmeLabels> {
  const [t, attributes, propertyType, listingType, condition, amenity, proximity] = await Promise.all([
    getTranslations({ locale, namespace: "AgencyImport.readme" }),
    getTranslations({ locale, namespace: "Attributes" }),
    getTranslations({ locale, namespace: "PropertyType" }),
    getTranslations({ locale, namespace: "ListingType" }),
    getTranslations({ locale, namespace: "Condition" }),
    getTranslations({ locale, namespace: "Amenity" }),
    getTranslations({ locale, namespace: "Proximity" }),
  ]);

  const optional = (translator: { has: (key: string) => boolean; (key: string): string }, key: string) =>
    translator.has(key) ? translator(key) : null;

  return {
    title: (types) => t("title", { types }),
    intro: [t("intro.format"), t("intro.header"), t("intro.values"), t("intro.reimport"), t("intro.photos"), t("intro.author")],
    typeHeading: (type) => t("typeHeading", { type }),
    templateFile: (fileName) => t("templateFile", { fileName }),
    columnsHeading: t("columnsHeading"),
    exampleHeading: t("exampleHeading"),
    allowedValues: t("allowedValues"),
    example: t("example"),
    rentOnly: t("rentOnly"),
    maxLength: (max) => t("maxLength", { max }),
    requirement: (requirement) => t(`requirement.${requirement}`),
    kind: (kind) => t(`kind.${kind}`),
    typeName: (type) => propertyType(type),
    describe: (column) => {
      const own = optional(t, `columns.${column.name}`);
      if (own) return own;
      const key = attributeKey(column.name);
      const label = optional(attributes, `${key}.label`);
      const hint = optional(attributes, `${key}.hint`);
      return label ? (hint ? `${label} (${hint})` : label) : null;
    },
    choiceLabel: (column, value) => {
      switch (column.name) {
        case "property_type":
          return propertyType(value);
        case "deal_type":
          return listingType(value === "rent" ? "Rent" : "Sale");
        case "status":
          return t(`statusValues.${value}`);
        case "currency":
          return null;
        case "condition":
          return optional(condition, value);
        case "amenities":
          return optional(amenity, value);
        case "proximities":
          return optional(proximity, value);
        default:
          return optional(attributes, `${attributeKey(column.name)}.options.${value}`);
      }
    },
  };
}
