import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

// Unit tests for pure frontend logic (schemas, form rules) — plain Node, no DOM. A component may be
// checked by rendering it to a string (react-dom/server, see SimilarListings.test.ts), never in a
// browser. Test files live next to the code they cover as *.test.ts.
export default defineConfig({
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  test: {
    environment: "node",
    include: ["src/**/*.test.ts"],
  },
});
