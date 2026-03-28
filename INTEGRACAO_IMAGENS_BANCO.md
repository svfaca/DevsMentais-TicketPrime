# Integração de Imagens com Banco de Dados

## 🎯 Objetivo
Conectar as imagens dos eventos ao banco de dados, ao invés de usar hardcoding no frontend.

## ✅ Mudanças Implementadas

### 1. **Backend (Program.cs)**

#### Schema do Banco
```sql
ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS ImagemUrl TEXT;
```

#### Endpoints Atualizados

**GET /api/eventos/publico** - Agora retorna `ImagemUrl`
```csharp
SELECT e.Id, e.Nome, e.CapacidadeTotal, e.DataEvento, e.PrecoPadrao, e.ImagemUrl
```

**GET /api/eventos** (Admin) - Também retorna `ImagemUrl` 
```csharp
SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, ImagemUrl
```

**POST /api/eventos** - Agora aceita `ImagemUrl` na requisição
```csharp
INSERT INTO Eventos (..., ImagemUrl)
VALUES (..., @ImagemUrl)
```

#### Record Atualizado
```csharp
record CriarEventoRequest(string Nome, int CapacidadeTotal, DateTime DataEvento, decimal PrecoPadrao, string? ImagemUrl);
```

### 2. **Frontend (events-hero.js)**

Antes:
```javascript
// Hardcoded e mapeava por nome
const imagem = IMAGENS_EVENTOS[nome] || IMAGENS_DISPONIVEIS[index];
```

Depois:
```javascript
// Usa a imagem do banco, com fallback
let imagem = evento.imagemUrl ?? evento.ImagemUrl;
if (!imagem) {
    imagem = IMAGENS_EVENTOS[nome] || IMAGENS_DISPONIVEIS[index];
}
```

### 3. **Scripts SQL**

Criado: `db/populate_eventos_imagens.sql`
- Popula eventos existentes com seus respectivos caminhos de imagem
- Usa `ILIKE` para match flexível de nomes

## 📊 Fluxo de Dados

```
Backend (DB):
Eventos.ImagemUrl = './imagens/leo.jpg'
         ↓
API GET /api/eventos/publico
         ↓
Response: { id: 1, nome: 'Leo Santana', ImagemUrl: './imagens/leo.jpg' }
         ↓
Frontend (JS):
evento.ImagemUrl = './imagens/leo.jpg'
         ↓
<div style="backgroundImage: url('./imagens/leo.jpg')">
```

## 🚀 Como Usar

### Ao criar um novo evento (Admin)
```json
POST /api/eventos
{
  "nome": "Novo Evento",
  "capacidadeTotal": 1000,
  "dataEvento": "2026-12-01",
  "precoPadrao": 150.00,
  "imagemUrl": "./imagens/minha-imagem.jpg"
}
```

### Eventsexistentes (Popular via SQL)
Execute o arquivo `db/populate_eventos_imagens.sql` no seu banco para associar imagens aos eventos existentes.

## 📋 Status

✅ Backend compilado com sucesso
✅ Coluna `ImagemUrl` adicionada ao schema
✅ Endpoints retornam imagem
✅ Frontend usa imagem da API
✅ Script SQL criado para popular dados

## 🔄 Próximas Ações

1. **Aplicar migração no banco**: Execute `db/populate_eventos_imagens.sql`
2. **Reiniciar API**: `dotnet run` em `src/TicketPrime.Api`
3. **Testar no frontend**: Verificar se as imagens carregam corretamente
4. **Opcional**: Criar endpoint para upload de imagens (futuro)

## 📁 Arquivos Modificados

- `src/TicketPrime.Api/Program.cs` - Endpoints e Schema
- `src/frontend/js/events-hero.js` - Lógica de carregamento de imagem
- `db/populate_eventos_imagens.sql` - Script de migração (novo)
