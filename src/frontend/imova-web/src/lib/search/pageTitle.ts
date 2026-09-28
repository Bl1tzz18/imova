import { getTranslations } from "next-intl/server";
import { singlePropertyType, type SearchState } from "./filters";

// "Apartamente de vânzare", "Anunțuri de închiriat", "Toate anunțurile", … — the heading (and page
// title) of a search, shared by the /search list and the /map view of the same search.
export async function searchTitle(state: SearchState): Promise<string> {
  const t = await getTranslations("Search");
  const type = singlePropertyType(state);
  const transaction = state.transactionType?.[0];
  const subject = type ? t(`typePlural.${type}`) : t("listings");
  if (transaction === "Sale") return t("titleSale", { subject });
  if (transaction === "Rent") return t("titleRent", { subject });
  return type ? subject : t("titleAll");
}
