# @orderly/mobile

App do iPhone (e Android no futuro) em **React Native + Expo**.

- Abas embaixo: Hoje / Em breve / Projetos / Buscar, com botão flutuante "+" (Design v1).
- Offline com **SQLite** (`expo-sqlite`), sincronizando com a API e SignalR (Fase 2).
- Fonte do sistema (SF Pro) no iOS.
- O Apple Watch e os widgets ficam ligados a este app; veja `apps/watch`.

```bash
pnpm --filter @orderly/mobile dev
```

Para gerar o projeto nativo iOS (necessário para Watch e widgets): `npx expo prebuild --platform ios`.
