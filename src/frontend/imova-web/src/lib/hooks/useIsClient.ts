import { useSyncExternalStore } from "react";

const subscribe = () => () => {};

// False while rendering on the server and during hydration, true afterwards. For output that
// depends on the browser — e.g. times in the viewer's time zone, which the server (running in UTC)
// can't know: rendering them only once this is true avoids a hydration mismatch.
export function useIsClient(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
}
