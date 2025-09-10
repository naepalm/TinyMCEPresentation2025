import { defineConfig } from "vite";
import { viteStaticCopy } from "vite-plugin-static-copy";
import path from "path";
import { fileURLToPath } from "url";

// Recreate __dirname for ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default defineConfig({
  build: {
    lib: {
      entry: path.resolve(__dirname, "TinyMceMentions/mentions.tinymce-api.ts"),
      name: "TinyMceMentionsPlugin",
      fileName: "tinymce-mentions-plugin",
      formats: ["es"],
    },
    outDir: path.resolve(
      __dirname,
      "../TinyMceUmbraco16.Web/wwwroot/App_Plugins/tinymce-mentions-plugin"
    ),
    emptyOutDir: false,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco-cms\//, /^@tiny-mce-umbraco\//],
    },
  },
  plugins: [
    viteStaticCopy({
      targets: [
        {
          // Copy all JSON, CSS, HTML, SVG, etc. files but ignore .ts
          src: "TinyMceMentions/*.{json,css,html,svg,png,jpg}",
          dest: ".",
        },
      ],
    }),
  ],
});