import { cn } from "@/lib/utils/cn";
import type { Message } from "@/types/messaging";

// One message: mine on the right with a Sent/Delivered/Read tick, theirs on the left.
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
  return (
    <div className={cn("flex", mine ? "justify-end" : "justify-start")}>
      <div
        className={cn(
          "max-w-[80%] rounded-2xl px-3.5 py-2 text-sm shadow-sm",
          mine ? "rounded-br-md bg-brand-700 text-white" : "rounded-bl-md border border-ink-100 bg-white text-ink-900",
        )}
      >
        {message.attachments.length > 0 && (
          <div className={cn("mb-1.5 grid gap-1", message.attachments.length > 1 ? "grid-cols-2" : "grid-cols-1")}>
            {message.attachments.map((a) => (
              <a key={a.id} href={a.url} target="_blank" rel="noopener noreferrer" className="block overflow-hidden rounded-lg">
                {/* eslint-disable-next-line @next/next/no-img-element -- blob storage URL */}
                <img src={a.url} alt="" className="max-h-60 w-full object-cover" />
              </a>
            ))}
          </div>
        )}
        {message.body && <p className="whitespace-pre-wrap break-words">{message.body}</p>}
        <p className={cn("mt-1 flex items-center justify-end gap-1 text-[11px]", mine ? "text-white/70" : "text-ink-400")}>
          {time}
          {mine && (
            <span title={statusLabel} aria-label={statusLabel} className={cn(message.status === "Read" && "text-sky-300")}>
              {message.status === "Sent" ? "✓" : "✓✓"}
            </span>
          )}
        </p>
      </div>
    </div>
  );
}
