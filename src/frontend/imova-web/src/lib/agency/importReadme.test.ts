import { describe, expect, it } from "vitest";
import ro from "../../../messages/ro.json";
import type { ImportColumn } from "@/types/agency";
import {
  attributeKey,
  buildImportReadme,
  exampleCsv,
  readmeFileName,
  templateFileName,
  type ImportReadmeLabels,
} from "./importReadme";

type Messages = Record<string, unknown>;

// A tiny stand-in for next-intl over the real Romanian messages: "{x}" placeholders only.
function translator(messages: Messages) {
  const lookup = (key: string) => key.split(".").reduce<unknown>((node, part) => (node as Messages | undefined)?.[part], messages);
  const t = (key: string, values: Record<string, string | number> = {}) => {
    const message = lookup(key);
    if (typeof message !== "string") throw new Error(`Missing message ${key}`);
    return message.replace(/\{(\w+)\}/g, (_, name: string) => String(values[name]));
  };
  t.has = (key: string) => typeof lookup(key) === "string";
  return t;
}

const t = translator(ro.AgencyImport.readme as Messages);
const attributes = translator(ro.Attributes as Messages);
const types = translator(ro.PropertyType as Messages);

const labels: ImportReadmeLabels = {
  title: (names) => t("title", { types: names }),
  intro: [t("intro.format"), t("intro.header")],
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
  typeName: (type) => types(type),
  describe: (column) =>
    t.has(`columns.${column.name}`) ? t(`columns.${column.name}`) : attributes(`${attributeKey(column.name)}.label`),
  choiceLabel: (column, value) => {
    const key = `${attributeKey(column.name)}.options.${value}`;
    return attributes.has(key) ? attributes(key) : null;
  },
};

const column = (overrides: Partial<ImportColumn> & Pick<ImportColumn, "name">): ImportColumn => ({
  kind: "Text",
  requirement: "Optional",
  scope: "Both",
  choices: null,
  maxLength: null,
  example: null,
  ...overrides,
});

const columns: ImportColumn[] = [
  column({ name: "external_ref", requirement: "Required", maxLength: 100, example: "AP-1024" }),
  column({ name: "heating_system", kind: "Choice", choices: ["OwnBoiler", "HeatPump"], example: "OwnBoiler" }),
  column({ name: "living_area_m2", kind: "Decimal", example: "34" }),
  column({ name: "deposit", kind: "Decimal", scope: "Rent" }),
  column({ name: "title", requirement: "Required", example: "Apartament; 2 camere" }),
];

describe("the import README", () => {
  it("names a detail column the way the listing form does", () => {
    expect(attributeKey("living_area_m2")).toBe("livingAreaM2");
    expect(attributeKey("total_floors_in_building")).toBe("totalFloorsInBuilding");
    expect(attributeKey("deposit")).toBe("deposit");
  });

  it("describes every column: required or not, its kind, meaning, codes with their labels, example", () => {
    const text = buildImportReadme([{ type: "Apartment", columns }], labels);

    expect(text).toContain("# IMOVA — import de anunțuri din CSV: Apartament\n\nFișierul: CSV");
    // Paragraphs apart, so Markdown keeps them as paragraphs.
    expect(text).toContain("5 MB.\n\nPrima linie:");
    expect(text).toContain("Șablon: imova-import-apartment.csv");
    expect(text).toContain("1. `external_ref` — OBLIGATORIU · text · max. 100 caractere");
    expect(text).toContain("   Codul anunțului în sistemul agenției");
    expect(text).toContain("2. `heating_system` — opțional · o valoare din listă");
    expect(text).toContain("   Sistem de încălzire");
    expect(text).toContain("   - `OwnBoiler` — Cazan propriu");
    expect(text).toContain("   - `HeatPump` — Pompă de căldură");
    expect(text).toContain("   Exemplu: `OwnBoiler`");
    expect(text).toContain("4. `deposit` — opțional · număr · doar la chirie");
  });

  it("ends with the example row as CSV, quoting a cell with a separator in it", () => {
    expect(exampleCsv(columns)).toBe(
      "external_ref;heating_system;living_area_m2;deposit;title\nAP-1024;OwnBoiler;34;;\"Apartament; 2 camere\"",
    );
    expect(buildImportReadme([{ type: "Apartment", columns }], labels)).toContain("```csv\nexternal_ref;heating_system");
  });

  it("names its files after the property type", () => {
    expect(templateFileName("Garage")).toBe("imova-import-garage.csv");
    expect(readmeFileName(["Garage"])).toBe("imova-import-garage-README.md");
    expect(readmeFileName(["Apartment", "House"])).toBe("imova-import-README.md");
  });
});
