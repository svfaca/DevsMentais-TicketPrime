# Correção TicketPrime (DevsMentais-TicketPrime) — AV1

## Resumo

| Avaliação | Nota |
| --- | --- |
| **AV1** | **10 / 10** |

---

## AV1 — Detalhamento

| Item | Critério | Resultado | Justificativa |
| --- | --- | --- | --- |
| 1 | `docs/requisitos.md` com pelo menos 3 blocos contendo `Como`, `Quero` e `Para` | ✅ 1 | O arquivo possui 7 histórias de usuário (US01 a US07), todas no formato `Como <papel>, Quero <ação>, Para <objetivo>`. Exemplo: US01 "Como usuário, Quero me cadastrar informando CPF, nome, email e senha, Para ter meus dados registrados no sistema." |
| 2 | `docs/requisitos.md` com pelo menos 1 cenário contendo `Dado`, `Quando` e `Então` | ✅ 1 | Seção "Critérios de Aceitação (BDD)" traz 8 cenários. Exemplo: "Dado que um usuário deseja se cadastrar no sistema / Quando ele envia um CPF que já está cadastrado / Então a API deve retornar erro 400 informando que o CPF já existe." |
| 3 | `README.md` com blocos de código Markdown contendo comandos de terminal (ex: `dotnet run`, `dotnet build`) | ✅ 1 | O README contém blocos ```bash com `dotnet restore`, `dotnet run` e `dotnet test --verbosity normal` nas seções de setup da API e execução dos testes. |
| 4 | Pasta `/db` com arquivo `.sql` contendo `CREATE TABLE` | ✅ 1 | `db/ticketprime.sql` possui `CREATE TABLE` para `Usuarios`, `Eventos`, `Cupons` e `Reservas`, com chaves primárias, `IDENTITY` e chaves estrangeiras. Arquivos auxiliares (`add_profile_columns.sql`, `insert_eventos.sql` etc.) reforçam o versionamento. |
| 5 | `/src` com arquivos `.cs` contendo `app.MapGet` ou `app.MapPost` | ✅ 1 | `src/TicketPrime.Api/Program.cs` registra mais de 20 rotas com `app.MapGet`, `app.MapPost`, `app.MapPut` e `app.MapDelete` (ex: `/api/eventos`, `/api/cupons`, `/api/reservas`, `/api/auth/login`, `/api/me`). |
| 6 | `/src` com retornos explícitos de `Results.BadRequest` ou `Results.NotFound` | ✅ 1 | Program.cs usa fail-fast em muitos handlers. Exemplos: `Results.BadRequest("Nome do evento é obrigatório")`, `Results.BadRequest("Capacidade deve ser maior que zero")`, `Results.NotFound("Evento nao encontrado.")`, `Results.NotFound("Reserva nao encontrada.")`. |
| 7 | Uso do caractere `@` nas strings de query Dapper | ✅ 1 | Todas as queries com Dapper usam parâmetros nomeados: `@Cpf`, `@Id`, `@Email`, `@Nome`, `@Codigo`, `@EventoId`, `@CriadoPorCpf`, entre outros. Ex: `"SELECT ... WHERE Cpf = @Cpf"`, `"INSERT INTO Eventos (...) VALUES (@Nome, @CapacidadeTotal, ...)"`. |
| 8 | Não usar `+` nem interpolação `$"{ }"` em comandos `SELECT/INSERT/UPDATE/DELETE` | ✅ 1 | Buscas por `$"...SELECT/INSERT/UPDATE/DELETE"` e por concatenação com `+` em strings de SQL não retornaram ocorrências. Todas as queries são literais ou `@"..."` verbatim com parâmetros. As interpolações `$"..."` encontradas no código aparecem apenas em mensagens de erro para `Results.BadRequest` (ex: `$"O assento '{conflito}' ja esta reservado."`), sem tocar em SQL. |
| 9 | `/tests` com `.cs` contendo `[Fact]` ou `[Theory]` | ✅ 1 | `tests/TicketPrime.Tests/BasicTests.cs` contém 6 métodos decorados com `[Fact]`, e o arquivo inicia com `using Xunit;`, provando que o xUnit está configurado. |
| 10 | `Assert.` dentro dos métodos de teste | ✅ 1 | Todos os 6 testes contêm chamadas `Assert.`: `Assert.Equal`, `Assert.Contains`, `Assert.True`, `Assert.InRange`, `Assert.False`. Nenhum teste sem Assert. |

**Total AV1: 10 / 10**

---

## Justificativa da nota final

O projeto **DevsMentais-TicketPrime** atinge nota cheia na AV1. Os pontos que sustentam a pontuação:

- **Documentação de requisitos** com 7 histórias de usuário bem formatadas e 8 cenários BDD cobrindo cadastro, eventos, reservas (inclusive conflito de assento) e login.
- **README executável** com instruções claras de pré-requisitos, setup do banco, blocos de comando `dotnet restore / dotnet run / dotnet test`, lista de endpoints e notas sobre segurança.
- **Banco versionado** por script principal `db/ticketprime.sql` criando as 4 tabelas obrigatórias, com FKs consistentes, além de scripts auxiliares de migração e seed.
- **Minimal API robusta** em `Program.cs` com mapeamento explícito de rotas HTTP para todos os recursos exigidos (eventos, cupons, reservas, usuários, auth) e validações fail-fast retornando `BadRequest`/`NotFound`.
- **Acesso a dados seguro** via Dapper com parâmetros nomeados (`@param`) em todas as queries. Nenhuma concatenação nem interpolação de SQL foi encontrada, mesmo em handlers complexos como criação de reserva com verificação de assentos ocupados.
- **Testes xUnit** presentes em `BasicTests.cs`, com `[Fact]` e `Assert.` cobrindo validações básicas (CPF, email, capacidade, desconto, preço, nome de evento).

Todos os 10 critérios da AV1 foram atendidos sem ressalvas. **Nota final AV1: 10 / 10.**
