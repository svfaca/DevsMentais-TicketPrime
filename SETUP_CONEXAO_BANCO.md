# 🎟️ Setup - Conexão Frontend com Banco de Dados

## ✅ Alterações Realizadas

### 1. **Banco de Dados (SQL)**
- Adicionados 8 eventos na tabela `Eventos` em [db/ticketprime.sql](db/ticketprime.sql)
- Eventos adicionados:
  - Luan Santana - Para Sempre
  - Matue & Brandão - Encontro de Gerações
  - Leo Santana - Festa do Povo
  - Bienal Internacional do Livro 2026
  - Futurecom 2026
  - CCXP 2026
  - Kanye West - O Retorno
  - Swing Gala Festival 2026

### 2. **Frontend JavaScript**
- **Removidos dados hardcoded** de eventos em [src/frontend/js/events-hero.js](src/frontend/js/events-hero.js)
- **Adicionada conexão com API** para buscar eventos dinamicamente
- URL da API: `http://localhost:5129/api/eventos/publico`
- Implementado fallback com dados fictícios caso API não responda

### 3. **Componentes React**
- Atualizado [src/frontend/Cupons.jsx](src/frontend/Cupons.jsx) com porta correta da API (5129)
- Atualizado [src/frontend/Reservas.jsx](src/frontend/Reservas.jsx) com porta correta da API (5129)

## 🚀 Como Testar

### Pré-requisitos
- SQL Server com banco `TicketPrime` criado
- .NET 8 ou 9 instalado
- Node.js (opcional, para rodar frontend)

### Passo 1: Executar Script SQL
```sql
-- Executar o script em db/ticketprime.sql no seu SQL Server
-- Isso criará as tabelas e adicionará os eventos
```

### Passo 2: Iniciar a API
```bash
cd src/TicketPrime.Api
dotnet run
```
A API estará disponível em: `http://localhost:5129`

### Passo 3: Verificar Endpoint
Abra o navegador e acesse:
```
http://localhost:5129/api/eventos/publico
```
Você deve ver um JSON com os eventos do banco.

### Passo 4: Servir Frontend
```bash
# Opção 1: Usar Live Server do VS Code
# Clique direito em src/frontend/index.html -> Open with Live Server

# Opção 2: Usar Python
cd src/frontend
python -m http.server 8000
# Acesse http://localhost:8000
```

### Passo 5: Testar Página
- Abra `http://localhost:8000` (ou porta do Live Server)
- Verifique se o hero e grid de eventos carregam corretamente
- Verifique o console do navegador (F12) para erros

## 📋 Checklist de Validação

- ✅ Eventos estão apenas no banco (não mais hardcoded no JS)
- ✅ Frontend busca eventos da API
- ✅ Hero section exibe o primeiro evento
- ✅ Grid de eventos mostra todos os eventos
- ✅ Dados são carregados dinamicamente
- ✅ Fallback funciona se API não responder

## 🔧 Configurações Importantes

### URL da API
Arquivo: `src/frontend/js/events-hero.js` (linha 1-3)
```javascript
const API_URL = 'http://localhost:5129/api/eventos/publico';
```

### Componentes React
- `Cupons.jsx`: Linha 3
- `Reservas.jsx`: Linha 3

## 📝 Notas

- Os eventos agora vêm **100% do banco de dados**
- O slide inicial (TicketPrime - Bem-vindo) é gerado dinamicamente no JavaScript
- Se a API não estiver disponível, os dados fictícios serão usado como fallback
- Para adicionar novos eventos, insira diretamente na tabela `Eventos` ou crie um endpoint POST

## 🐛 Troubleshooting

### "API não responde"
- Verifique se `dotnet run` está rodando
- Confirme a porta (5129) em `Properties/launchSettings.json`
- Verifique se o CORS está habilitado na API

### "Erro de conexão com banco"
- Verifique `appsettings.json` - Connection String
- Confirme se SQL Server está rodando
- Verifique se banco `TicketPrime` existe

### "Eventos não aparecem"
- Abra DevTools (F12) e verifique a aba Network
- Veja se a requisição para `/api/eventos/publico` retorna 200
- Verifique o console para mensagens de erro

