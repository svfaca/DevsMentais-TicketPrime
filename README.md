# TicketPrime

Plataforma de venda de ingressos para eventos musicais. Sistema completo com frontend responsivo, API REST em ASP.NET Core e banco de dados PostgreSQL.

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
│   └── TicketPrime.Api/       # Backend API
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
# Editar src/TicketPrime.Api/appsettings.json com suas credenciais PostgreSQL
# Exemplo:
{
  "ConnectionStrings": {
    "NeonDB": "Server=endpoint.neon.tech;Database=ticketprime;User Id=user;Password=pass;"
  }
}
```

3. **Executar API**
```bash
cd src/TicketPrime.Api
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

### Autenticação
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Login com CPF e senha
- `POST /api/auth/bootstrap-admin` - Criar primeiro admin
- `POST /api/auth/register-admin` - Criar novo admin (apenas admin)

### Usuários
- `POST /api/usuarios` - Criar usuário
- `GET /api/me` - Dados do usuário autenticado

### Eventos
- `GET /api/eventos/publico` - Listar eventos (público)
- `GET /api/eventos` - Listar eventos (autenticado)
- `POST /api/eventos` - Criar evento (admin)
- `GET /api/eventos/{id}/assentos-ocupados` - Verificar assentos

### Reservas
- `POST /api/reservas` - Criar reserva
- `GET /api/reservas` - Listar reservas do usuário
- `DELETE /api/reservas/{id}` - Cancelar reserva

### Cupons
- `GET /api/cupons` - Listar cupons
- `POST /api/cupons` - Criar cupom (admin)

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

## 🔐 Segurança

- Senhas hashadas com SHA-512
- Autenticação por JWT
- Validação de CPF duplicado
- Proteção de rotas admin

## 👥 Autores

- **Desenvolvedores:** @svfaca, @patrickmqs, @MiriamSsntos e @jvdellolio.

- **Disciplina:** Engenharia de Software - Projeto TicketPrime

## 📝 Licença

Este projeto é fornecido para fins educacionais.
