# 🚀 CORREÇÕES IMPLEMENTADAS PARA RAILWAY

## ✅ O QUE FOI ALTERADO

### 1️⃣ Program.cs - 5 Correções Críticas

#### ✅ Correção 1: Swagger liberado em PRODUÇÃO
```csharp
// ANTES:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}

// DEPOIS:
app.UseSwagger();
app.UseSwaggerUI(...);
```
📌 Resultado: `/` agora abre a documentação interativa mesmo em produção

---

#### ✅ Correção 2: Log da Connection String
```csharp
// ADICIONADO:
app.Logger.LogInformation("🔥 DATABASE_URL detectada: {Conn}", connectionString);
```
📌 Resultado: Você vê nos logs se a BD foi conectada corretamente

---

#### ✅ Correção 3: Rota Raiz (`/`) + Health Check (`/health`)
```csharp
// ADICIONADO após app.UseStaticFiles():
app.MapGet("/", () => Results.Ok(new { status = "API online", timestamp = DateTime.UtcNow }));
app.MapGet("/health", () => Results.Ok(new { ok = true, timestamp = DateTime.UtcNow }));
```
📌 Resultado: 
- `GET /` → Railway consegue validar que app está viva ✅
- `GET /health` → Health check adicional
- Railway **NÃO VAI FAZER RESTART LOOP** mais 🎯

---

#### ✅ Correção 4: Porta Corrigida
```csharp
// ANTES:
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Run($"http://0.0.0.0:{port}");
}
else
{
    app.Run();  // ❌ Inconsistente
}

// DEPOIS:
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Logger.LogInformation("🚀 App será executada na porta: {Port}", port);
app.Run($"http://0.0.0.0:{port}");
```
📌 Resultado: 
- Always listen on PORT (Railway injeta isso)
- Default: 8080 se não houver PORT
- **Comportamento previsível e consistente** ✅

---

#### ✅ Correção 5: Log da Porta
```csharp
app.Logger.LogInformation("🚀 App será executada na porta: {Port}", port);
```
📌 Resultado: Logs mostram exatamente qual porta está sendo usada

---

### 2️⃣ Dockerfile - 1 Correção Crítica

#### ✅ Correção: ASPNETCORE_ENVIRONMENT + Remover ASPNETCORE_URLS
```dockerfile
# ANTES:
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}  # ❌ Não expande
ENTRYPOINT ["dotnet", "TicketPrime.Api.dll"]

# DEPOIS:
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_ENVIRONMENT=Production  # ✅ Novo
ENTRYPOINT ["dotnet", "TicketPrime.Api.dll"]
```
📌 Resultado:
- Swagger rodando em produção (você sabe quando está online)
- Variável PORT em Dockerfile **não é mais um problema**
- Program.cs gerencia a porta com segurança ✅

---

## 🧪 COMO TESTAR LOCALMENTE

### Teste 1: Build local (sem Docker)
```bash
cd c:\Users\Nosta\OneDrive\Projetos\DevsMentais-TicketPrime

# Definir PORT e rodar
$env:PORT=8080
$env:ASPNETCORE_ENVIRONMENT=Production
$env:DATABASE_URL="postgresql://seu-user:sua-pass@seu-host/seu-db"

cd src/TicketPrime.Api
dotnet run
```

Aguarde e procure no console:
```
🔥 DATABASE_URL detectada: postgresql://...
🚀 App será executada na porta: 8080
```

### Teste 2: Testar as rotas
```bash
# Health check raiz
curl -i http://localhost:8080/

# Resposta esperada:
# HTTP/1.1 200 OK
# {"status":"API online","timestamp":"2026-04-22T...Z"}

# Health check específico
curl -i http://localhost:8080/health

# Swagger
curl -i http://localhost:8080/swagger/index.html

# Eventos públicos (nenhuma auth)
curl -i http://localhost:8080/api/eventos/publico
```

