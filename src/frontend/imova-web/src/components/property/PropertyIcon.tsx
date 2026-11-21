// One glyph per property type, distinct at a glance (the type row on the home page shows them side
// by side): a block of flats, a house, a field under the sun, a storefront, a garage door, a bed.
const paths: Record<string, string> = {
  Apartment: "M5 21V4h14v17M3 21h18M9 8h2M13 8h2M9 12h2M13 12h2M10 21v-4h4v4",
  House: "M3 12 12 4l9 8M5 10.5V21h14V10.5M9 21v-6h6v6",
  Land: "M3 17c3.5-3 6.5-3 9 0s5.5 3 9 0M3 21h18M16 10a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z",
  Commercial: "M4 10v11h16V10M3 10l1.5-6h15L21 10ZM9 21v-6h6v6",
  Garage: "M3 21V9l9-5 9 5v12M7 21v-9h10v9M7 15h10M7 18h10",
  Room: "M3 19v-8a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v8M3 15h18M7 9V7.5A1.5 1.5 0 0 1 8.5 6h2A1.5 1.5 0 0 1 12 7.5V9M3 19v2M21 19v2",
};

export function PropertyIcon({
  type,
  className,
}: {
  type: string;
  className?: string;
}) {
  const d = paths[type] ?? paths.House;
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.4"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d={d} />
    </svg>
  );
}
