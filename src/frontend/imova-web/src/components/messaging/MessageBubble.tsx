import { attachmentSrc } from "@/lib/messaging/attachments";
import { cn } from "@/lib/utils/cn";
import type { Message } from "@/types/messaging";

// Mine: brand orange on the right, sharp bottom-right corner. Theirs: light gray on the left, sharp
// bottom-left corner. Images sit above the text as thumbnails with the same corner shape; the time
// (and, for mine, the Sent/Delivered/Read ticks) goes under the bubble.
export function MessageBubble({
  message,
  mine,
  time,
  statusLabel,
}: {
  message: Message;
  mine: boolean;
  time: string;
  statusLabel: string;
}) {
  const corners = mine ? "rounded-[16px_16px_4px_16px]" : "rounded-[16px_16px_16px_4px]";

  return (
    <div className={cn("flex flex-col gap-1", mine ? "items-end" : "items-start")}>
      {message.attachments.length > 0 && (
        <div className={cn("flex max-w-[min(80%,36rem)] flex-wrap gap-1.5", mine ? "justify-end" : "justify-start")}>
          {message.attachments.map((a) => (
            <a
              key={a.id}
              href={attachmentSrc(a.id)}
              target="_blank"
              rel="noopener noreferrer"
              className={cn("block h-36 w-48 overflow-hidden border border-line bg-bubble", corners)}
            >
              {/* eslint-disable-next-line @next/next/no-img-element -- private image via the access-checked proxy */}
              <img src={attachmentSrc(a.id)} alt="" className="h-full w-full object-cover" />
            </a>
          ))}
        </div>
      )}
      {message.body && (
        <p
          className={cn(
            "max-w-[min(80%,36rem)] whitespace-pre-wrap break-words px-3.5 py-2 text-sm leading-relaxed",
            corners,
            mine ? "bg-accent-500 text-white" : "bg-bubble text-ink-900",
          )}
        >
          {message.body}
        </p>
      )}
      <p className="flex items-center gap-1 px-1 text-[11px] text-ink-400">
        {time}
        {mine && (
          <span
            title={statusLabel}
            aria-label={statusLabel}
            className={cn("font-semibold tracking-[-0.2em]", message.status === "Read" ? "text-sky-500" : "text-ink-400")}
          >
            {message.status === "Sent" ? "✓" : "✓✓"}
          </span>
        )}
      </p>
    </div>
  );
}
