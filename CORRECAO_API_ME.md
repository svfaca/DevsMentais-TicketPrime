# Correção do Erro 405 - API /api/me

## Problema Identificado
- **Erro**: 405 Method Not Allowed em `localhost:5129/api/me`
- **Causa**: Backend só tinha `GET /api/me` implementado, mas o frontend tentava usar `PUT` e `DELETE` também

## Soluções Implementadas

### 1. Adição de Colunas no Banco de Dados
No método `EnsureAuthSchemaAsync` do `Program.cs`:
- Adicionada coluna `Telefone` (TEXT)
- Adicionada coluna `FotoPerfil` (TEXT)

Arquivo: `db/add_profile_columns.sql` (criado para aplicar manualmente se necessário)

### 2. Implementação de Novos Endpoints

#### PUT /api/me - Atualizar Perfil
- **Método**: PUT
- **Autenticação**: Token obrigatório
- **Body esperado**: 
```json
{
  "nome": "string",
  "email": "string",
  "telefone": "string (opcional)",
  "fotoPerfil": "string (opcional)"
}
```
- **Response**: Dados do usuário atualizado
- **Status codes**: 200 (sucesso), 400 (validação), 401 (não autenticado)

#### DELETE /api/me - Deletar Conta
- **Método**: DELETE
- **Autenticação**: Token obrigatório
- **Response**: `{ "mensagem": "Conta excluida com sucesso." }`
- **Status codes**: 200 (sucesso), 401 (não autenticado)

#### GET /api/me - Sincronizar Dados (já existia, apenas atualizado)
- Agora retorna também `Telefone` e `FotoPerfil`

### 3. Record para Requisição
Adicionado `AtualizarPerfilRequest`:
```csharp
record AtualizarPerfilRequest(string Nome, string Email, string? Telefone, string? FotoPerfil);
```

## Mudanças de Arquivo

### src/TicketPrime.Api/Program.cs
✅ Compilou sem erros
- Novos endpoints PUT e DELETE para `/api/me`
- Schema atualizado com novas colunas
- Record `AtualizarPerfilRequest` adicionado

## Como Testar

### Frontend (Homeauthuser.html)
Quando estiver logado, clique em "Settings" e:
1. **Atualizar foto de perfil**: Deve fazer `PUT /api/me` com sucesso
2. **Atualizar informações**: Deve fazer `PUT /api/me` com nome, email, telefone
3. **Deletar conta**: Deve fazer `DELETE /api/me` com sucesso

### Via curl (linha de comando)
```bash
# Obter dados do usuário
curl -H "Authorization: Bearer SEU_TOKEN" http://localhost:5129/api/me

# Atualizar perfil
curl -X PUT -H "Content-Type: application/json" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -d '{"nome":"Novo Nome","email":"novo@email.com","telefone":"11999999999"}' \
  http://localhost:5129/api/me

# Deletar conta
curl -X DELETE -H "Authorization: Bearer SEU_TOKEN" http://localhost:5129/api/me
```

## Status
✅ Backend: Corrigido e Compilado com sucesso
✅ API: Rodinando em http://localhost:5129
⏳ Próximo: Testar no frontend

## Notas Importantes
- As mudanças no schema são aplicadas automaticamente via `EnsureAuthSchemaAsync`
- Se estiver usando SQL Server em vez de PostgreSQL, execute o arquivo `db/add_profile_columns.sql` manualmente
- O frontend já faz as requisições corretas, agora o backend está pronto para recebê-las
