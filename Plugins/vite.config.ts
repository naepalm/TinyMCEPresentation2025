import { defineConfig } from "vite";
import { viteStaticCopy } from "vite-plugin-static-copy";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        tinymceExtensions: path.resolve(
          __dirname,
          "TinyMceExtensions/extensions.tinymce-api.ts"
        ),
        tinymceCloudCdn: path.resolve(
          __dirname,
          "TinyMceCloudCdn/manifests.js"
        ),
      },
      external: [/^@umbraco-cms\//, /^@tiny-mce-umbraco\//],
      output: {
        entryFileNames: (chunk) => {
          if (chunk.name === "tinymceExtensions") {
            return "tinymce-extensions-plugin/tinymce-extensions-plugin.js";
          }
          if (chunk.name === "tinymceCloudCdn") {
            return "tinymce-cloud-cdn/manifests.js";
          }
          return "[name].js";
        },
      },
    },
    outDir: path.resolve(
      __dirname,
      "../TinyMceUmbraco16.Web/wwwroot/App_Plugins"
    ),
    emptyOutDir: false,
    sourcemap: true,
  },
  plugins: [
    viteStaticCopy({
      targets: [
        {
          src: "TinyMceExtensions/*.{json,css,html,svg,png,jpg}",
          dest: "tinymce-extensions-plugin",
        },
        {
          src: "TinyMceExtensions/files/**/*",
          dest: "tinymce-extensions-plugin/files",
        },
        {
          src: "TinyMceCloudCdn/umbraco-package.json",
          dest: "tinymce-cloud-cdn",
        },
      ],
    }),
  ],
});
