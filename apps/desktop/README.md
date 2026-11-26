# @orderly/desktop

App desktop para **Windows e macOS** em React + Vite empacotado com **Tauri 2**.

- A interface segue as telas "Mac" do Design v1 (sidebar + lista + painel de detalhe).
- No Windows usar os controles de janela nativos (à direita) e `Ctrl` no lugar de `⌘` nos atalhos.

## Rodar

Pré-requisitos: Node 22, pnpm, Rust estável e as [dependências do Tauri](https://tauri.app/start/prerequisites/).

```bash
pnpm --filter @orderly/desktop dev        # só a interface, no navegador
pnpm --filter @orderly/desktop tauri:dev  # app nativo
```

> Os ícones em `src-tauri/icons` são provisórios (check branco sobre roxo). Quando o logo final estiver pronto, gere de novo com `pnpm --filter @orderly/desktop tauri icon <logo.png>`.
