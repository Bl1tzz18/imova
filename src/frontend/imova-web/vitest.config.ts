import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

// Unit tests for pure frontend logic only (schemas, form rules) — plain Node, no DOM/React
// rendering. Test files live next to the code they cover as *.test.ts.
export default defineConfig({
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  test: {
    environment: "node",
    include: ["src/**/*.test.ts"],
  },
});
