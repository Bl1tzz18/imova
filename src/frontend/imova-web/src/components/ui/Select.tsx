"use client";

import {
  Children,
  Fragment,
  isValidElement,
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type ComponentPropsWithoutRef,
  type FormEvent,
  type KeyboardEvent,
  type ReactElement,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import { edgeIndex, filterOptions, SEARCH_THRESHOLD, stepIndex, typeaheadIndex, type ListOption } from "@/lib/ui/listbox";
import { popoverPanel, popoverSearch } from "./popover";

// The site's list picker, a drop-in for <select>: callers keep writing <option>s, `name`,
// `value`/`defaultValue`, `onChange`, `required`. A real <select> stays in the page, out of sight,
// so forms submit and validate exactly as before and `onChange` receives a real change event; what
// people see is ours — a soft card under the field on a computer, a sheet from the bottom on a
// phone (the "Fin" design). Long lists get a search field. Keyboard: arrows, Home/End, Enter/Space,
// Escape, and typing letters jumps (the ARIA "select-only combobox" pattern).

type Option = ListOption & { label: ReactNode };

const PHONE = "(max-width: 767px)";
const POPOVER_MAX_HEIGHT = 320;
const GAP = 6;

let focusClaimed = false;

const triggerClass =
  "flex h-11 w-full items-center justify-between gap-2 rounded-xl border border-ink-200 bg-white px-3.5 text-left text-sm text-ink-900 outline-none transition-[border-color,box-shadow] hover:border-ink-300 focus-visible:border-brand-500 focus-visible:ring-2 focus-visible:ring-brand-500/20 disabled:cursor-not-allowed disabled:opacity-60 aria-expanded:border-ink-300 aria-expanded:ring-4 aria-expanded:ring-ink-200/50";

export function Select({
  className,
  children,
  value,
  defaultValue,
  onChange,
  id,
  disabled,
  "aria-label": ariaLabel,
  "aria-invalid": ariaInvalid,
  "aria-describedby": ariaDescribedBy,
  ...rest
}: ComponentPropsWithoutRef<"select">) {
  const t = useTranslations("Common");
  const options = useMemo(() => readOptions(children), [children]);
  const controlled = value !== undefined;
  const [inner, setInner] = useState(() => String(defaultValue ?? options[0]?.value ?? ""));
  const current = controlled ? String(value ?? "") : inner;
  // Like a native select, a value that isn't in the list shows (and submits) the first option.
  const selectedIndex = Math.max(0, options.findIndex((o) => o.value === current));
  const selected = options[selectedIndex];

  const [open, setOpen] = useState<null | "popover" | "sheet">(null);
  const [active, setActive] = useState(-1);
  const [query, setQuery] = useState("");
  const [invalid, setInvalid] = useState(false);
  const [title, setTitle] = useState<string | undefined>(ariaLabel);
  const [position, setPosition] = useState<{ top: number; left: number; width: number; maxHeight: number; up: boolean } | null>(null);

  const triggerRef = useRef<HTMLButtonElement>(null);
  const selectRef = useRef<HTMLSelectElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const typed = useRef({ text: "", at: 0 });
  const baseId = useId();
  const listId = `${baseId}-list`;
  const optionId = useCallback((i: number) => `${baseId}-opt-${i}`, [baseId]);

  const searchable = options.length > SEARCH_THRESHOLD;
  const indexed = useMemo(() => options.map((o, index) => ({ ...o, index })), [options]);
  const visible = useMemo(() => filterOptions(indexed, query), [indexed, query]);

  const close = useCallback((refocus = true) => {
    setOpen(null);
    setQuery("");
    if (refocus) triggerRef.current?.focus({ preventScroll: true });
  }, []);

  function openList(at?: number) {
    if (disabled || options.length === 0) return;
    setActive(at ?? selectedIndex);
    setTitle(ariaLabel ?? fieldLabel(triggerRef.current));
    setOpen(window.matchMedia(PHONE).matches ? "sheet" : "popover");
  }

  // Picking goes through the hidden <select>: set it and fire a real change event, so the caller's
  // onChange and any form-level listener see exactly what a native select would have sent.
  function pick(index: number) {
    const option = options[index];
    const select = selectRef.current;
    if (!option || option.disabled || !select) return;
    close();
    if (index === selectedIndex) return;
    select.value = option.value;
    select.dispatchEvent(new Event("change", { bubbles: true }));
  }

  function handleNativeChange(e: ChangeEvent<HTMLSelectElement>) {
    if (!controlled) setInner(e.target.value);
    setInvalid(false);
    onChange?.(e);
  }

  // Up/down move within what's visible (the search may hide some options).
  function move(step: 1 | -1) {
    const next = stepIndex(visible, visible.findIndex((o) => o.index === active), step);
    if (next >= 0) setActive(visible[next].index);
  }

  function jump(key: string) {
    const now = Date.now();
    typed.current = { text: now - typed.current.at < 700 ? typed.current.text + key : key, at: now };
    return typeaheadIndex(options, typed.current.text, open ? active : selectedIndex);
  }

  function handleKey(e: KeyboardEvent) {
    const fromSearch = e.target === searchRef.current;
    const letter = e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey;
    if (!open) {
      if (["ArrowDown", "ArrowUp", "Enter", " "].includes(e.key)) {
        e.preventDefault();
        openList();
      } else if (letter) {
        openList(jump(e.key));
      }
      return;
    }
    switch (e.key) {
      case "ArrowDown":
      case "ArrowUp":
        e.preventDefault();
        move(e.key === "ArrowDown" ? 1 : -1);
        break;
      case "Home":
      case "End":
        if (fromSearch) return;
        e.preventDefault();
        setActive(visible[edgeIndex(visible, e.key === "Home" ? "first" : "last")]?.index ?? active);
        break;
      case "Enter":
        e.preventDefault();
        if (visible.some((o) => o.index === active)) pick(active);
        break;
      case " ":
        if (fromSearch) return;
        e.preventDefault();
        pick(active);
        break;
      case "Escape":
        e.preventDefault();
        close();
        break;
      case "Tab":
        close(false);
        break;
      default:
        if (!fromSearch && letter) setActive(jump(e.key));
    }
  }

  // Popover placement: under the field, or above it when there's more room there; it follows the
  // field while the page scrolls. Fixed and portalled, so no scrolling or clipping parent cuts it.
  useLayoutEffect(() => {
    if (open !== "popover") return;
    function place() {
      const rect = triggerRef.current?.getBoundingClientRect();
      if (!rect) return;
      const below = window.innerHeight - rect.bottom - GAP - 8;
      const above = rect.top - GAP - 8;
      const up = below < Math.min(POPOVER_MAX_HEIGHT, 200) && above > below;
      const width = Math.max(rect.width, 200);
      setPosition({
        top: up ? rect.top - GAP : rect.bottom + GAP,
        left: Math.max(8, Math.min(rect.left, window.innerWidth - width - 8)),
        width,
        maxHeight: Math.min(POPOVER_MAX_HEIGHT, up ? above : below),
        up,
      });
    }
    place();
    window.addEventListener("resize", place);
    window.addEventListener("scroll", place, true);
    return () => {
      window.removeEventListener("resize", place);
      window.removeEventListener("scroll", place, true);
    };
  }, [open]);

  // A press anywhere else closes it.
  useEffect(() => {
    if (!open) return;
    function onPointerDown(e: PointerEvent) {
      const target = e.target as Node;
      if (!panelRef.current?.contains(target) && !triggerRef.current?.contains(target)) close(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, [open, close]);

  // The search field takes the keyboard on a computer (not on a phone: the keyboard would cover
  // half the sheet before anyone asked to type).
  useEffect(() => {
    if (open === "popover" && searchable) searchRef.current?.focus({ preventScroll: true });
  }, [open, searchable]);

  // Keep the active option in view; while searching, the first match is the active one.
  useEffect(() => {
    if (open) document.getElementById(optionId(active))?.scrollIntoView({ block: "nearest" });
  }, [active, open, optionId]);
  useEffect(() => {
    if (query && visible.length > 0 && !visible.some((o) => o.index === active)) setActive(visible[0].index);
  }, [query, visible, active]);

  // A required list left empty: our own red outline instead of the browser's bubble (which would
  // point at the hidden select), and the field is brought into view.
  function handleInvalid(e: FormEvent<HTMLSelectElement>) {
    e.preventDefault();
    setInvalid(true);
    // A form submit reports every invalid control in page order; only the first takes focus. (A
    // step-by-step form calling reportValidity() on just this one gets it too.)
    if (focusClaimed) return;
    focusClaimed = true;
    setTimeout(() => (focusClaimed = false));
    triggerRef.current?.focus({ preventScroll: true });
    triggerRef.current?.scrollIntoView({ block: "center", behavior: "smooth" });
  }

  const isInvalid = invalid || ariaInvalid === true || ariaInvalid === "true";
  const activeId = open && visible.some((o) => o.index === active) ? optionId(active) : undefined;
  const onSheet = open === "sheet";

  const list = (
    <ul id={listId} role="listbox" aria-label={title} className="grid gap-px">
      {visible.length === 0 && <li className="px-3 py-2.5 text-sm text-ink-400">{t("listNoResults")}</li>}
      {visible.map((o) => {
        const isSelected = o.index === selectedIndex;
        return (
          <li
            key={o.index}
            id={optionId(o.index)}
            role="option"
            aria-selected={isSelected}
            aria-disabled={o.disabled || undefined}
            onPointerMove={() => !o.disabled && setActive(o.index)}
            onClick={() => pick(o.index)}
            className={cn(
              "flex cursor-pointer select-none items-center gap-3 rounded-[10px] text-ink-800 transition-colors",
              onSheet ? "min-h-12 px-3.5 py-3 text-[15px]" : "px-3 py-2.5 text-sm",
              o.index === active && "bg-ink-50",
              isSelected && "font-semibold text-ink-950",
              o.disabled && "cursor-default opacity-40",
            )}
          >
            <span className="min-w-0 flex-1">{o.label}</span>
            <CheckIcon className={cn("h-4 w-4 shrink-0 text-accent-600", !isSelected && "invisible")} />
          </li>
        );
      })}
    </ul>
  );

  const search = searchable && (
    <div className="relative mb-1.5 shrink-0">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" aria-hidden>
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.5-3.5" />
      </svg>
      <input
        ref={searchRef}
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        onKeyDown={handleKey}
        placeholder={t("listSearch")}
        aria-label={t("listSearch")}
        aria-controls={listId}
        aria-activedescendant={activeId}
        autoComplete="off"
        className={cn(popoverSearch, "pl-9")}
      />
    </div>
  );

  return (
    <>
      <button
        ref={triggerRef}
        id={id}
        type="button"
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open !== null}
        aria-controls={open ? listId : undefined}
        aria-activedescendant={searchable ? undefined : activeId}
        aria-label={ariaLabel}
        aria-invalid={isInvalid || undefined}
        aria-describedby={ariaDescribedBy}
        disabled={disabled}
        onClick={() => (open ? close() : openList())}
        onKeyDown={handleKey}
        className={cn(triggerClass, isInvalid && "border-red-400 ring-2 ring-red-500/15", className)}
      >
        {/* Every option's text stacked invisibly in one grid cell keeps the field as wide as its
            longest option, like a native select — choosing doesn't make it jump. */}
        <span className="grid min-w-0 flex-1">
          {options.map((o, i) => (
            <span key={i} aria-hidden={i !== selectedIndex} className={cn("col-start-1 row-start-1 truncate", i !== selectedIndex && "invisible h-0")}>
              {o.label}
            </span>
          ))}
        </span>
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className={cn("h-4 w-4 shrink-0 text-ink-400 transition-transform duration-200 motion-reduce:transition-none", open && "rotate-180")}
          aria-hidden
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </button>
      <select
        ref={selectRef}
        {...rest}
        value={selected?.value ?? ""}
        onChange={handleNativeChange}
        onInvalid={handleInvalid}
        onFocus={() => triggerRef.current?.focus()}
        disabled={disabled}
        tabIndex={-1}
        aria-hidden
        className="pointer-events-none absolute h-px w-px overflow-hidden opacity-0"
      >
        {options.map((o, i) => (
          <option key={i} value={o.value} disabled={o.disabled}>
            {o.text}
          </option>
        ))}
      </select>

      {open === "popover" &&
        position &&
        createPortal(
          <div
            ref={panelRef}
            onKeyDown={handleKey}
            style={{
              top: position.up ? undefined : position.top,
              bottom: position.up ? window.innerHeight - position.top : undefined,
              left: position.left,
              minWidth: position.width,
              maxHeight: position.maxHeight,
            }}
            className={cn(
              "fixed z-[60] flex max-w-[min(26rem,calc(100vw-16px))] flex-col overflow-hidden p-1.5",
              popoverPanel,
              position.up ? "origin-bottom" : "origin-top",
            )}
          >
            {search}
            <div className="min-h-0 overflow-y-auto overscroll-contain">{list}</div>
          </div>,
          document.body,
        )}

      {onSheet &&
        createPortal(
          <div className="fixed inset-0 z-[60]" onKeyDown={handleKey}>
            <div aria-hidden className="absolute inset-0 bg-ink-950/35" onClick={() => close()} />
            <div
              ref={panelRef}
              className="absolute inset-x-0 bottom-0 flex max-h-[75vh] animate-sheet-up flex-col rounded-t-[22px] bg-white px-2.5 pb-[calc(1rem+env(safe-area-inset-bottom))] pt-2 shadow-[0_-12px_32px_-12px_rgba(11,22,32,0.35)]"
            >
              <div aria-hidden className="mx-auto mb-1 h-1 w-9 shrink-0 rounded-full bg-ink-200" />
              <div className="mb-1 flex shrink-0 items-center justify-between gap-3 pl-3.5">
                <p className="truncate text-[15px] font-semibold text-ink-950">{title}</p>
                <button
                  type="button"
                  onClick={() => close()}
                  aria-label={t("close")}
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-ink-500 transition-colors hover:bg-ink-50"
                >
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="h-5 w-5" aria-hidden>
                    <path d="M6 6l12 12M18 6 6 18" />
                  </svg>
                </button>
              </div>
              {search}
              <div className="min-h-0 overflow-y-auto overscroll-contain">{list}</div>
            </div>
          </div>,
          document.body,
        )}
    </>
  );
}

function CheckIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden>
      <path d="m5 12.5 4.5 4.5L19 7.5" />
    </svg>
  );
}

// The <option>s among the children (also inside fragments and arrays), as the caller wrote them.
function readOptions(children: ReactNode): Option[] {
  const out: Option[] = [];
  Children.forEach(children, (child) => {
    if (!isValidElement(child)) return;
    const element = child as ReactElement<{ value?: unknown; children?: ReactNode; disabled?: boolean }>;
    if (element.type === Fragment) {
      out.push(...readOptions(element.props.children));
    } else if (element.type === "option") {
      const text = textOf(element.props.children);
      out.push({
        value: element.props.value === undefined ? text : String(element.props.value),
        text,
        label: element.props.children,
        disabled: element.props.disabled,
      });
    }
  });
  return out;
}

function textOf(node: ReactNode): string {
  if (node == null || typeof node === "boolean") return "";
  if (typeof node === "string" || typeof node === "number") return String(node);
  if (Array.isArray(node)) return node.map(textOf).join("");
  if (isValidElement(node)) return textOf((node.props as { children?: ReactNode }).children);
  return "";
}

// The field's name for the sheet's title and the list: the text of the <label> around it, minus
// the field itself and the "*" of a required field.
function fieldLabel(trigger: HTMLElement | null): string | undefined {
  const label = trigger?.closest("label");
  if (!label) return undefined;
  const text = Array.from(label.childNodes)
    .filter((node) => !node.contains(trigger) && node.nodeName !== "SELECT")
    .map((node) => node.textContent ?? "")
    .join(" ")
    .replace(/\*/g, "")
    .replace(/\s+/g, " ")
    .trim();
  return text || undefined;
}
