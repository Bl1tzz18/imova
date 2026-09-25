"use client";

import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { startConversation } from "@/lib/messaging/actions";
import { MessageComposer } from "./MessageComposer";

// The first message about a listing — then straight into the conversation.
export function StartConversationForm({ listingId }: { listingId: string }) {
  const t = useTranslations("Messages");
  const router = useRouter();

  async function handleSend(body: string, blobNames: string[]) {
    const result = await startConversation(listingId, body, blobNames);
    if (result.error !== undefined) return { error: result.error };
    router.push(`/messages/${result.conversationId}`);
    return {};
  }

  return <MessageComposer onSend={handleSend} autoFocus placeholder={t("firstMessagePlaceholder")} />;
}
