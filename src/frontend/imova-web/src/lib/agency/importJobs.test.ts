import { describe, expect, it } from "vitest";
import ro from "../../../messages/ro.json";
import type { ListingImportProblem } from "@/types/agency";
import {
  describeImportProblem,
  importFileProblem,
  importJobHref,
  importProgress,
  isImportRunning,
  sortProblems,
  type ImportTranslate,
} from "./importJobs";

type Messages = Record<string, unknown>;

// The real Romanian "Errors" messages, "{x}" placeholders only.
const translate: ImportTranslate = (key, values = {}) => {
  const message = key.split(".").reduce<unknown>((node, part) => (node as Messages | undefined)?.[part], ro.Errors as Messages);
  return typeof message === "string" ? message.replace(/\{(\w+)\}/g, (_, name: string) => String(values[name] ?? "")) : null;
};

const problem = (overrides: Partial<ListingImportProblem>): ListingImportProblem => ({
  severity: "Error",
  column: null,
  code: "import.invalidValue",
  params: null,
  message: "English text",
  ...overrides,
});

describe("an import's state", () => {
  it("runs while waiting or processing, with progress over the file's rows", () => {
    expect(isImportRunning({ status: "Pending" })).toBe(true);
    expect(isImportRunning({ status: "Processing" })).toBe(true);
    expect(isImportRunning({ status: "Completed" })).toBe(false);

    expect(importProgress({ status: "Pending", totalRows: 0, processedRows: 0 })).toBe(0);
    expect(importProgress({ status: "Processing", totalRows: 8, processedRows: 3 })).toBe(37);
    expect(importProgress({ status: "Failed", totalRows: 8, processedRows: 3 })).toBe(100);
    expect(importJobHref("a1", "j9")).toBe("/account/agencies/a1/import/j9");
  });

  it("only takes a non-empty CSV of at most 5 MB", () => {
    expect(importFileProblem({ name: "anunturi.CSV", size: 10 })).toBeNull();
    expect(importFileProblem({ name: "anunturi.xlsx", size: 10 })).toBe("notCsv");
    expect(importFileProblem({ name: "gol.csv", size: 0 })).toBe("empty");
    expect(importFileProblem({ name: "mare.csv", size: 5 * 1024 * 1024 + 1 })).toBe("tooLarge");
  });
});

describe("a row's problems, in Romanian", () => {
  it("uses each code's own message with its values", () => {
    expect(describeImportProblem(problem({ column: "price", code: "import.notANumber", params: { value: "optzeci" } }), translate)).toContain("optzeci");
    expect(describeImportProblem(problem({ code: "import.tooFewPhotos", params: { min: "3", count: "2" } }), translate)).toMatch(/3.*2|2.*3/);
  });

  it("translates the reason of a photo or coordinates warning", () => {
    const photo = describeImportProblem(
      problem({ severity: "Warning", code: "import.photoFailed", params: { url: "https://x.md/1.jpg", reason: "NotFound" } }),
      translate,
    );
    expect(photo).toContain("https://x.md/1.jpg");
    expect(photo).toContain(ro.Errors.codes.import.photoReason.NotFound);

    const coordinates = describeImportProblem(
      problem({ severity: "Warning", code: "import.coordinatesIgnored", params: { reason: "TooFar", km: "10" } }),
      translate,
    );
    expect(coordinates).toContain("10");
  });

  it("falls back to a message about the column for the listing form's generic rules", () => {
    expect(describeImportProblem(problem({ column: "floor", code: "PredicateValidator" }), translate)).toContain("floor");
    expect(describeImportProblem(problem({ column: "rooms", code: "InclusiveBetweenValidator", params: { from: "1", to: "50" } }), translate)).toContain("50");
  });

  it("lists errors before warnings", () => {
    const sorted = sortProblems([problem({ severity: "Warning", code: "a" }), problem({ code: "b" })]);
    expect(sorted.map((p) => p.code)).toEqual(["b", "a"]);
  });

  it("has a message for every import code the API can send", () => {
    const codes = [
      "fileUnreadable", "noRows", "tooManyRows", "missingColumns", "unknownColumns", "duplicateColumns", "fileRequired",
      "fileTooLarge", "agencyNotVerified", "agencyInactive", "alreadyRunning", "fileMissing", "workerFailed", "required",
      "notANumber", "notAWholeNumber", "notYesNo", "notADate", "unknownValue", "tooLong", "columnNotForType", "rentOnly",
      "duplicateRef", "unknownCity", "unknownLocality", "ambiguousLocality", "unknownSector", "sectorOutsideChisinau",
      "localityAndSector", "coordinatesIncomplete", "tooFewPhotos", "tooManyPhotos", "badPhotoUrl", "unknownAmenity",
      "unknownProximity", "invalidValue", "rowFailed", "notEnoughPhotos", "photoFailed", "agentNotMember", "coordinatesIgnored",
    ];
    for (const code of codes) {
      expect(translate(`codes.import.${code}`), code).not.toBeNull();
    }

    for (const reason of ["Blocked", "NotFound", "TooLarge", "TimedOut", "Failed", "NotAnImage"]) {
      expect(translate(`codes.import.photoReason.${reason}`), reason).not.toBeNull();
    }

    for (const reason of ["OutsideMoldova", "TooFar", "Unverifiable"]) {
      expect(translate(`codes.import.coordinatesReason.${reason}`), reason).not.toBeNull();
    }
  });
});
