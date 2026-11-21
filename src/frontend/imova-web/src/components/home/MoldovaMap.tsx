"use client";

import { useEffect, useRef } from "react";
import Link from "next/link";
import { insideMoldova, MAP_ASPECT_RATIO, type MapSpot } from "@/lib/home/moldovaMap";
import { cn } from "@/lib/utils/cn";

export type MoldovaMapSpot = MapSpot & {
  href: string;
  // "Chișinău: 13 anunțuri" — the link's accessible name.
  ariaLabel: string;
  countText: string;
};

// Columns of dots across the map, whatever its size.
const DOT_COLUMNS = 38;

// The home page's map: Moldova as a field of dots (decorative, drawn on a canvas and redrawn when
// the box resizes) with a link per raion that has listings — its dot sized by how many, its name
// shown unless it would sit on a busier raion's (then on hover/focus only). Spots and labels are
// positioned in percentages of the box, so they need no measuring.
export function MoldovaMap({ spots, ariaLabel }: { spots: MoldovaMapSpot[]; ariaLabel: string }) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const container = box.current;
    const element = canvas.current;
    if (!container || !element) return;

    const draw = () => {
      const width = container.clientWidth;
      const height = container.clientHeight;
      const ctx = element.getContext("2d");
      if (!ctx || width === 0 || height === 0) return;
      const dpr = window.devicePixelRatio || 1;
      element.width = Math.round(width * dpr);
      element.height = Math.round(height * dpr);
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, width, height);
      ctx.fillStyle = "rgba(195, 225, 236, 0.3)";

      const step = width / DOT_COLUMNS;
      const radius = step * 0.2;
      for (let y = step / 2; y < height; y += step) {
        for (let x = step / 2; x < width; x += step) {
          if (!insideMoldova({ x: x / width, y: y / height })) continue;
          ctx.beginPath();
          ctx.arc(x, y, radius, 0, Math.PI * 2);
          ctx.fill();
        }
      }
    };

    draw();
    const observer = new ResizeObserver(draw);
    observer.observe(container);
    return () => observer.disconnect();
  }, []);

  return (
    <nav aria-label={ariaLabel}>
      <div ref={box} className="relative mx-auto w-full" style={{ aspectRatio: MAP_ASPECT_RATIO }}>
        <canvas ref={canvas} aria-hidden className="absolute inset-0 h-full w-full" />
        {spots.map((spot) => {
          // Labels go to the left of spots near the eastern edge, so they stay inside the map.
          const labelLeft = spot.position.x > 0.62;
          const size = 8 + Math.round(spot.weight * 14);
          return (
            <Link
              key={spot.raionId}
              href={spot.href}
              aria-label={spot.ariaLabel}
              className="group absolute flex h-8 w-8 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full outline-none focus-visible:ring-2 focus-visible:ring-white"
              style={{ left: `${spot.position.x * 100}%`, top: `${spot.position.y * 100}%` }}
            >
              <span
                aria-hidden
                className="absolute rounded-full bg-accent-500/20 transition-transform group-hover:scale-125"
                style={{ width: size * 2.2, height: size * 2.2 }}
              />
              <span aria-hidden className="relative rounded-full bg-accent-500" style={{ width: size, height: size }} />
              <span
                aria-hidden
                className={cn(
                  "absolute top-1/2 -translate-y-1/2 whitespace-nowrap rounded-full bg-white px-2.5 py-1 text-xs font-semibold text-ink-950 shadow-[0_6px_16px_-8px_rgba(0,0,0,0.6)] transition-opacity",
                  labelLeft ? "right-full mr-1" : "left-full ml-1",
                  spot.labelled ? "opacity-100" : "pointer-events-none opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100",
                  spot.weight === 1 && "text-[13px]",
                )}
              >
                {spot.raionName}
                <span className="ml-1 text-brand-700">{spot.countText}</span>
              </span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
