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
      entry: path.resolve(__dirname, "TinyMceExtensions/extensions.tinymce-api.ts"),
      name: "TinyMceExtensionsPlugin",
      fileName: "tinymce-extensions-plugin",
      formats: ["es"],
    },
    outDir: path.resolve(
      __dirname,
      "../TinyMceUmbraco16.Web/wwwroot/App_Plugins/tinymce-extensions-plugin"
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
          // Copy JSON, CSS, HTML, SVG, images recursively (ignore .ts files)
          src: "TinyMceExtensions/*.{json,css,html,svg,png,jpg}",
          dest: ".",
        },
        {
          src: 'TinyMceExtensions/files/**/*',
          dest: 'files', // creates/keeps outDir/files/...
        }
      ],
    }),
  ],
});