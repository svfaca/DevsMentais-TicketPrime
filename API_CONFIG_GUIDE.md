## 🔧 Arquitetura da Configuração da API - Centralizada

### 📁 Estrutura

```
src/frontend/
├── config/
│   └── api.js                    ← 🔑 FONTE ÚNICA DA CONFIGURAÇÃO
├── js/
│   ├── init-api-global.js        ← Expõe config como global para HTMLs
│   ├── events-hero.js            ← Importa de config/api.js
│   ├── home-carousel.js          ← Importa de config/api.js
│   └── components/
├── Cupons.jsx                    ← Importa de config/api.js
├── Reservas.jsx                  ← Importa de config/api.js
└── paginas/
    ├── login.html                ← Carrega init-api-global.js
    ├── Homeauthuser.html        ← Carrega init-api-global.js
    ├── Homeauthadm.html         ← Carrega init-api-global.js
    ├── meus-ingressos.html      ← Carrega init-api-global.js
    └── painel-ingressos.html    ← Carrega init-api-global.js
```

---

### 🎯 Fluxo de Dados

```
localStorage.getItem('API_URL')
          ↓
config/api.js (export const API_BASE_URL)
    ↓                               ↓
JS Modules (import)        HTML via init-api-global.js (window.API_BASE_URL)
  ↓                                 ↓
events-hero.js            login.html
home-carousel.js          Homeauthuser.html
Cupons.jsx               Homeauthadm.html
Reservas.jsx             meus-ingressos.html
                         painel-ingressos.html
```

---

### 📋 Arquivos Modificados

| Arquivo | Mudança |
|---------|---------|
| `config/api.js` | ✨ NOVO - Definição centralizada e exportada |
| `js/init-api-global.js` | ✨ NOVO - Expõe config como global |
| `js/events-hero.js` | 🔄 Importa de `../config/api.js` |
| `js/home-carousel.js` | 🔄 Importa de `../config/api.js` |
| `Cupons.jsx` | 🔄 Importa de `./config/api.js` |
| `Reservas.jsx` | 🔄 Importa de `./config/api.js` |
| `paginas/login.html` | 🔄 Carrega `../js/init-api-global.js` |
| `paginas/Homeauthuser.html` | 🔄 Carrega `../js/init-api-global.js` |
| `paginas/Homeauthadm.html` | 🔄 Carrega `../js/init-api-global.js` |
| `paginas/meus-ingressos.html` | 🔄 Carrega `../js/init-api-global.js` |
| `paginas/painel-ingressos.html` | 🔄 Carrega `../js/init-api-global.js` |

---

### 🚀 Como Usar

#### Em um JS/JSX module novo:
```javascript
import { API_BASE_URL } from '../config/api.js';

console.log(API_BASE_URL);
// Output: https://devsmentais-ticketprime-production.up.railway.app
```

#### Em um HTML novo:
```html
<!-- Adicionar no início do <body> -->
<script type="module" src="../js/init-api-global.js"></script>

<!-- Depois usar normalmente -->
<script>
  console.log(window.API_BASE_URL);
  // Output: https://devsmentais-ticketprime-production.up.railway.app
</script>
```

---

### ✅ Benefícios

- ✨ **DRY**: Definição única, sem duplicação
- 🔒 **Seguro**: localStorage + fallback automático
- 🌍 **Consistente**: Mesma lógica em todos os arquivos
- 🔄 **Fácil manutenção**: Alterar URL em um único lugar
- 📱 **Dev-friendly**: Funciona localhost e produção
- 🎯 **Profissional**: Padrão de arquitetura reconhecido

---

### 🔑 Estratégia de Fallback

```javascript
1. localStorage.getItem('API_URL')  // Permite override via localStorage
2. window.location.hostname === 'localhost' → 'http://localhost:5129'
3. Padrão de produção → 'https://devsmentais-ticketprime-production.up.railway.app'
```

**Para override local em dev:**
```javascript
localStorage.setItem('API_URL', 'http://seu-backend:porta');
// Recarregar página
```

---

### ⚠️ Notas

- HTMLs sincronos esperam até 100ms pelo carregamento do módulo (compatibilidade)
- Para novo módulo HTML, sempre manter `<script type="module">` **ANTES** dos scripts inline
- Certificar que nenhum script tenta acessar `API_BASE_URL` antes do módulo carregar
