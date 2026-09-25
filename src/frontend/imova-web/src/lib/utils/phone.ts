// Same rule as the backend's PhoneNumberRules: an optional "+", then digits/spaces/dashes/
// parentheses, with 7-15 actual digits (E.164's range).
export function isValidPhone(value: string): boolean {
  if (!/^\+?[\d\s\-()]+$/.test(value)) return false;
  const digits = value.replace(/\D/g, "").length;
  return digits >= 7 && digits <= 15;
}
