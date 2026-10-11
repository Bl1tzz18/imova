import type { ImportColumn } from "@/types/agency";

// The plain-text README of the CSV import, for preparing a file without the app: per property type,
// every column (required or not, its kind, what it means, the accepted codes with their labels, the
// template's example), then the example row as CSV. Built from the API's column definitions
// (GET …/import/columns — the backend's ImportColumns) and the site's own labels, so neither can drift.
// Pure (Vitest): the caller passes the labels.

export const IMPORT_PROPERTY_TYPES = ["Apartment", "House", "Land", "Commercial", "Garage", "Room"] as const;
export type ImportPropertyType = (typeof IMPORT_PROPERTY_TYPES)[number];

// What the README needs from the translations (the route handler wires them to next-intl).
export type ImportReadmeLabels = {
  title: (types: string) => string;
  intro: string[];
  typeHeading: (type: string) => string;
  templateFile: (fileName: string) => string;
  columnsHeading: string;
  exampleHeading: string;
  allowedValues: string;
  example: string;
  rentOnly: string;
  maxLength: (max: number) => string;
  requirement: (requirement: ImportColumn["requirement"]) => string;
  kind: (kind: ImportColumn["kind"]) => string;
  typeName: (type: string) => string;
  // What the column means: its own description, or the listing form's label (and hint) for a detail.
  describe: (column: ImportColumn) => string | null;
  // A code's label (e.g. OwnBoiler → "Cazan propriu"), or null when the code says it all (EUR).
  choiceLabel: (column: ImportColumn, value: string) => string | null;
};

// "living_area_m2" → "livingAreaM2": the listing form's (and Attributes.*) name for a detail column.
export function attributeKey(column: string): string {
  return column.replace(/_([a-z0-9])/g, (_, c: string) => c.toUpperCase());
}

export function templateFileName(type: string): string {
  return `imova-import-${type.toLowerCase()}.csv`;
}

export function readmeFileName(types: readonly string[]): string {
  return types.length === 1 ? `imova-import-${types[0].toLowerCase()}-README.md` : "imova-import-README.md";
}

// One CSV cell: quoted when it holds the separator, a quote or a line break.
function csvCell(value: string): string {
  return /[;"\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

export function exampleCsv(columns: readonly ImportColumn[]): string {
  return [columns.map((c) => csvCell(c.name)).join(";"), columns.map((c) => csvCell(c.example ?? "")).join(";")].join("\n");
}

export function buildImportReadme(
  sections: readonly { type: string; columns: readonly ImportColumn[] }[],
  labels: ImportReadmeLabels,
): string {
  const lines: string[] = [];
  lines.push(`# ${labels.title(sections.map((s) => labels.typeName(s.type)).join(", "))}`);
  for (const paragraph of labels.intro) {
    lines.push("", paragraph);
  }

  for (const { type, columns } of sections) {
    lines.push("", `## ${labels.typeHeading(labels.typeName(type))}`, "", labels.templateFile(templateFileName(type)), "");
    lines.push(`### ${labels.columnsHeading}`, "");

    columns.forEach((column, index) => {
      const facts = [labels.requirement(column.requirement), labels.kind(column.kind)];
      if (column.maxLength) facts.push(labels.maxLength(column.maxLength));
      if (column.scope === "Rent") facts.push(labels.rentOnly);
      lines.push(`${index + 1}. \`${column.name}\` — ${facts.join(" · ")}`);

      const description = labels.describe(column);
      if (description) lines.push(`   ${description}`);

      if (column.choices && column.choices.length > 0) {
        lines.push(`   ${labels.allowedValues}`);
        for (const value of column.choices) {
          const label = labels.choiceLabel(column, value);
          lines.push(label ? `   - \`${value}\` — ${label}` : `   - \`${value}\``);
        }
      }

      if (column.example) lines.push(`   ${labels.example} \`${column.example}\``);
      lines.push("");
    });

    lines.push(`### ${labels.exampleHeading}`, "", "```csv", exampleCsv(columns), "```");
  }

  return `${lines.join("\n").trimEnd()}\n`;
}
