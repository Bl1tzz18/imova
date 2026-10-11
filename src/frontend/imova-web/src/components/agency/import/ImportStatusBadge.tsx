import { Badge } from "@/components/ui/Badge";
import type { ListingImportJob } from "@/types/agency";

const TONE = { Pending: "neutral", Processing: "brand", Completed: "success", Failed: "accent" } as const;

// An import's state as a coloured badge, with its label (colour is never the only cue).
export function ImportStatusBadge({ status, label }: { status: ListingImportJob["status"]; label: string }) {
  return <Badge tone={TONE[status]}>{label}</Badge>;
}
