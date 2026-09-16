const paths: Record<string, string> = {
  Apartment:
    "M4 21V7l8-4 8 4v14M9 21v-6h6v6M9 11h.01M15 11h.01M9 15h.01M15 15h.01",
  House: "M3 12 12 4l9 8M5 10.5V21h14V10.5M9 21v-6h6v6",
  Land: "M3 20h18M5 20V9l7-5 7 5v11M9 20v-5h6v5",
  Commercial: "M4 21V9l8-6 8 6v12M9 21v-5h6v5M8 12h.01M12 12h.01M16 12h.01",
  Garage: "M4 21V10l8-6 8 6v11M4 21h16M7 21v-7h10v7",
  Room: "M4 21V6.5L12 3l8 3.5V21M4 21h16M9 21v-6h6v6",
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
