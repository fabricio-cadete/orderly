# apps/watch

Apps nativos da Apple (Fase 3), escritos em **Swift/SwiftUI**:

| Pasta | Alvo | Tecnologia |
| --- | --- | --- |
| `OrderlyWatch/` | App do Apple Watch | SwiftUI + watchOS |
| `OrderlyWidgets/` | Widgets do iPhone (e macOS depois) | SwiftUI + WidgetKit |
| `Shared/` | Tokens de cor e modelos em Swift | Swift |

## Como se ligam ao iPhone

O Watch e os widgets **não** têm projeto próprio isolado: viram *targets* do projeto iOS gerado pelo Expo (`apps/mobile`, via `expo prebuild`).
Eles compartilham dados com o app do iPhone por um **App Group** (`group.app.orderly`) e, no Watch, por **WatchConnectivity**.
Quando precisarem de dados frescos, falam com a mesma API ASP.NET Core.

O projeto Xcode ainda não foi criado. Isso entra na Fase 3; por enquanto esta pasta guarda só os arquivos-fonte de referência.

`Shared/OrderlyTheme.swift` espelha os tokens de `packages/ui`. Ao mudar uma cor lá, mude aqui também.
