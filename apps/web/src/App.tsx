import { brandName, resolveColorScheme, slogan, themes, typography } from "@orderly/ui";
import { useEffect, useState } from "react";

function useSystemScheme() {
  const query = "(prefers-color-scheme: dark)";
  const [dark, setDark] = useState(() => window.matchMedia(query).matches);
  useEffect(() => {
    const media = window.matchMedia(query);
    const onChange = (event: MediaQueryListEvent) => setDark(event.matches);
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);
  return dark ? "dark" : "light";
}

export function App() {
  const scheme = resolveColorScheme("system", useSystemScheme());
  const colors = themes[scheme];

  return (
    <main
      style={{
        minHeight: "100vh",
        margin: 0,
        display: "grid",
        placeItems: "center",
        background: colors.background,
        color: colors.text,
        fontFamily: `${typography.fontFamily.default}, system-ui, sans-serif`,
      }}
    >
      <div style={{ textAlign: "center" }}>
        <h1 style={{ fontSize: typography.size.xl, margin: 0 }}>{brandName}</h1>
        <p style={{ color: colors.textSecondary }}>{slogan}</p>
      </div>
    </main>
  );
}
