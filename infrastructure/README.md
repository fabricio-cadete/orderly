# infrastructure

Tudo que roda a API do Orderly fora do código: Docker, banco e, mais tarde, deploy.

- `docker-compose.yml`: PostgreSQL 17 + API (`http://localhost:5080`) para desenvolvimento local.
- `docker/api.Dockerfile`: imagem de produção da API (build multi-stage .NET 10).
- `.env.example`: variáveis do compose. Copie para `.env`.

```bash
cd infrastructure
docker compose up --build
curl http://localhost:5080/health
```

O deploy (provedor, domínio, secrets) ainda não foi decidido e entra quando o backend tiver funcionalidades.
