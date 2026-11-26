# Arquitetura

Todos os clientes falam com **uma única API ASP.NET Core** (REST + autenticação + SignalR), que roda em Docker e persiste num **PostgreSQL central**.

```
 iPhone (RN/Expo) ─┐
   ├─ Apple Watch  │   (ligados ao app do iPhone: App Group + WatchConnectivity)
   └─ Widgets      │
 Desktop (Tauri) ──┼──► API ASP.NET Core (.NET 10, Docker) ──► PostgreSQL
 Web (React) ──────┘      REST · Auth · SignalR /hubs/sync
```

## Monorepo

| Pasta                 | Conteúdo                                                | Stack                               |
| --------------------- | ------------------------------------------------------- | ----------------------------------- |
| `apps/mobile`         | App do iPhone                                           | React Native + Expo, SQLite offline |
| `apps/desktop`        | App Windows e macOS                                     | React + Vite + Tauri 2              |
| `apps/web`            | Cliente web                                             | React + Vite                        |
| `apps/watch`          | Apple Watch e widgets                                   | SwiftUI, watchOS, WidgetKit         |
| `packages/ui`         | Tokens de design (cores, tipografia, espaçamento, tema) | TypeScript                          |
| `packages/core`       | Modelos e regras de domínio compartilhados              | TypeScript                          |
| `packages/api-client` | Cliente HTTP tipado da API                              | TypeScript                          |
| `backend`             | API, domínio, persistência e testes                     | ASP.NET Core, EF Core, Npgsql       |
| `infrastructure`      | Docker Compose e Dockerfiles                            | Docker                              |
| `docs`                | Documentação                                            | Markdown                            |

## Gerenciador de workspaces: pnpm + Turborepo

- **pnpm**: instalação rápida e com pouco disco, e não deixa um pacote importar dependência que não declarou. O Expo suporta pnpm em monorepo.
  Usamos `node-linker=hoisted` (`.npmrc`) porque o Metro do React Native resolve melhor pacotes nativos com `node_modules` achatado.
- **Turborepo**: roda `build`, `lint`, `typecheck` e `test` na ordem certa entre pacotes e guarda cache, o que acelera o CI.

## Decisões de produto que afetam o código

- Não existe Entrada: tarefa sem data cai em **Hoje**; existe a lista inteligente **Todas**.
- Projeto = lista com cor. Categoria = etiqueta transversal.
- Prioridades P1 a P4 (vermelho, laranja, azul, cinza). Roxo `#8B5CF6` só para ações e seleção.
- Aparência: Claro, Escuro ou Automático (padrão Automático).
- Login: Apple, Google e link mágico por e-mail (sem senha).
- Fonte Inter; SF Pro nos apps Apple.
