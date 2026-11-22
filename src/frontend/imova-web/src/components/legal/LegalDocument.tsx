import { Fragment } from "react";
import { Link } from "@/i18n/navigation";
import {
  hasPlaceholders,
  isSitePath,
  parseInline,
  type LegalBlock,
  type LegalDocument as LegalDocumentData,
  type LegalTable,
} from "@/lib/legal/document";

export type LegalDocumentLabels = {
  title: string;
  updated: string;
  contents: string;
  // Shown while the text still has placeholders to fill in.
  draftNotice: string;
  // en/ru only: the Romanian text prevails.
  translationNotice?: string;
};

// The Terms of use and the Privacy policy (/terms, /privacy): a table of contents — a collapsible
// block on phones, a sticky column beside the text on wide screens — then the numbered sections.
// Tables are real tables from sm up and one card per row on phones (no sideways scrolling).
export function LegalDocument({ document, labels }: { document: LegalDocumentData; labels: LegalDocumentLabels }) {
  const toc = (
    <ol className="flex flex-col gap-1.5 text-sm">
      {document.sections.map((section, i) => (
        <li key={section.id}>
          <a href={`#${section.id}`} className="flex gap-2 text-ink-600 hover:text-brand-700">
            <span className="w-5 shrink-0 text-right tabular-nums text-ink-400">{i + 1}.</span>
            <span>{section.heading}</span>
          </a>
        </li>
      ))}
    </ol>
  );

  return (
    <main className="mx-auto w-full max-w-6xl flex-1 px-4 pb-16 sm:px-6">
      <div className="lg:grid lg:grid-cols-[15rem_minmax(0,1fr)] lg:gap-12">
        <aside className="hidden lg:block">
          <nav aria-label={labels.contents} className="sticky top-24 pt-10">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-ink-400">{labels.contents}</p>
            {toc}
          </nav>
        </aside>

        <article className="max-w-3xl">
          <header className="pt-8">
            <h1 className="font-hero text-2xl font-extrabold text-ink-950 sm:text-3xl">{labels.title}</h1>
            <p className="mt-2 text-sm text-ink-500">{labels.updated}</p>
          </header>

          {hasPlaceholders(document) && (
            <p role="note" className="mt-5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
              {labels.draftNotice}
            </p>
          )}
          {labels.translationNotice && (
            <p className="mt-5 rounded-xl border border-ink-100 bg-white px-4 py-3 text-sm text-ink-600">{labels.translationNotice}</p>
          )}

          <details className="mt-6 rounded-2xl border border-ink-100 bg-white px-4 py-3 lg:hidden">
            <summary className="cursor-pointer text-sm font-semibold text-ink-800">{labels.contents}</summary>
            <nav aria-label={labels.contents} className="mt-3">
              {toc}
            </nav>
          </details>

          <p className="mt-6 leading-relaxed text-ink-700">
            <Inline text={document.intro} />
          </p>

          {document.sections.map((section, i) => (
            <section key={section.id} id={section.id} className="mt-10 scroll-mt-24">
              <h2 className="font-hero text-lg font-bold text-ink-950 sm:text-xl">
                {i + 1}. {section.heading}
              </h2>
              <div className="mt-3 flex flex-col gap-3 text-[0.9375rem] leading-relaxed text-ink-600">
                {section.blocks.map((block, j) => (
                  <Block key={j} block={block} />
                ))}
              </div>
            </section>
          ))}
        </article>
      </div>
    </main>
  );
}

function Block({ block }: { block: LegalBlock }) {
  if (typeof block === "string") {
    return (
      <p>
        <Inline text={block} />
      </p>
    );
  }
  if ("list" in block) {
    return (
      <ul className="flex list-disc flex-col gap-2 pl-5 marker:text-ink-300">
        {block.list.map((item, i) => (
          <li key={i}>
            <Inline text={item} />
          </li>
        ))}
      </ul>
    );
  }
  return <Table table={block.table} />;
}

function Table({ table }: { table: LegalTable }) {
  return (
    <>
      <div className="hidden overflow-hidden rounded-2xl border border-ink-100 bg-white sm:block">
        <table className="w-full text-left text-sm">
          <thead className="bg-ink-50 text-ink-800">
            <tr>
              {table.columns.map((column) => (
                <th key={column} scope="col" className="px-4 py-2.5 font-semibold">
                  {column}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100">
            {table.rows.map((row, i) => (
              <tr key={i} className="align-top">
                {row.map((cell, j) => (
                  <td key={j} className={j === 0 ? "px-4 py-3 font-medium text-ink-800" : "px-4 py-3"}>
                    <Inline text={cell} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className="flex flex-col gap-2 sm:hidden">
        {table.rows.map((row, i) => (
          <li key={i} className="rounded-2xl border border-ink-100 bg-white px-4 py-3 text-sm">
            <p className="font-semibold text-ink-800">
              <Inline text={row[0]} />
            </p>
            <dl className="mt-1.5 flex flex-col gap-1.5">
              {row.slice(1).map((cell, j) => (
                <div key={j}>
                  <dt className="text-xs font-medium uppercase tracking-wide text-ink-400">{table.columns[j + 1]}</dt>
                  <dd>
                    <Inline text={cell} />
                  </dd>
                </div>
              ))}
            </dl>
          </li>
        ))}
      </ul>
    </>
  );
}

function Inline({ text }: { text: string }) {
  return parseInline(text).map((part, i) => {
    switch (part.kind) {
      case "bold":
        return (
          <strong key={i} className="font-semibold text-ink-800">
            {part.text}
          </strong>
        );
      case "placeholder":
        return (
          <mark key={i} className="rounded bg-amber-100 px-1 text-amber-900">
            {part.text}
          </mark>
        );
      case "link":
        return isSitePath(part.href) ? (
          <Link key={i} href={part.href} className="font-medium text-brand-700 underline-offset-2 hover:underline">
            {part.text}
          </Link>
        ) : (
          <a key={i} href={part.href} className="font-medium text-brand-700 underline-offset-2 hover:underline" rel="noopener noreferrer">
            {part.text}
          </a>
        );
      default:
        return <Fragment key={i}>{part.text}</Fragment>;
    }
  });
}
