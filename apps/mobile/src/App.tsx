import { brandName, resolveColorScheme, slogan, themes, typography } from "@orderly/ui";
import { StatusBar } from "expo-status-bar";
import { StyleSheet, Text, useColorScheme, View } from "react-native";

export function App() {
  const scheme = resolveColorScheme("system", useColorScheme() === "light" ? "light" : "dark");
  const colors = themes[scheme];

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <Text style={[styles.title, { color: colors.text }]}>{brandName}</Text>
      <Text style={{ color: colors.textSecondary, fontSize: typography.size.md }}>{slogan}</Text>
      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
  },
  title: {
    fontSize: typography.size.xl,
    fontWeight: "700",
  },
});
