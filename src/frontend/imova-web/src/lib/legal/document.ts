// The Terms of use and the Privacy policy, as data: one file per document and language in
// src/content/legal/, rendered by components/legal/LegalDocument. Text is plain strings with three
// small marks, parsed by parseInline:
//   [label](/path)  a link — a site path (rendered through the i18n Link, so it stays in the
//                   reader's language), "mailto:…" or "https://…"
//   **bold**
//   {{…}}           a placeholder still to be filled in (operator details, the email provider, …):
//                   shown highlighted, and while any is left the page says it's a preliminary version.

export type LegalTable = { columns: string[]; rows: string[][] };

export type LegalBlock = string | { list: string[] } | { table: LegalTable };

export type LegalSection = { id: string; heading: string; blocks: LegalBlock[] };

export type LegalDocument = { intro: string; sections: LegalSection[] };

export type InlinePart =
  | { kind: "text"; text: string }
  | { kind: "bold"; text: string }
  | { kind: "link"; text: string; href: string }
  | { kind: "placeholder"; text: string };

const INLINE = /\[([^\]]+)\]\(([^)\s]+)\)|\*\*(.+?)\*\*|\{\{(.+?)\}\}/g;

export function parseInline(text: string): InlinePart[] {
  const parts: InlinePart[] = [];
  let last = 0;
  for (const match of text.matchAll(INLINE)) {
    if (match.index > last) parts.push({ kind: "text", text: text.slice(last, match.index) });
    if (match[1] !== undefined) parts.push({ kind: "link", text: match[1], href: match[2] });
    else if (match[3] !== undefined) parts.push({ kind: "bold", text: match[3] });
    else parts.push({ kind: "placeholder", text: match[4] });
    last = match.index + match[0].length;
  }
  if (last < text.length) parts.push({ kind: "text", text: text.slice(last) });
  return parts;
}

// Every string of the document, in order (intro, headings, paragraphs, list items, table cells).
export function documentStrings(doc: LegalDocument): string[] {
  return [
    doc.intro,
    ...doc.sections.flatMap((section) => [
      section.heading,
      ...section.blocks.flatMap((block) =>
        typeof block === "string" ? [block] : "list" in block ? block.list : [...block.table.columns, ...block.table.rows.flat()],
      ),
    ]),
  ];
}

export function hasPlaceholders(doc: LegalDocument): boolean {
  return documentStrings(doc).some((s) => parseInline(s).some((p) => p.kind === "placeholder"));
}

export function linkTargets(doc: LegalDocument): string[] {
  return documentStrings(doc).flatMap((s) => parseInline(s).flatMap((p) => (p.kind === "link" ? [p.href] : [])));
}

// A site path ("/privacy#cookies") vs. an address outside the site (mailto:, https:).
export function isSitePath(href: string): boolean {
  return href.startsWith("/") && !href.startsWith("//");
}
