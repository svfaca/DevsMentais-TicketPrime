# ✅ CHECKLIST DE VERIFICAÇÃO - PRÉ DEPLOY RAILWAY

## 📋 Mudanças Aplicadas

### Program.cs
- [x] ✅ Swagger liberado em PRODUÇÃO (linhas ~51-55)
- [x] ✅ Rota `/` adicionada (linha ~63)
- [x] ✅ Rota `/health` adicionada (linha ~67)
- [x] ✅ Log da CONNECTION STRING (linha ~73)
- [x] ✅ Porta configurada corretamente (linha ~570)
- [x] ✅ Log da PORTA (linha ~570)

### Dockerfile
- [x] ✅ `ENV ASPNETCORE_ENVIRONMENT=Production` adicionado
- [x] ✅ `ENV ASPNETCORE_URLS` REMOVIDO (conflito de porta eliminado)

---

## 🧪 TESTES ANTES DE FAZER PUSH

### Passo 1: Build Local
```powershell
cd "c:\Users\Nosta\OneDrive\Projetos\DevsMentais-TicketPrime"
dotnet build --configuration Release
```
✅ **Esperado:** Build completa sem erros

---

### Passo 2: Rodar Localmente
```powershell
$env:PORT = "8080"
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DATABASE_URL = "postgresql://neondb_owner:npg_XWGeo2BU4dHi@ep-dawn-cherry-amuwhcoi-pooler.c-5.us-east-1.aws.neon.tech/neondb?sslmode=require"

cd "src\TicketPrime.Api"
dotnet run
```

✅ **Esperado nos LOGS:**
```
🔥 DATABASE_URL detectada: postgresql://...
🚀 App será executada na porta: 8080
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://0.0.0.0:8080
```

---

### Passo 3: Testar Endpoints Críticos

**Terminal Nova (PowerShell):**

#### 3a. Health Check Raiz
```powershell
curl -i http://localhost:8080/
```
✅ **Resposta esperada:**
```
HTTP/1.1 200 OK
Content-Type: application/json
{"status":"API online","timestamp":"2026-04-22T...Z"}
```

#### 3b. Health Check Específico
```powershell
curl -i http://localhost:8080/health
```
✅ **Resposta esperada:**
```
HTTP/1.1 200 OK
{"ok":true,"timestamp":"2026-04-22T...Z"}
```

#### 3c. Swagger
```powershell
curl -i http://localhost:8080/swagger/index.html
```
✅ **Resposta esperada:**
```
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
(... HTML da documentação)
```

#### 3d. Eventos Públicos
```powershell
curl -i http://localhost:8080/api/eventos/publico
```
✅ **Resposta esperada:**
```
HTTP/1.1 200 OK
Content-Type: application/json
[] (ou JSON com eventos)
```

---

### Passo 4: Build com Docker
```powershell
docker build -t ticketprime-api:latest .
```
✅ **Esperado:** Build completa com sucesso

---

### Passo 5: Run com Docker (Simular Railway)
```powershell
docker run -p 8080:8080 `
  -e PORT=8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e DATABASE_URL="postgresql://neondb_owner:npg_XWGeo2BU4dHi@ep-dawn-cherry-amuwhcoi-pooler.c-5.us-east-1.aws.neon.tech/neondb?sslmode=require" `
  ticketprime-api:latest
```

✅ **Esperado nos LOGS:**
```
🔥 DATABASE_URL detectada: postgresql://...
🚀 App será executada na porta: 8080
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://0.0.0.0:8080
```

#### Testar no Docker:
```powershell
curl -i http://localhost:8080/
```
✅ **Resposta esperada:** 200 OK (mesma do passo 3a)

---

## 🚂 CONFIGURAÇÃO NO RAILWAY

1. No Railway, crie/editar o projeto
2. Vá em **Variables**
3. Defina:

| Chave | Valor |
|-------|-------|
| `DATABASE_URL` | `postgresql://neondb_owner:npg_XWGeo2BU4dHi@ep-dawn-cherry-amuwhcoi-pooler.c-5.us-east-1.aws.neon.tech/neondb?sslmode=require` |
| `PORT` | `8080` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Auth__TokenSecret` | SEU_TOKEN_SECRETO_LONG_AQUI |
| `Auth__BootstrapAdminKey` | SUA_CHAVE_BOOTSTRAP_AQUI |

---

## 🚀 DEPLOY PARA RAILWAY

1. **Commit e Push das mudanças:**
```bash
git add -A
git commit -m "🔧 Corrigir configuração para deploy no Railway: adicionar health check, rota raiz, logs e porta corrigida"
git push origin main
```

2. **No Railway:**
   - Vá em Deploy
   - Novo Deploy (ou ative auto-deploy)
   - Monitore os logs

3. **Verificar nos Logs do Railway:**
```
🔥 DATABASE_URL detectada: postgresql://...
🚀 App será executada na porta: 8080
Now listening on: http://0.0.0.0:8080
```

4. **Testar URL do Railway:**
```
https://seu-app-id.up.railway.app/
```
✅ **Esperado:** Mesmo JSON do health check local

---

## 🆘 TROUBLESHOOTING

| Erro | Causa | Solução |
|------|-------|--------|
| Deploy falha (timeout) | BD não acessível | Verifique `DATABASE_URL` |
| 502 Bad Gateway | App não escuta em PORT | Confirme logs mostram `8080` |
| 404 em `/` | Código não compilou | Verify build output |
| Swagger não aparece | ASPNETCORE_ENVIRONMENT inválido | Confirme `Production` no Dockerfile |
| Restart loop | App trava ou exception | Verifique connection string |

---

## ✨ SUCESSO!

Quando você acessar `https://seu-app-id.up.railway.app/` e ver:
```json
{"status":"API online","timestamp":"2026-04-22T...Z"}
```

**🎉 PARABÉNS! DEPLOY FUNCIONANDO!**

---

## 📝 RESUMO DAS MUDANÇAS

```
Program.cs:
  - Swagger liberado (sem if Development)
  - Rota GET / adicionada
  - Rota GET /health adicionada
  - Log da BD adicionado
  - Porta simplificada e robusta
  - Log da porta adicionado

Dockerfile:
  - ENV ASPNETCORE_ENVIRONMENT=Production adicionado
  - ENV ASPNETCORE_URLS removido (conflito)

Railway:
  - DATABASE_URL configurada
  - PORT, ASPNETCORE_ENVIRONMENT, Auth variables configuradas
```

---

**Última verificação:** ✅ 22/04/2026
