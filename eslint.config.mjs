import js from "@eslint/js";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      "**/dist/**",
      "**/node_modules/**",
      "**/.expo/**",
      "**/src-tauri/target/**",
      "backend/**",
      "apps/watch/**",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
);
