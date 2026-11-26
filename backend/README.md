# backend

API única do Orderly em **ASP.NET Core / .NET 10**, usada por todos os clientes (iPhone, desktop, web, Watch e widgets).

| Projeto | Papel |
| --- | --- |
| `src/Orderly.Api` | Endpoints REST, autenticação (Fase 1), hub SignalR `/hubs/sync` |
| `src/Orderly.Domain` | Entidades e regras de negócio, sem dependências |
| `src/Orderly.Infrastructure` | EF Core + PostgreSQL (Npgsql) |
| `tests/Orderly.Api.Tests` | Testes de integração com `WebApplicationFactory` + PostgreSQL |

## Rodar

```bash
docker compose -f ../infrastructure/docker-compose.yml up -d postgres
dotnet run --project src/Orderly.Api   # http://localhost:5080/health
dotnet test
```

Ou tudo em Docker: `docker compose -f ../infrastructure/docker-compose.yml up --build`.

Os testes de integração sobem um PostgreSQL com Testcontainers (precisa de Docker). Sem Docker, aponte
para um banco descartável: `ORDERLY_TEST_CONNECTION="Host=localhost;Port=5432;Database=orderly_test;Username=..." dotnet test`.

Em `Development` a API aplica as migrações ao subir (`Database:MigrateOnStartup`). Para criar uma migração:

```bash
dotnet tool restore
dotnet ef migrations add NomeDaMigracao --project src/Orderly.Infrastructure --startup-project src/Orderly.Api --output-dir Persistence/Migrations
```

## Autenticação

Sem senha. Os clientes fazem login de uma destas formas e recebem um **JWT de acesso** (15 min) e um
**refresh token** (30 dias, trocado a cada uso; reusar um token antigo derruba todas as sessões do usuário).

| Endpoint | O que faz |
| --- | --- |
| `POST /auth/magic-link` `{ email }` | Envia o link mágico (vale 15 min, uso único). Sempre responde 202. |
| `POST /auth/magic-link/verify` `{ token }` | Troca o token do link pela sessão |
| `POST /auth/apple` `{ idToken, name? }` | Login com Apple (o app manda o id_token; `name` só vem no 1º login) |
| `POST /auth/google` `{ idToken }` | Login com Google |
| `POST /auth/refresh` `{ refreshToken }` | Renova a sessão |
| `POST /auth/logout` `{ refreshToken }` | Encerra a sessão |
| `GET /me` | Usuário logado |

Contas com o mesmo e-mail verificado são unidas (entrar por link mágico e depois pela Apple cai no mesmo usuário).
Os endpoints de `/auth` têm limite de requisições por IP (`Auth:RateLimit:PermitsPerMinute`).
O hub `/hubs/sync` também exige login (token em `?access_token=`).

### Configuração (pendente de credenciais)

| Chave | Para que serve | Sem ela |
| --- | --- | --- |
| `Auth:Jwt:SigningKey` | Chave HMAC (≥ 32 bytes) que assina os JWTs | Em `Development` usa uma chave fixa de dev; fora dele a API não sobe |
| `Auth:Apple:ClientIds` | Bundle ID / Services ID aceitos no id_token da Apple | `/auth/apple` responde 503 |
| `Auth:Google:ClientIds` | OAuth Client IDs (iOS, web...) aceitos no id_token do Google | `/auth/google` responde 503 |
| `Auth:MagicLink:LinkBaseUrl` | Endereço que o e-mail abre (web ou deep link do app) | `http://localhost:5173/auth/magic` |
| `Email:Smtp:Host` (+ `Port`, `Username`, `Password`) e `Email:From` | Envio real dos e-mails | O e-mail (com o link) só aparece no log da API |

Em variáveis de ambiente, use `__` no lugar de `:` (ex.: `Auth__Google__ClientIds__0=...`).

## Endpoints de dados

Todos exigem login e só enxergam dados do próprio usuário (filtro global no EF Core).

- `/projects` e `/categories`: `GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`. Apagar um projeto
  deixa as tarefas sem projeto; apagar uma categoria só a remove das tarefas.
- `/tasks`: `GET` (filtros `projectId`, `categoryId`, `completed`), `GET /{id}`, `POST`, `PUT /{id}`,
  `DELETE /{id}`, `POST /{id}/complete`, `POST /{id}/reopen`.
- `GET /tasks/today?timeZone=America/Sao_Paulo`: tarefas abertas sem data, vencidas ou que vencem hoje
  no fuso informado (padrão UTC).

Tarefa: `title`, `notes`, `priority` (`P1`…`P4`, padrão `P4`), `projectId`, `dueDate` (ISO 8601 com fuso),
`dueHasTime` (falso quando é só o dia), `categoryIds`.
