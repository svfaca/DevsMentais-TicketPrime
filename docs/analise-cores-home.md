# Análise de Cores da Home - TicketPrime

## 1. COR PADRÃO DO PROJETO

### Paleta Primária de Fundo
A página home usa um **gradiente azul/escuro** como cor padrão:

```css
--cor-pagina-primary: #000000  (Preto - tom primário)
--cor-pagina-secondary: #111111 (Preto escuro - tom secundário)
```

**Aplicação no fundo:**
```css
background: linear-gradient(135deg, #000000 0%, #111111 55%, #1a1a1a 100%);
```

### Cores de Destaque Padrão (CTAs)
```css
--hero-accent: #ff9b43      (Laranja principal)
--hero-accent-alt: #ff6f2c  (Laranja secundário)
--hero-cta-text: #ffffff    (Branco para texto)
```

---

## 2. COMO A COLORAÇÃO É DEFINIDA

### 2.1 Variáveis CSS (CSS Custom Properties)
Todas as cores dinâmicas são definidas no `:root` no [style.css](../src/frontend/style.css#L3-L5):

```css
:root {
    --hero-accent: #ff9b43;
    --hero-accent-alt: #ff6f2c;
    --hero-cta-text: #ffffff;
}
```

Essas variáveis são **atualizadas dinamicamente via JavaScript** quando o usuário navega entre categorias.

### 2.2 Cores por Categoria (Definidas no JavaScript)
No arquivo [events-hero.js](../src/frontend/js/events-hero.js#L20-L60), cada categoria tem uma cor base:

```javascript
CATEGORY_DATA = [
    {
        slug: 'musicais',
        nome: 'Musicais',
        cor: '#6b4c9f'  // Roxo
    },
    {
        slug: 'cinema',
        nome: 'Cinema',
        cor: '#dc2626'  // Vermelho
    },
    {
        slug: 'eventos-diversos',
        cor: '#1e40af'  // Azul
    },
    {
        slug: 'copa',
        cor: '#166534'  // Verde
    },
    {
        slug: 'viagens',
        cor: '#0891b2'  // Ciano
    }
]
```

### 2.3 Cores da Navbar por Categoria
No CSS, existem "pills" (badges) com cores específicas:

```css
.navbar-category-pill.pill-musicais {
    background: rgba(169, 107, 255, 0.22);  /* Roxo semi-transparente */
    color: #ecd9ff;
    border-color: rgba(210, 170, 255, 0.55);
}

.navbar-category-pill.pill-cinema {
    background: rgba(255, 98, 98, 0.2);     /* Vermelho semi-transparente */
    color: #ffd7d7;
    border-color: rgba(255, 150, 150, 0.52);
}

.navbar-category-pill.pill-copa {
    background: rgba(52, 176, 111, 0.22);   /* Verde semi-transparente */
    color: #d7f8e8;
    border-color: rgba(113, 211, 156, 0.55);
}

.navbar-category-pill.pill-viagens {
    background: rgba(61, 186, 227, 0.24);   /* Ciano semi-transparente */
    color: #d5f6ff;
    border-color: rgba(124, 220, 248, 0.55);
}
```

---

## 3. ITENS COM CORES DINÂMICAS

### 3.1 Hero Section (Slider Principal)
O hero é o elemento **mais dinâmico** em cores. Quando o usuário navega entre categorias:

```javascript
async function recomputeItemColors(item) {
    // 1. Extrai cores da imagem de fundo
    const [c1, c2] = await getDualAverageColors(item.imagem);
    
    // 2. Mistura com a cor da categoria
    item.computedPrimary = blendHex(c1, item.cor, 0.42);
    item.computedSecondary = blendHex(c2, item.cor, 0.36);
}
```

**Elementos afetados no hero:**
- **Fundo do body** - gradiente dinâmico
- **Navbar (header)** - cor de fundo recalculada
- **Botões CTA** - gradiente de cores atualizadas
- **Logo da navbar** - muda por categoria
- **Pill (badge) ao lado do logo** - cor dinâmica complementar

### 3.2 Cálculo de Cores Dinâmicas
O projeto usa um algoritmo sofisticado para calcular cores:

1. **Extração de cores da imagem:**
   ```javascript
   async function getDualAverageColors(imageUrl) {
       // Divide a imagem em duas partes (topo e base)
       // Calcula a cor média de cada parte
       // Retorna [colorPrimaria, colorSecundaria]
   }
   ```

2. **Blend (Mistura) de cores:**
   ```javascript
   function blendHex(hexA, hexB, weight) {
       // Mistura duas cores com um peso específico
       // weight: 0..1 (quanto hexB contribui)
       // Exemplo: blendHex('#ff0000', '#0000ff', 0.5) = cor roxa
   }
   ```

3. **Validação de contraste:**
   ```javascript
   function isBrownish(hex) {
       // Verifica se a cor resultante ficou muito "marrom"
       // Se sim, aplica uma cor de fallback
   }
   ```

### 3.3 Casos Especiais

**Copa Brasil:**
Quando a imagem da Copa é do Brasil, cores especiais são aplicadas:

```javascript
if (item?.slug === 'copa' && isCopaBrazilImagePath(item.imagem)) {
    item.computedPrimary = '#0f6d31';      // Verde escuro
    item.computedSecondary = '#0a4f25';    // Verde muito escuro
    item.paletteOverride = {
        accent: '#FFD60A',                 // Amarelo
        accentAlt: '#FBC02D',              // Amarelo escuro
        ctaText: '#1f2937',                // Texto escuro
        pillHighlight: '#FFD60A'
    };
}
```

### 3.4 Navegação (Navbar)
- **Logo**: Muda entre 6 variações por categoria
- **Pill (badge)**: Cor dinâmica e complementar
- **Background**: Mistura da cor primária com a cor da categoria

### 3.5 Grid de Categorias
Todas as cards usam a cor padrão (laranja):
```css
.event-card {
    background: linear-gradient(135deg, rgba(255, 107, 53, 0.1), rgba(247, 147, 30, 0.05));
}

.event-card:hover {
    border-color: rgba(255, 107, 53, 0.4);
}
```

---

## 4. ONDE AS CORES SÃO VINCULADAS

### 4.1 Arquivo: `style.css`
- **Linhas 1-5**: Variáveis CSS raiz (`:root`)
- **Linhas 14-21**: Fundo do body com gradiente
- **Linhas 27-30**: Fundo da home com gradiente
- **Linhas 39-57**: Navbar com gradiente dinâmico usando `color-mix`
- **Linhas 81-163**: Pills de categorias com cores específicas
- **Linhas 590-710**: Hero section (cores dinâmicas via CSS variables)
- **Linhas 1007-1100**: Grid de eventos com cores padrão

### 4.2 Arquivo: `events-hero.js`
**Linhas-chave onde cores são gerenciadas:**

1. **Definição de cores base (linhas 20-60):**
   ```javascript
   const CATEGORY_DATA = [
       { slug: 'musicais', cor: '#6b4c9f' },
       { slug: 'cinema', cor: '#dc2626' },
       // ... mais categorias
   ]
   ```

2. **Extração de cores da imagem (linhas 330-410):**
   ```javascript
   async function getDualAverageColors(imageUrl) { ... }
   ```

3. **Cálculo de blend de cores (linhas 420-435):**
   ```javascript
   function blendHex(hexA, hexB, weight) { ... }
   ```

4. **Atualização do fundo (linhas 571-600):**
   ```javascript
   function atualizarCorFundo(corHex, categoryBase, headerOverride, paletteOverride) {
       document.documentElement.style.setProperty('--cor-pagina-primary', corHex[0]);
       document.documentElement.style.setProperty('--cor-pagina-secondary', corHex[1]);
       // ... mais atualizações
   }
   ```

5. **Atualização da paleta de CTAs (linhas 500-550):**
   ```javascript
   function atualizarPaletaCTA(primaryHex, secondaryHex, categoryBase, paletteOverride) {
       document.documentElement.style.setProperty('--hero-accent', accentA);
       document.documentElement.style.setProperty('--hero-accent-alt', accentB);
       // ... mais atualizações
   }
   ```

6. **Renderização do hero (linhas 630-750):**
   ```javascript
   function carregarHero() { ... }
   ```

### 4.3 Arquivo: `theme-toggle.js`
Define o tema claro/escuro:
```javascript
function applyTheme(theme) {
    document.body.classList.toggle('light-mode', isLight);
    // Muda entre logo.png e logo-modified.png
}
```

---

## 5. FLUXO DE ATUALIZAÇÃO DE CORES

### Quando o usuário navega para uma nova categoria:

```
1. Usuário clica em um dot ou seta do hero
   ↓
2. função mudarHero(index) é chamada
   ↓
3. Hero item anterior fica opaco (opacity: 0)
   Novo hero item fica ativo (opacity: 1)
   ↓
4. Cores da imagem são extraídas
   getDualAverageColors(novaImagem)
   ↓
5. Cores são blendadas com a cor da categoria
   blendHex(corImagem, corCategoria, peso)
   ↓
6. Validações são aplicadas (isBrownish, relativeLuminance)
   ↓
7. atualizarCorFundo() atualiza as CSS variables:
   - --cor-pagina-primary
   - --cor-pagina-secondary
   - --hero-accent
   - --hero-accent-alt
   - --hero-cta-text
   ↓
8. atualizarLogoPorSlide() muda logo e pill
   ↓
9. CSS do body, navbar e botões refletem as mudanças
   (transition: 450ms ease)
```

---

## 6. RESUMO VISUAL

### Cores Padrão
| Elemento | Cor | Uso |
|----------|-----|-----|
| Fundo Primário | `#000000` | Background principal (Preto) |
| Fundo Secundário | `#111111` | Gradiente de fundo (Preto escuro) |
| Destaque (CTA) | `#ff9b43` | Botões, badges |
| Destaque Alt | `#ff6f2c` | Gradiente de botões |
| Texto CTA | `#ffffff` | Texto de botões |

### Cores Dinâmicas (por categoria)
| Categoria | Cor Base | Logo | Exemplo de Uso |
|-----------|----------|------|-----------------|
| Musicais | `#6b4c9f` | logomusica.png | Roxo |
| Cinema | `#dc2626` | logocinema.png | Vermelho |
| Diversos | `#1e40af` | logo.png | Azul |
| Copa | `#166534` | logocopa.png | Verde |
| Viagens | `#0891b2` | logoviajjens.png | Ciano |

### Elementos com Cores Dinâmicas
- ✅ Fundo da página (hero section)
- ✅ Navbar (header)
- ✅ Logo (muda por categoria)
- ✅ Pill de categoria (ao lado do logo)
- ✅ Botões CTA
- ✅ Animações de transição

### Elementos com Cores Estáticas
- ❌ Grid de eventos (sempre laranja padrão)
- ❌ Texto geral (sempre branco)
- ❌ Borders (sempre rgba com branco)

---

## 7. TECNOLOGIAS UTILIZADAS

### Análise de Cores de Imagem
- **Canvas API** - Desenha imagem em canvas 32x32
- **getImageData()** - Extrai pixels RGB
- **Algoritmo de média** - Calcula cores dominantes

### Manipulação de Cores
- **Hex ↔ RGB** - Conversão de formatos
- **Hex ↔ HSL** - Para validação de contraste
- **Luminância relativa** - WCAG 2.0 para readability
- **Blend (mistura)** - Combinação ponderada de cores

### CSS Features
- **CSS Custom Properties** - Variáveis dinâmicas
- **color-mix()** - Mistura de cores via CSS
- **Gradientes lineares** - Backgrounds compostos
- **Transitions** - Animações suaves (450ms)
- **Backdrop-filter** - Efeito glassmorphism
