# backend

API única do Orderly em **ASP.NET Core / .NET 10**, usada por todos os clientes (iPhone, desktop, web, Watch e widgets).

| Projeto | Papel |
| --- | --- |
| `src/Orderly.Api` | Endpoints REST, autenticação (Fase 1), hub SignalR `/hubs/sync` |
| `src/Orderly.Domain` | Entidades e regras de negócio, sem dependências |
| `src/Orderly.Infrastructure` | EF Core + PostgreSQL (Npgsql) |
| `tests/Orderly.Api.Tests` | Testes de integração com `WebApplicationFactory` |

## Rodar

```bash
docker compose -f ../infrastructure/docker-compose.yml up -d postgres
dotnet run --project src/Orderly.Api   # http://localhost:5080/health
dotnet test
```

Ou tudo em Docker: `docker compose -f ../infrastructure/docker-compose.yml up --build`.
