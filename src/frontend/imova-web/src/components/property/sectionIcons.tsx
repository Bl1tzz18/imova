import type { ReactNode } from "react";
import type { DetailSectionId } from "@/lib/property/detailLayouts";

// Icons (24×24 stroke paths) for the listing form's detail sections — also used by the /search
// filter panel, which groups filters the same way.
export const SECTION_ICONS: Record<DetailSectionId, ReactNode> = {
  structure: <path d="M3 11 12 4l9 7M5 10v10h14V10M10 20v-5h4v5" />,
  areas: <path d="M4 4h16v16H4zM4 9h5V4M15 20v-5h5" />,
  typeArea: <path d="M3 20h18M5 20V9l7-5 7 5v11M9 20v-6h6v6" />,
  systems: <path d="M12 3c2.5 3 4 5.3 4 7.5a4 4 0 0 1-8 0C8 8.3 9.5 6 12 3ZM6 21h12M9 17h6" />,
  utilitiesAccess: <path d="M13 2 4 14h7l-1 8 9-12h-7l1-8Z" />,
  finishing: <path d="M4 20 14 10M14 4l6 6-3 3-6-6 3-3ZM4 20l2-6 4 4-6 2Z" />,
  comfort: <path d="M5 11V8a3 3 0 0 1 3-3h8a3 3 0 0 1 3 3v3M3 12a2 2 0 0 1 4 0v3h10v-3a2 2 0 0 1 4 0v6H3v-6ZM6 18v2M18 18v2" />,
  security: <path d="M12 3.5l7 2.6v5.2c0 5-3 8-7 9.2-4-1.2-7-4.2-7-9.2V6.1l7-2.6ZM9 12l2 2 4-4" />,
  leisure: <path d="M12 3a6 6 0 0 1 6 6H6a6 6 0 0 1 6-6ZM12 9v12M8 21h8M3 17c1.5 1 3 1 4.5 0s3-1 4.5 0 3 1 4.5 0 3-1 4.5 0" />,
  other: <path d="M4 6h16M4 12h16M4 18h10" />,
  amenities: <path d="M12 3l2.6 5.3 5.9.9-4.3 4.1 1 5.8L12 16.4 6.8 19.1l1-5.8L3.5 9.2l5.9-.9L12 3Z" />,
  proximities: <path d="M12 21s-6-5.3-6-10.5a6 6 0 0 1 12 0C18 15.7 12 21 12 21ZM12 12.5a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z" />,
  rentalRules: (
    <path d="M8.5 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM15.5 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM5 15a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM19 15a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM12 13c-2.5 0-5 3-5 5 0 1.5 1.5 2 2.5 2 1 0 1.5-.5 2.5-.5s1.5.5 2.5.5c1 0 2.5-.5 2.5-2 0-2-2.5-5-5-5Z" />
  ),
};
