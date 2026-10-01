// A finished touch on the photo viewer, as a swipe — or not. Mostly sideways and far enough:
// left (finger moved left) shows the next photo, right the previous one. Anything else (a tap, a
// mostly vertical drag, a short wobble) is no swipe.
export const SWIPE_MIN_DISTANCE = 50;

export function swipeDirection(dx: number, dy: number): "next" | "previous" | null {
  if (Math.abs(dx) < SWIPE_MIN_DISTANCE || Math.abs(dx) <= Math.abs(dy) * 1.2) return null;
  return dx < 0 ? "next" : "previous";
}
