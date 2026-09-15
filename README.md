# Date Map — backend MVP

Fundação do Date Map em .NET 10: API REST, domínio, PostgreSQL/PostGIS, JWT e o fluxo `Like recíproco → Match → proposta de Date → aceite → SCHEDULED`.

## Arquitetura

- `src/DateMap.Domain`: entidades, enums, invariantes e máquina de estados; não depende de ASP.NET Core, EF ou Infrastructure.
- `src/DateMap.Application`: contratos, DTOs e políticas de aplicação.
- `src/DateMap.Infrastructure`: EF Core, PostgreSQL/PostGIS, hashing e JWT.
- `src/DateMap.Api`: controllers, autenticação, Swagger e ProblemDetails.
- `tests`: testes de domínio, aplicação e integração HTTP.

## Requisitos

- .NET SDK 10.0.401 ou compatível
- PostgreSQL 17 em `localhost:5432`
- PostGIS instalado no servidor PostgreSQL

## Banco e configuração segura

Crie somente o banco deste projeto e habilite o PostGIS (execute com seu usuário administrativo local):

```sql
CREATE DATABASE datemap_dev;
\connect datemap_dev
CREATE EXTENSION IF NOT EXISTS postgis;
```

As credenciais não ficam no repositório. Configure user-secrets na API:

```powershell
dotnet user-secrets init --project src/DateMap.Api
dotnet user-secrets set "ConnectionStrings:DateMap" "Host=localhost;Port=5432;Database=datemap_dev;Username=SEU_USUARIO;Password=SUA_SENHA" --project src/DateMap.Api
dotnet user-secrets set "Jwt:Key" "UMA-CHAVE-ALEATORIA-COM-PELO-MENOS-32-BYTES" --project src/DateMap.Api
```

Para comandos do EF, defina `DATEMAP_CONNECTION_STRING` apenas na sessão atual. Não grave esse valor em arquivo versionado.

## Executar

```powershell
dotnet tool restore
$env:DATEMAP_CONNECTION_STRING="Host=localhost;Port=5432;Database=datemap_dev;Username=SEU_USUARIO;Password=SUA_SENHA"
dotnet ef database update --project src/DateMap.Infrastructure --startup-project src/DateMap.Infrastructure
dotnet run --project src/DateMap.Api
```

O Swagger fica em `https://localhost:PORT/swagger`. Use `Authorize` com o token retornado no register/login.

## Testes e build

```powershell
dotnet build DateMap.sln
dotnet test DateMap.sln
```

O teste de integração usa banco EF InMemory isolado e percorre o cenário HTTP completo, sem tocar no banco de desenvolvimento.

## Fluxo principal

1. Registre A e B em `POST /api/auth/register`.
2. Complete cada perfil em `PUT /api/profile/me` usando seu respectivo JWT.
3. A curte B e B curte A em `POST /api/likes/{profileId}`; o segundo like retorna `matchCreated: true` e `matchId`.
4. Liste estabelecimentos em `GET /api/venues` (opcionalmente `?category=Restaurant`).
5. Um participante propõe em `POST /api/dates` com `matchId`, `venueId` e `scheduledAt` em UTC.
6. O convidado chama `POST /api/dates/{id}/accept`; o DateEvent passa a `SCHEDULED`.

Alterações de status são operações explícitas (`accept`, `decline`, `cancel`); não existe PUT genérico de status.
