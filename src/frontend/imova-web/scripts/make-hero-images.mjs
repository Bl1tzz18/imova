// Makes the home page hero's photo files from one source photo:
//   node scripts/make-hero-images.mjs public/images/hero-home.jpg
// Two crops, each in AVIF + WebP at a few widths, into public/images/hero/:
// - wide (≥640px screens): 12:5, the band the hero shows on a computer;
// - tall (phones): 3:4, the hero is about as tall as it is wide there.
// Both keep the photo's point of interest at 60% across, 30% down (the hero's object-position).
// Bump VERSION when the photo changes: the files are cached for a year under their names.
import sharp from "sharp";
import { mkdirSync } from "node:fs";

const VERSION = 1;
const FOCUS = { x: 0.6, y: 0.3 };
const CROPS = {
  wide: { ratio: 12 / 5, widths: [1280, 1600, 2000] },
  tall: { ratio: 3 / 4, widths: [480, 800] },
};
const FORMATS = { avif: { quality: 38, effort: 6 }, webp: { quality: 60, effort: 6 } };

const source = process.argv[2];
if (!source) throw new Error("usage: node scripts/make-hero-images.mjs <photo>");
const outDir = "public/images/hero";
mkdirSync(outDir, { recursive: true });

const { width: srcW, height: srcH } = await sharp(source).rotate().metadata();
for (const [name, { ratio, widths }] of Object.entries(CROPS)) {
  // The largest crop of this ratio, placed around the focus point.
  const cropW = Math.min(srcW, Math.round(srcH * ratio));
  const cropH = Math.min(srcH, Math.round(cropW / ratio));
  const left = Math.round((srcW - cropW) * FOCUS.x);
  const top = Math.round((srcH - cropH) * FOCUS.y);
  for (const width of widths.filter((w) => w <= cropW)) {
    for (const [format, options] of Object.entries(FORMATS)) {
      const file = `${outDir}/home-v${VERSION}-${name}-${width}.${format}`;
      const info = await sharp(source).rotate().extract({ left, top, width: cropW, height: cropH }).resize({ width })[format](options).toFile(file);
      console.log(file, `${info.width}x${info.height}`, `${Math.round(info.size / 1024)} KB`);
    }
  }
}
