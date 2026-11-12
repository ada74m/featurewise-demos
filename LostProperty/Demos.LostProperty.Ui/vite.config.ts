import { defineConfig } from "vitest/config";

// Builds the module as one ES module, dist/index.js, which the shell loads with a plain import().
export default defineConfig({
  build: {
    lib: { entry: "src/index.ts", formats: ["es"], fileName: () => "index.js" },
    outDir: "dist",
    minify: true,
    emptyOutDir: true
  },
  test: { include: ["test/**/*.test.ts"] }
});
