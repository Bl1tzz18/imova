"use client";

import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { startConversation } from "@/lib/messaging/actions";
import { track } from "@/lib/analytics/events";
import { MessageComposer } from "./MessageComposer";

// The first message about a listing — then straight into the conversation.
export function StartConversationForm({ listingId }: { listingId: string }) {
  const t = useTranslations("Messages");
  const router = useRouter();

  async function handleSend(body: string, blobNames: string[]) {
    const result = await startConversation(listingId, body, blobNames);
    if (result.error !== undefined) return { error: result.error };
    track("message_started", { listing_id: listingId, with_images: blobNames.length > 0 });
    router.push(`/messages/${result.conversationId}`);
    return {};
  }

  return <MessageComposer onSend={handleSend} autoFocus placeholder={t("firstMessagePlaceholder")} variant="card" />;
}
