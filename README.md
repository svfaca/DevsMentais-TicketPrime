# TicketPrime

Plataforma de ingressos focada em categorias nichadas com experiência visual dinâmica na home e nas páginas de categoria.

Categorias atuais:
- musicais
- cinema
- eventos-diversos
- viagens

## Stack

Backend:
- ASP.NET Core (.NET 8)
- Dapper
- PostgreSQL (Neon)
- JWT

Frontend:
- HTML + CSS + JavaScript Vanilla
- Hero dinâmico por categoria
- Troca dinâmica de logo e imagens

## Estrutura

Principais caminhos do projeto:
- [src/Backend](src/Backend)
- [src/frontend](src/frontend)
- [db](db)
- [tests/TicketPrime.Tests](tests/TicketPrime.Tests)
- [docs](docs)

Arquivos importantes:
- [src/Backend/Program.cs](src/Backend/Program.cs)
- [src/frontend/index.html](src/frontend/index.html)
- [src/frontend/style.css](src/frontend/style.css)
- [src/frontend/js/events-hero.js](src/frontend/js/events-hero.js)
- [src/frontend/paginas/categoria.html](src/frontend/paginas/categoria.html)

## Executando Localmente

Pré-requisitos:
- .NET SDK 8+
- PostgreSQL

1. Clonar o repositório

```bash
git clone https://github.com/svfaca/DevsMentais-TicketPrime.git
cd DevsMentais-TicketPrime
```

2. Configurar banco e connection string

- Execute os scripts da pasta [db](db) conforme necessário.
- Configure a string de conexão (recomendado via User Secrets no backend).

3. Subir o backend

```bash
cd src/Backend
dotnet restore
dotnet run
```

Ambiente local esperado:
- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger

4. Abrir o frontend

- Home: [src/frontend/index.html](src/frontend/index.html)
- Categorias: [src/frontend/paginas/categoria.html](src/frontend/paginas/categoria.html)

Observação:
- O frontend usa [src/frontend/config/api.js](src/frontend/config/api.js) para apontar para http://localhost:8080 em localhost/127.0.0.1.

## Funcionalidades Recentes

- Hero da home com visual premium e cores dinâmicas por imagem.
- Hero da categoria com paridade visual da home.
- Múltiplas imagens rotativas no hero para:
  - home (slide inicial)
  - viagens
- Troca dinâmica de logo:
  - home: muda conforme slide/categoria ativa
  - categoria: usa logo específica da categoria
- Normalização de imagem para suportar variações de campo da API (ex.: imagemurl, imagemUrl).

Assets utilizados:
- Logos em [src/frontend/imagens](src/frontend/imagens)
- Imagens da home em [src/frontend/imagens/home](src/frontend/imagens/home)
- Imagens de categorias em [src/frontend/imagens/categorias](src/frontend/imagens/categorias)

## Endpoints Principais

Autenticação:
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/bootstrap-admin
- POST /api/auth/register-admin

Perfil:
- GET /api/me
- PUT /api/me
- DELETE /api/me
- POST /api/me/foto

Eventos:
- GET /api/eventos/publico
- GET /api/eventos/publico?categoria=musicais|cinema|eventos-diversos|viagens
- GET /api/eventos
- GET /api/eventos?categoria=musicais|cinema|eventos-diversos|viagens
- PUT /api/eventos/{id}
- DELETE /api/eventos/{id}
- GET /api/eventos/{id}/assentos-ocupados

Viagens:
- GET /api/eventos/{eventoId}/viagem
- PUT /api/eventos/{eventoId}/viagem
- DELETE /api/eventos/{eventoId}/viagem

Tipos de ingresso:
- GET /api/eventos/{eventoId}/tipos-ingresso
- GET /api/tipos-ingresso
- POST /api/eventos/{eventoId}/tipos-ingresso
- PUT /api/tipos-ingresso/{id}
- DELETE /api/tipos-ingresso/{id}

Reservas:
- POST /api/reservas
- GET /api/reservas
- DELETE /api/reservas/{id}

Cupons:
- GET /api/cupons

## Regras de Categoria

Categorias válidas no backend:
- musicais
- cinema
- eventos-diversos
- viagens

Ao criar evento, o campo Categoria pode ser enviado no payload.
Se não for enviado, o padrão é musicais.

Regra de assento em reservas:
- musicais e cinema: assento obrigatório
- eventos-diversos e viagens: assento opcional

## Testes

```bash
cd tests/TicketPrime.Tests
dotnet test --verbosity normal
```

## Documentação

- Requisitos: [docs/requisitos.md](docs/requisitos.md)
- Operação: [docs/operacao.md](docs/operacao.md)
- ADR: [docs/adr.md](docs/adr.md)
- Changelog: [docs/changelog](docs/changelog)

## Segurança

- Senhas hashadas com SHA-512
- Autenticação JWT
- Proteção de rotas de admin
- Validação de CPF duplicado

## Autores

- @jvdellolio
- @MiriamSsntos
- @patrickmqs
- @svfaca

Projeto acadêmico da disciplina de Engenharia de Software.
