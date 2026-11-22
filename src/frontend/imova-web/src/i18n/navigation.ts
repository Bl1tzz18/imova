import { createNavigation } from "next-intl/navigation";
import { routing } from "./routing";

// Use these instead of next/link and next/navigation's router helpers: they keep the visitor in
// their language (/ru/… stays /ru/…). usePathname returns the path without the language prefix.
// notFound and useSearchParams still come from next/navigation.
export const { Link, redirect, permanentRedirect, usePathname, useRouter, getPathname } = createNavigation(routing);
