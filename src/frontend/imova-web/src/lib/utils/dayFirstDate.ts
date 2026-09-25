// Typing support for a day-first date field (dd.mm.yyyy / dd/mm/yyyy), kept as pure functions so
// the behavior is testable without a DOM. The field stores only the digits typed (max 8).

// "25" -> "25."   "2512" -> "25.12."   "251220" -> "25.12.20"
// The separator appears as soon as the day (or month) is complete, so the user never has to type
// it — without that, "25" looked finished and people pressed "." expecting it to be needed.
export function formatDayFirst(digits: string, separator: string): string {
  const day = digits.slice(0, 2);
  const month = digits.slice(2, 4);
  const year = digits.slice(4, 8);
  let text = day;
  if (digits.length >= 2) text += separator;
  text += month;
  if (digits.length >= 4) text += separator;
  return text + year;
}

// The digits after an edit, given what the field showed before and the input's new raw text.
// Backspacing the trailing separator ("25." -> "25") removes the digit before it too — otherwise
// the separator would just be re-added and backspace would appear stuck. Typed separators are
// ignored (they're inserted automatically).
export function digitsAfterEdit(previousDigits: string, previousText: string, rawText: string): string {
  const removedTrailingSeparator =
    /\D$/.test(previousText) && rawText === previousText.slice(0, -1);
  if (removedTrailingSeparator) return previousDigits.slice(0, -1);
  return rawText.replace(/\D/g, "").slice(0, 8);
}
