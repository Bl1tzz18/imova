import { cookies } from "next/headers";

// The anonymous visitor cookie: a random id that lets the API count a visitor's views and phone
// reveals once a day without an account (it only ever stores a hash of it). Created on the first
// counted visit — only from a server action, where cookies can be set.
const COOKIE = "imova_vid";
const ONE_YEAR = 60 * 60 * 24 * 365;

export async function visitorHeader(): Promise<Record<string, string>> {
  const jar = await cookies();
  let id = jar.get(COOKIE)?.value;
  if (!id || !/^[A-Za-z0-9-]{8,64}$/.test(id)) {
    id = crypto.randomUUID();
    jar.set(COOKIE, id, {
      httpOnly: true,
      sameSite: "lax",
      secure: process.env.NODE_ENV === "production",
      maxAge: ONE_YEAR,
      path: "/",
    });
  }
  return { "X-Imova-Visitor": id };
}
