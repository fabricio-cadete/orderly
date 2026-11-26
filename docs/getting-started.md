# Como começar

Pré-requisitos: Node 22, pnpm 10 (`corepack enable`), .NET SDK 10, Docker. Para o desktop, Rust estável. Para iOS/Watch, Xcode.

```bash
pnpm install
pnpm build         # pacotes e apps JS
pnpm typecheck
pnpm lint

cd infrastructure && docker compose up --build   # API + PostgreSQL
```

Rodar um app:

```bash
pnpm --filter @orderly/web dev
pnpm --filter @orderly/desktop tauri:dev
pnpm --filter @orderly/mobile dev
```
