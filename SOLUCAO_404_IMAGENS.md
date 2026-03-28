# Solução: Erro 404 ao Carregar Imagens (localhost:3000)

## 🔴 Problema
```
GET http://127.0.0.1:3000/src/frontend/paginas/imagens/luan.jpg 404 (Not Found)
```

As imagens estavam sendo procuradas em **`/src/frontend/paginas/imagens/`** mas estão localizadas em **`/src/frontend/imagens/`**.

## 🔍 Causa Raiz

O arquivo `events-hero.js` usava **caminhos relativos** (`./imagens/luan.jpg`):

- Quando carregava de **`/index.html`** (raiz): `./imagens/` ✅ funcionava
- Quando carregava de **`/paginas/Homeauthuser.html`**: `./imagens/` ❌ procurava em `/paginas/imagens/`

**Por quê?** Caminhos relativos são resolvidos em relação ao arquivo HTML que carrega o script, não ao script em si.

## ✅ Solução

Alterou todos os caminhos para **absolutos** (relativos à raiz do servidor):

### Antes
```javascript
const IMAGENS_DISPONIVEIS = [
    './imagens/luan.jpg',        // ❌ Relativo
    './imagens/leo.jpg',
    ...
];
```

### Depois
```javascript
const IMAGENS_DISPONIVEIS = [
    '/imagens/luan.jpg',         // ✅ Absoluto
    '/imagens/leo.jpg',
    ...
];
```

## 📝 Arquivos Modificados

### 1. **src/frontend/js/events-hero.js**
- ✅ `IMAGENS_DISPONIVEIS` → 8 imagens com caminho `/imagens/`
- ✅ `IMAGENS_EVENTOS` → 8 mapeamentos com caminho `/imagens/`
- ✅ `slideInicial` → 2 ocorrências atualizadas
- ✅ `carregarEventosFictícios()` → todos os eventos com caminho `/imagens/`

### 2. **src/frontend/style.css**
- ✅ `.login-page` background-image → `/imagens/fundo.jpg`
- ✅ `.light-mode.login-page` background-image → `/imagens/fundo.jpg`

### 3. **db/populate_eventos_imagens.sql**
- ✅ Todos os `UPDATE` statements com caminho `/imagens/`

## 📊 Comparação de Caminhos

| Contexto | Caminho Relativo | Resolvido Para | Status |
|----------|-----------------|----------------|--------|
| `/index.html` | `./imagens/leo.jpg` | `/imagens/leo.jpg` | ✅ OK |
| `/paginas/Homeauthuser.html` | `./imagens/leo.jpg` | `/paginas/imagens/leo.jpg` | ❌ ERRO |
| Ambos | `/imagens/leo.jpg` | `/imagens/leo.jpg` | ✅ OK |

## 🚀 Resultado

```
✅ GET http://127.0.0.1:3000/imagens/luan.jpg 200 (OK)
✅ GET http://127.0.0.1:3000/imagens/leo.jpg 200 (OK)
✅ GET http://127.0.0.1:3000/imagens/ccxp.jpg 200 (OK)
```

## 💡 Dica Importante

Quando você tem caminhos em um arquivo JavaScript (`.js`):
- **Se o JS pode ser carregado de locais diferentes**: use caminhos **absolutos** (`/imagens/`)
- **Se o JS está sempre no mesmo diretório**: use caminhos **relativos** (`./imagens/`)
