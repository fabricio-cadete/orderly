/**
 * Tokens de design do Orderly (Design v1).
 * Fonte única de cores, tipografia e espaçamento para mobile, desktop e web.
 * Os apps Apple nativos (watch, widgets) espelham estes valores em Swift.
 */

export const brand = {
  ink: "#111114",
  graphite: "#24242B",
  violet: "#8B5CF6",
  lavender: "#C4B5FD",
  mist: "#F4F4F5",
} as const;

export type ColorScheme = "light" | "dark";

/** Preferência do usuário. "system" (Automático) é o padrão e segue o sistema. */
export type AppearancePreference = "light" | "dark" | "system";

export const defaultAppearance: AppearancePreference = "system";

export interface ThemeColors {
  background: string;
  surface: string;
  border: string;
  text: string;
  textSecondary: string;
  primary: string;
  accentText: string;
  priority: Record<Priority, string>;
}

export type Priority = "p1" | "p2" | "p3" | "p4";

export const themes: Record<ColorScheme, ThemeColors> = {
  dark: {
    background: brand.ink,
    surface: brand.graphite,
    border: "#2E2E36",
    text: brand.mist,
    textSecondary: "#A1A1AA",
    primary: brand.violet,
    accentText: brand.lavender,
    priority: {
      p1: "#F87171",
      p2: "#FB923C",
      p3: "#60A5FA",
      p4: "#71717A",
    },
  },
  light: {
    background: "#F7F7F9",
    surface: "#FFFFFF",
    border: "#E4E4E7",
    text: brand.ink,
    textSecondary: "#5B5B66",
    primary: brand.violet,
    accentText: "#6D28D9",
    priority: {
      p1: "#DC2626",
      p2: "#EA580C",
      p3: "#2563EB",
      p4: "#A1A1AA",
    },
  },
};

export const typography = {
  /** Inter em web/desktop/Android; SF Pro (fonte do sistema) nos apps Apple. */
  fontFamily: {
    default: "Inter",
    apple: "System",
  },
  size: {
    xs: 12,
    sm: 14,
    md: 16,
    lg: 20,
    xl: 28,
  },
  weight: {
    regular: "400",
    medium: "500",
    semibold: "600",
    bold: "700",
  },
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

export const radius = {
  sm: 6,
  md: 10,
  lg: 16,
  full: 9999,
} as const;

/** Resolve a preferência do usuário para um esquema concreto. */
export function resolveColorScheme(
  preference: AppearancePreference,
  systemScheme: ColorScheme | null | undefined,
): ColorScheme {
  if (preference === "system") return systemScheme ?? "dark";
  return preference;
}

export const brandName = "Orderly.";
export const slogan = "Less chaos. More clarity.";
