# TicketPrime

Plataforma de venda de ingressos para categorias nichadas (musicais, cinema, eventos-diversos e viagens). Sistema completo com frontend responsivo, API REST em ASP.NET Core e banco de dados PostgreSQL.

## 🚀 Stack Tecnológico

### Backend
- **Framework:** ASP.NET Core (.NET 8+)
- **ORM/QueryBuilder:** Dapper
- **Banco de Dados:** PostgreSQL (Neon)
- **Autenticação:** JWT (JSON Web Tokens)
- **API Documentation:** Swagger/OpenAPI

### Frontend
- **HTML5** com estrutura semântica
- **CSS3** com temas (light/dark mode)
- **JavaScript Vanilla** para interatividade
- **Responsivo** para desktop e mobile

## 📁 Estrutura do Projeto

```
TicketPrime/
├── src/
│   ├── frontend/              # Frontend (HTML/CSS/JS)
│   │   ├── index.html
│   │   ├── style.css
│   │   ├── js/
│   │   │   ├── events-hero.js        # Carousel dinâmico de eventos
│   │   │   ├── theme-toggle.js       # Toggle de tema (light/dark)
│   │   │   └── home-carousel.js
│   │   └── imagens/
│   └── Backend/              # Backend API
│       ├── Program.cs          # Configuração e rotas da API
│       └── appsettings.json    # Configuração de ambiente
├── tests/
│   └── TicketPrime.Tests/     # Testes unitários
├── docs/
│   ├── requisitos.md          # Histórias de usuário e critérios BDD
│   └── Avaliacoes_ALUNO.pdf
└── db/
    └── ticketprime.sql        # Script de criação do banco
```

## ⚙️ Configuração e Execução

### Pré-requisitos
- .NET 8 ou superior
- PostgreSQL 12+
- Node.js/npm (opcional, se usar ferramentas frontend)

### Backend Setup

1. **Clonar repositório**
```bash
git clone https://github.com/svfaca/DevsMentais-TicketPrime.git
cd DevsMentais-TicketPrime
```

2. **Configurar banco de dados**
```bash
# Editar src/Backend/appsettings.json com suas credenciais PostgreSQL
# Exemplo:
{
  "ConnectionStrings": {
    "NeonDB": "Server=endpoint.neon.tech;Database=ticketprime;User Id=user;Password=pass;"
  }
}
```

3. **Executar API**
```bash
cd src/Backend
dotnet restore
dotnet run
```

A API estará disponível em: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

4. **Executar testes**
```bash
cd tests/TicketPrime.Tests
dotnet test
```

### Frontend

Simplesmente abrir `src/frontend/index.html` em um navegador web.

## 📚 Endpoints da API

### ✅ Endpoints Obrigatórios da Avaliação (AV1)
- `GET /api/eventos` - Listar eventos disponíveis (público)
- `POST /api/eventos` - Criar evento (admin)
- `POST /api/cupons` - Criar cupom (admin)
- `POST /api/usuarios` - Criar usuário (público)

### Autenticação (Endpoints Adicionais)
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Login com CPF e senha
- `POST /api/auth/bootstrap-admin` - Criar primeiro admin
- `POST /api/auth/register-admin` - Criar novo admin (apenas admin)

### Endpoints Adicionais

**Usuários**
- `GET /api/me` - Dados do usuário autenticado
- `PUT /api/me` - Atualizar perfil do usuário
- `DELETE /api/me` - Deletar conta do usuário
- `POST /api/me/foto` - Upload de foto de perfil

**Eventos**
- `GET /api/eventos/publico` - Listar eventos de admins ativos (não obrigatório)
- `GET /api/eventos/publico?categoria=musicais|cinema|eventos-diversos|viagens` - Filtrar eventos públicos por categoria
- `GET /api/eventos?categoria=musicais|cinema|eventos-diversos|viagens` - Filtrar eventos por categoria
- `GET /api/eventos/categorias` - Listar categorias válidas para eventos
- `PUT /api/eventos/{id}` - Atualizar evento (admin dono do evento)
- `DELETE /api/eventos/{id}` - Inativar evento (exclusão lógica, admin dono do evento)
- `GET /api/eventos/{id}/assentos-ocupados` - Verificar assentos

**Viagens (detalhes por evento)**
- `GET /api/eventos/{eventoId}/viagem` - Consultar detalhes de viagem (evento da categoria viagens)
- `PUT /api/eventos/{eventoId}/viagem` - Criar/atualizar detalhes de viagem (admin dono do evento)
- `DELETE /api/eventos/{eventoId}/viagem` - Remover detalhes de viagem (admin dono do evento)

**Tipos de ingresso**
- `GET /api/eventos/{eventoId}/tipos-ingresso` - Listar tipos de ingresso ativos de um evento (público)
- `GET /api/tipos-ingresso` - Listar tipos de ingresso dos eventos do admin autenticado
- `POST /api/eventos/{eventoId}/tipos-ingresso` - Criar tipo de ingresso (admin dono do evento)
- `PUT /api/tipos-ingresso/{id}` - Atualizar tipo de ingresso (admin dono do evento)
- `DELETE /api/tipos-ingresso/{id}` - Desativar tipo de ingresso (admin dono do evento)

### Categorias de Evento

O backend agora suporta as categorias abaixo para eventos:

- `musicais`
- `cinema`
- `eventos-diversos`
- `viagens`

Ao criar evento (`POST /api/eventos`), o campo `Categoria` pode ser enviado no payload.
Se o campo não for informado, a API assume `musicais` por padrão.

**Reservas**
- `POST /api/reservas` - Criar reserva (aceita `TipoIngressoId` opcional)
- `GET /api/reservas` - Listar reservas do usuário
- `DELETE /api/reservas/{id}` - Cancelar reserva

Regra por categoria na reserva:
- `musicais` e `cinema`: assento obrigatório
- `eventos-diversos` e `viagens`: assento opcional

**Cupons**
- `GET /api/cupons` - Listar cupons (admin)

## 🎨 Features Frontend

✅ **Carousel dinâmico** com eventos em tempo real  
✅ **Tema claro/escuro** persistido em localStorage  
✅ **Páginas responsivas** para desktop e mobile  
✅ **Grid de eventos** com busca visual  
✅ **Autenticação** com páginas separadas (user/admin)

## 🧪 Testes

O projeto inclui testes unitários. Execute com:
```bash
cd tests/TicketPrime.Tests
dotnet test --verbosity normal
```

## 📖 Documentação

- **Requisitos:** Ver [docs/requisitos.md](docs/requisitos.md)
- **API Swagger:** Disponível em `/swagger` quando a API estiver rodando
- **SQL Schema:** Ver [db/ticketprime.sql](db/ticketprime.sql)
- **Migrações incrementais:** Ver scripts em [db/](db/)

## 🔐 Segurança

- Senhas hashadas com SHA-512
- Autenticação por JWT
- Validação de CPF duplicado
- Proteção de rotas admin

## 👥 Autores

- **Desenvolvedores:** @jvdellolio, @MiriamSsntos, @patrickmqs e @svfaca.

- **Disciplina:** Engenharia de Software - Projeto TicketPrime

## 📝 Licença

Este projeto é fornecido para fins educacionais.