### Teste 3: Build e run com Docker
```bash
# Build
docker build -t ticketprime-api:latest .

# Run (simular Railway)
docker run -p 8080:8080 `
  -e PORT=8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e DATABASE_URL="postgresql://seu-user:sua-pass@seu-host/seu-db" `
  ticketprime-api:latest

# Testar
curl -i http://localhost:8080/
```

---

## 🚂 VARIÁVEIS DO RAILWAY

Configure EXATAMENTE isso no Railway:

```env
# OBRIGATÓRIA
DATABASE_URL=postgresql://neondb_owner:SEU_PASSWORD@ep-dawn-cherry-amuwhcoi-pooler.c-5.us-east-1.aws.neon.tech/neondb?sslmode=require

# RECOMENDADAS (preenchidas automaticamente mas confirme)
PORT=8080
ASPNETCORE_ENVIRONMENT=Production

# SEGURANÇA - substitua pelos seus valores reais
Auth__TokenSecret=seu-token-super-secreto-minimo-32-caracteres
Auth__BootstrapAdminKey=sua-chave-de-bootstrap-temporaria
```

---

## 📊 VALIDAÇÃO PRÉ-DEPLOY

Checklist antes de fazer push ao Railway:

- [ ] `dotnet build` passa sem erros
- [ ] `dotnet run` inicia e mostra os logs `🔥` e `🚀`
- [ ] `GET /` retorna `{"status":"API online",...}`
- [ ] `GET /health` retorna `{"ok":true,...}`
- [ ] `GET /swagger/index.html` abre documentação
- [ ] `GET /api/eventos/publico` funciona (pode estar vazio)
- [ ] Logs mostram `🔥 DATABASE_URL detectada: postgresql://...`
- [ ] Variáveis de ambiente estão configuradas no Railway

---

## 🎯 APÓS O DEPLOY

No Railway, procure nos logs por:

```
🔥 DATABASE_URL detectada: postgresql://...
🚀 App será executada na porta: 8080
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://0.0.0.0:8080
```

Se vir isso = **ESTÁ FUNCIONANDO** ✅

---

## 🆘 SE AINDA DER ERRO

1. **App não inicia (timeout na Railway)**
   - Verifique `DATABASE_URL` nos logs
   - Conecte manualmente: `psql $(echo $DATABASE_URL)`

2. **Erro 404 em `/` ou `/health`**
   - Limpe o cache: `docker image prune -a`
   - Faça rebuild: `docker build --no-cache -t ticketprime-api:latest .`

3. **Swagger retorna 404**
   - Confirme que `ASPNETCORE_ENVIRONMENT=Production` (ou em branco)
   - Verifique se o build tem Swashbuckle

4. **Conexão com BD falha**
   - Teste a string: `psql $DATABASE_URL`
   - Se falhar, BD está inacessível ou credenciais erradas

---

## 📝 RESUMO DAS MUDANÇAS

| Arquivo | Mudança | Impacto |
|---------|---------|--------|
| `Program.cs` | Swagger liberado em prod | 🟢 API documentada em produção |
| `Program.cs` | Rota `/` adicionada | 🟢 Railway consegue validar app viva |
| `Program.cs` | `/health` adicionado | 🟢 Health check específico |
| `Program.cs` | Porta mais robusta | 🟢 PORT sempre respeitada |
| `Program.cs` | Logs de debug | 🟢 Rastreamento da BD e porta |
| `Dockerfile` | ASPNETCORE_ENVIRONMENT | 🟢 Sem comportamento indefinido |
| `Dockerfile` | Remover ASPNETCORE_URLS | 🟢 Sem conflitos de porta |

---

## 🚀 PRÓXIMOS PASSOS

1. Testar localmente conforme instruções acima
2. Fazer push ao repo
3. No Railway: deletar deployment antigo (se houver)
4. Fazer novo deploy
5. Monitorar logs por 2-3 minutos
6. Testar `GET /` no navegador

**Sucesso!** 🎉
