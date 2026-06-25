import { API_BASE_URL } from '../config/api.js';

const FRONTEND_BASE_URL = (() => {
    const scriptSrc = document.currentScript?.src;
    if (scriptSrc) {
        return scriptSrc.replace(/\/js\/events-hero\.js.*$/, '');
    }

    const currentPath = window.location.pathname.replace(/\/[^/]*$/, '');
    return `${window.location.origin}${currentPath.replace(/\/paginas$/, '')}`;
})();

const CATEGORY_DATA = [
    {
        slug: 'musicais',
        nome: 'Musicais',
        destaque: 'Shows e festivais ao vivo',
        titulo: 'Os melhores shows.<br><span class="hero-highlight-musica">As melhores emoções.</span>',
        descricao: 'Encontre os shows que você ama e viva momentos inesquecíveis.',
        ctaLabel: 'Garanta seu ingresso',
        imagem: './imagens/categorias/musicais/luan.jpg',
        icone: 'bx-music',
        cor: '#6b4c9f',
        heroStyle: 'musica-spotlight'
    },
    {
        slug: 'cinema',
        nome: 'Cinema',
        destaque: 'Pré-estreias e sessões especiais',
        descricao: 'Sessões exclusivas, maratonas e experiências cinematográficas imersivas.',
        imagem: './imagens/categorias/cinema/marvelvsdc.png',
        icone: 'bx-camera-movie',
        cor: '#dc2626',
        // override header color for this category (better contrast with this hero image)
        headerCor: '#0f1724'
    },
    {
        slug: 'eventos-diversos',
        nome: 'Eventos Diversos',
        destaque: 'Feiras, congressos e cultura pop',
        descricao: 'Eventos de negócios, tecnologia, literatura e cultura para todos os públicos.',
        imagem: './imagens/categorias/eventos-diversos/futurecom.png',
        icone: 'bx-calendar-event',
        cor: '#1e40af'
    },
    {
        slug: 'copa',
        nome: 'Copa 2026',
        destaque: 'Copa do Mundo 2026 já chegou no TicketPrime',
        descricao: 'A Copa do Mundo 2026 já chegou no TicketPrime: garanta sua experiência e viva cada partida com emoção total.',
        imagem: './imagens/categorias/copa/copa.webp',
        icone: 'bx-football',
        cor: '#166534'
    },
    {
        slug: 'viagens',
        nome: 'Viagens',
        destaque: 'Passagens e experiências de rota',
        titulo: 'O próximo destino pode mudar tudo.',
        descricao: 'Encontre passagens por preços imperdíveis e viaje com quem você confia.',
        ctaLabel: 'Acessar ofertas',
        imagem: './imagens/categorias/viagens/fundoviagens.png',
        icone: 'bx-map-alt',
        cor: '#0891b2',
        heroStyle: 'viagens'
    }
]

const CATEGORY_FALLBACK_IMAGE_POOLS = {
    musicais: [
        './imagens/categorias/musicais/luan.jpg',
        './imagens/categorias/musicais/matueebrandao.jpg',
        './imagens/categorias/musicais/leo.png',
        './imagens/categorias/musicais/kanye.jpg'
    ],
    cinema: [
        './imagens/categorias/cinema/marvelvsdc.png',
        './imagens/categorias/cinema/marvelvsdc2.png',
        './imagens/categorias/cinema/michaek.jpg',
        './imagens/categorias/cinema/tdmundoempanico.jpg',
        './imagens/categorias/cinema/trailer-de-street-fighter-2026.webp'
    ],
    'eventos-diversos': [
        './imagens/categorias/eventos-diversos/futurecom.png',
        './imagens/categorias/eventos-diversos/bienal.jpg',
        './imagens/categorias/eventos-diversos/ccxp.jpg',
        './imagens/categorias/eventos-diversos/SWING.png'
    ],
    copa: [
        './imagens/categorias/copa/copa.webp',
        './imagens/categorias/copa/argentina.jpg',
        './imagens/categorias/copa/br.avif',
        './imagens/categorias/copa/br.jpg',
        './imagens/categorias/copa/bra.png',
        './imagens/categorias/copa/pt.png',
        './imagens/categorias/copa/BR1.jpg'
    ],
    viagens: [
        './imagens/categorias/viagens/fundoviagens.png',
        './imagens/categorias/viagens/metro.png',
        './imagens/categorias/viagens/onibus.png',
        './imagens/categorias/viagens/trem.png',
        './imagens/categorias/viagens/van.png'
    ]
};

const HOME_FALLBACK_IMAGE_POOL = [
    './imagens/home/fundo.jpg',
    './imagens/home/019447ed-88bb-459f-b812-126260147dfb.png',
    './imagens/home/43a17584-2e96-4cd3-8281-a98b0c5efed1.png'
];

const HOME_SLIDE_POOL_KEY = 'slide-inicial-home';

const HERO_DATA = [
    {
        tipo: 'slide-inicial',
        destaque: '+50.000 experiências disponíveis',
        titulo: 'Tudo o que vale a pena viver, <span class="hero-accent">em um só lugar.</span>',
        descricao: 'Descubra, compare e reserve experiências incríveis em música, cinema, festivais, teatro e viagens.',
        ctaLabel: 'Explorar Experiências',
        imagem: './imagens/home/fundo.jpg',
        cor: '#000000',
        headerCor: '#000000',
        heroStyle: 'inicial-premium'
    },
    ...CATEGORY_DATA.map((categoria) => ({
        tipo: 'categoria',
        ...categoria
    }))
];

const LOGO_BY_CATEGORY = {
    default: './imagens/logo.png',
    musicais: './imagens/logomusica.png',
    cinema: './imagens/logocinema.png',
    'eventos-diversos': './imagens/logo.png',
    copa: './imagens/logocopa.png',
    viagens: './imagens/logoviajjens.png'
};

const NAV_PILL_BY_CATEGORY = {
    musicais: { label: 'Musicais', className: 'pill-musicais' },
    cinema: { label: 'Cinema', className: 'pill-cinema' },
    'eventos-diversos': { label: 'Diversos', className: 'pill-diversos' },
    copa: { label: 'Copa 2026', className: 'pill-copa' },
    viagens: { label: 'Viagens', className: 'pill-viagens' }
};

let heroCurrentIndex = 0;
let autoPlayInterval = null;
const categoryImagePools = new Map();
const categoryLastImage = new Map();
const categoryVisits = new Map();

const LEGACY_IMAGE_TO_CATEGORY_PATH = {
    'luan.jpg': './imagens/categorias/musicais/luan.jpg',
    'matueebrandao.jpg': './imagens/categorias/musicais/matueebrandao.jpg',
    'leo.png': './imagens/categorias/musicais/leo.png',
    'kanye.jpg': './imagens/categorias/musicais/kanye.jpg',
    'swing.png': './imagens/categorias/eventos-diversos/SWING.png',
    'ccxp.jpg': './imagens/categorias/eventos-diversos/ccxp.jpg',
    'bienal.jpg': './imagens/categorias/eventos-diversos/bienal.jpg',
    'futurecom.png': './imagens/categorias/eventos-diversos/futurecom.png',
    'copa.webp': './imagens/categorias/copa/copa.webp',
    'argentina.jpg': './imagens/categorias/copa/argentina.jpg',
    'br.avif': './imagens/categorias/copa/br.avif',
    'br.jpg': './imagens/categorias/copa/br.jpg',
    'bra.png': './imagens/categorias/copa/bra.png',
    'pt.png': './imagens/categorias/copa/pt.png',
    'br1.jpg': './imagens/categorias/copa/BR1.jpg',
    'br1.jpg.webp': './imagens/categorias/copa/BR1.jpg',
    'fundoviagens.png': './imagens/categorias/viagens/fundoviagens.png',
    'metro.png': './imagens/categorias/viagens/metro.png',
    'onibus.png': './imagens/categorias/viagens/onibus.png',
    'trem.png': './imagens/categorias/viagens/trem.png',
    'van.png': './imagens/categorias/viagens/van.png',
    'fundo.jpg': './imagens/home/fundo.jpg',
    '019447ed-88bb-459f-b812-126260147dfb.png': './imagens/home/019447ed-88bb-459f-b812-126260147dfb.png',
    '43a17584-2e96-4cd3-8281-a98b0c5efed1.png': './imagens/home/43a17584-2e96-4cd3-8281-a98b0c5efed1.png'
};

function mapLegacyImagePath(rawPath) {
    const fileName = String(rawPath ?? '').split('/').pop()?.toLowerCase();
    if (!fileName) return '';
    return LEGACY_IMAGE_TO_CATEGORY_PATH[fileName] || '';
}

function isCopaBrazilImagePath(imagePath) {
    const src = String(imagePath || '').toLowerCase();
    if (!src) return false;
    return src.includes('/copa/br.avif') || src.includes('/copa/bra.png') || src.endsWith('br.avif') || src.endsWith('bra.png');
}

function normalizeImageUrl(rawImage) {
    const raw = String(rawImage ?? '').trim();
    if (!raw) return '';
    if (/^(https?:|data:|blob:)/i.test(raw)) return raw;
    if (raw.startsWith('/imagens/')) {
        const mapped = mapLegacyImagePath(raw);
        if (mapped) return mapped;
        return `${API_BASE_URL}${raw}`;
    }
    return raw;
}

function extractEventImageUrl(evento) {
    return normalizeImageUrl(
        evento?.imagemurl
        ?? evento?.imagemUrl
        ?? evento?.ImagemUrl
        ?? evento?.imagem
        ?? evento?.Imagem
        ?? evento?.foto
        ?? evento?.Foto
        ?? ''
    );
}

function pickNextCategoryImage(slug, fallbackImage) {
    const pool = categoryImagePools.get(slug) || [];
    if (!pool.length) return fallbackImage;

    const lastImage = categoryLastImage.get(slug);
    let candidates = pool;
    if (pool.length > 1 && lastImage) {
        const withoutLast = pool.filter((img) => img !== lastImage);
        if (withoutLast.length) candidates = withoutLast;
    }

    const picked = candidates[Math.floor(Math.random() * candidates.length)] || fallbackImage;
    categoryLastImage.set(slug, picked);
    return picked;
}

async function carregarImagensCategoriaPool() {
    const loaders = CATEGORY_DATA.map(async (categoria) => {
        const imagensFallback = (CATEGORY_FALLBACK_IMAGE_POOLS[categoria.slug] || []).map(normalizeImageUrl).filter(Boolean);

        // For the home hero, keep category imagery curated and deterministic.
        // API images can vary by environment and may mix visual identities across categories.
        if (imagensFallback.length) {
            categoryImagePools.set(categoria.slug, [...new Set(imagensFallback)]);
            return;
        }

        try {
            const response = await fetch(`${API_BASE_URL}/api/eventos/publico?categoria=${encodeURIComponent(categoria.slug)}`);
            if (!response.ok) return;

            const eventos = await response.json();
            if (!Array.isArray(eventos)) return;

            const imagensApi = eventos.map(extractEventImageUrl).filter(Boolean);
            const imagens = [...new Set(imagensApi)];
            if (imagens.length) categoryImagePools.set(categoria.slug, imagens);
        } catch (error) {
            // Keep fallback image when API is unavailable for a category.
        }

        if (imagensFallback.length && !categoryImagePools.has(categoria.slug)) {
            categoryImagePools.set(categoria.slug, [...new Set(imagensFallback)]);
        }
    });

    await Promise.all(loaders);
}

function definirImagemInicialCategorias() {
    const homeSlide = HERO_DATA.find((item) => item.tipo === 'slide-inicial');
    if (homeSlide) {
        const homePool = HOME_FALLBACK_IMAGE_POOL.map(normalizeImageUrl).filter(Boolean);
        if (homePool.length) {
            categoryImagePools.set(HOME_SLIDE_POOL_KEY, [...new Set(homePool)]);
            homeSlide.imagemPadrao = homeSlide.imagemPadrao || homeSlide.imagem;
            homeSlide.imagem = pickNextCategoryImage(HOME_SLIDE_POOL_KEY, homeSlide.imagemPadrao);
        }
    }

    HERO_DATA.forEach((item) => {
        if (item.tipo !== 'categoria' || !item.slug) return;

        item.imagemPadrao = item.imagemPadrao || item.imagem;
        item.imagem = pickNextCategoryImage(item.slug, item.imagemPadrao);
    });
}

async function recomputeItemColors(item) {
    item.paletteOverride = null;

    try {
        const [c1, c2] = await getDualAverageColors(item.imagem);
        if (item.cor) {
            item.computedPrimary = blendHex(c1, item.cor, 0.42);
            item.computedSecondary = blendHex(c2, item.cor, 0.36);
        } else {
            item.computedPrimary = c1;
            item.computedSecondary = c2;
        }
    } catch (error) {
        item.computedPrimary = item.cor || '#000000';
        item.computedSecondary = item.cor || '#111111';
    }

    if (item?.slug === 'copa' && isCopaBrazilImagePath(item.imagem)) {
        item.computedPrimary = '#0f6d31';
        item.computedSecondary = '#0a4f25';
        item.paletteOverride = {
            accent: '#FFD60A',
            accentAlt: '#FBC02D',
            ctaText: '#1f2937',
            pillHighlight: '#FFD60A'
        };
    }
}

async function refreshCategorySlideImage(item, index) {
    if (!item) return;

    const poolKey = item.tipo === 'categoria'
        ? item.slug
        : (item.tipo === 'slide-inicial' ? HOME_SLIDE_POOL_KEY : null);

    if (!poolKey) return;

    const visitCount = categoryVisits.get(poolKey) || 0;
    categoryVisits.set(poolKey, visitCount + 1);
    if (visitCount === 0) return;

    const nextImage = pickNextCategoryImage(poolKey, item.imagemPadrao || item.imagem);
    if (!nextImage || nextImage === item.imagem) return;

    item.imagem = nextImage;
    const heroItem = document.querySelector(`.hero-item[data-index="${index}"]`);
    if (heroItem) {
        heroItem.style.backgroundImage = `url('${item.imagem}')`;
    }

    await recomputeItemColors(item);

    if (heroCurrentIndex === index) {
        atualizarCorFundo(
            [item.computedPrimary || item.cor, item.computedSecondary || item.cor],
            item.cor,
            item.headerCor,
            item.paletteOverride
        );
    }
}

async function getAverageColor(imageUrl) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => {
            try {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                const w = 32;
                const h = 32;
                canvas.width = w;
                canvas.height = h;
                ctx.drawImage(img, 0, 0, w, h);
                const data = ctx.getImageData(0, 0, w, h).data;
                let r=0,g=0,b=0,count=0;
                for (let i=0;i<data.length;i+=4){
                    const alpha = data[i+3];
                    if(alpha < 40) continue; // ignore very transparent pixels
                    r += data[i]; g += data[i+1]; b += data[i+2]; count++;
                }
                if(count===0) return resolve('#000000');
                r = Math.round(r/count); g = Math.round(g/count); b = Math.round(b/count);
                const toHex = (n) => n.toString(16).padStart(2,'0');
                resolve('#' + toHex(r) + toHex(g) + toHex(b));
            } catch(err){
                resolve('#000000');
            }
        };
        img.onerror = () => reject(new Error('Image load error'));
        img.src = imageUrl;
    });
}

async function getDualAverageColors(imageUrl) {
    return new Promise((resolve) => {
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => {
            try {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                const w = 48;
                const h = 48;
                canvas.width = w;
                canvas.height = h;
                ctx.drawImage(img, 0, 0, w, h);
                const data = ctx.getImageData(0, 0, w, h).data;
                let r1=0,g1=0,b1=0,c1=0; // top half
                let r2=0,g2=0,b2=0,c2=0; // bottom half
                for (let y=0;y<h;y++){
                    for (let x=0;x<w;x++){
                        const i = (y*w + x) * 4;
                        const a = data[i+3];
                        if(a < 40) continue;
                        const r = data[i], g = data[i+1], b = data[i+2];
                        if (y < h/2) { r1+=r; g1+=g; b1+=b; c1++; } else { r2+=r; g2+=g; b2+=b; c2++; }
                    }
                }
                if(c1===0) c1=1;
                if(c2===0) c2=1;
                const toHex=(n)=>n.toString(16).padStart(2,'0');
                const p1 = '#' + toHex(Math.round(r1/c1)) + toHex(Math.round(g1/c1)) + toHex(Math.round(b1/c1));
                const p2 = '#' + toHex(Math.round(r2/c2)) + toHex(Math.round(g2/c2)) + toHex(Math.round(b2/c2));
                resolve([p1,p2]);
            } catch (err) {
                resolve(['#000000','#111111']);
            }
        };
        img.onerror = () => resolve(['#000000','#111111']);
        img.src = imageUrl;
    });
}

function hexToRgb(hex) {
    hex = hex.replace('#','');
    if (hex.length === 3) hex = hex.split('').map(h=>h+h).join('');
    const bigint = parseInt(hex, 16);
    return { r: (bigint >> 16) & 255, g: (bigint >> 8) & 255, b: bigint & 255 };
}

function rgbToHex(r,g,b){
    const toHex = (n) => Math.max(0,Math.min(255,Math.round(n))).toString(16).padStart(2,'0');
    return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function blendHex(hexA, hexB, weight){
    // weight: 0..1 how much hexB contributes
    try {
        const a = hexToRgb(hexA || '#000000');
        const b = hexToRgb(hexB || '#000000');
        const r = a.r * (1-weight) + b.r * weight;
        const g = a.g * (1-weight) + b.g * weight;
        const bl = a.b * (1-weight) + b.b * weight;
        return rgbToHex(r,g,bl);
    } catch (e) {
        return hexA || hexB || '#000000';
    }
}

function hexToHsl(hex) {
    const {r,g,b} = hexToRgb(hex);
    const r1 = r/255, g1 = g/255, b1 = b/255;
    const max = Math.max(r1,g1,b1), min = Math.min(r1,g1,b1);
    let h=0,s=0,l=(max+min)/2;
    if (max !== min) {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch(max) {
            case r1: h = (g1 - b1) / d + (g1 < b1 ? 6 : 0); break;
            case g1: h = (b1 - r1) / d + 2; break;
            case b1: h = (r1 - g1) / d + 4; break;
        }
        h /= 6;
    }
    return { h: Math.round(h * 360), s: Math.round(s * 100), l: Math.round(l * 100) };
}

function isBrownish(hex) {
    try {
        const hsl = hexToHsl(hex);
        // brown-ish hues roughly between 10 and 45 degrees, moderate saturation and medium lightness
        if (hsl.h >= 10 && hsl.h <= 45 && hsl.s >= 10 && hsl.s <= 70 && hsl.l >= 10 && hsl.l <= 60) return true;
        return false;
    } catch (e) {
        return false;
    }
}

function relativeLuminance(hex) {
    try {
        const { r, g, b } = hexToRgb(hex || '#000000');
        const srgb = [r, g, b].map((v) => v / 255).map((v) => (
            v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4)
        ));
        return 0.2126 * srgb[0] + 0.7152 * srgb[1] + 0.0722 * srgb[2];
    } catch {
        return 0;
    }
}

function readableTextForBackground(hex) {
    return relativeLuminance(hex) > 0.52 ? '#0f172a' : '#f8fafc';
}

function complementaryHex(hex) {
    try {
        const { r, g, b } = hexToRgb(hex || '#000000');
        return rgbToHex(255 - r, 255 - g, 255 - b);
    } catch {
        return '#cbd5e1';
    }
}

function applyComplementaryPillStyle(pill, primaryHex, secondaryHex, categoryBase, forcedHighlightHex) {
    if (!pill) return;

    let base = blendHex(primaryHex || '#000000', secondaryHex || '#111111', 0.38);
    if (categoryBase) base = blendHex(base, categoryBase, 0.35);

    let comp = forcedHighlightHex || complementaryHex(base);
    let bg = blendHex(comp, '#ffffff', 0.58);
    let border = blendHex(comp, '#ffffff', 0.36);
    if (isBrownish(bg)) bg = blendHex(comp, '#cfe1ff', 0.5);

    const text = readableTextForBackground(bg);

    pill.style.background = bg;
    pill.style.borderColor = border;
    pill.style.color = text;
}

function atualizarPaletaCTA(primaryHex, secondaryHex, categoryBase, paletteOverride) {
    if (paletteOverride?.accent && paletteOverride?.accentAlt) {
        document.documentElement.style.setProperty('--hero-accent', paletteOverride.accent);
        document.documentElement.style.setProperty('--hero-accent-alt', paletteOverride.accentAlt);
        document.documentElement.style.setProperty('--hero-cta-text', paletteOverride.ctaText || '#f8fafc');
        return;
    }

    const primary = primaryHex || '#000000';
    const secondary = secondaryHex || '#111111';

    let accentA = blendHex(primary, secondary, 0.42);
    if (categoryBase) accentA = blendHex(accentA, categoryBase, 0.46);
    if (isBrownish(accentA)) accentA = categoryBase || blendHex(primary, '#ff8a3d', 0.5);

    const accentAHsl = hexToHsl(accentA);
    if (accentAHsl.s < 35 || accentAHsl.l < 22) {
        accentA = blendHex(accentA, '#ff8a3d', 0.68);
    }

    let accentB = blendHex(accentA, secondary, 0.33);
    if (isBrownish(accentB)) accentB = blendHex(accentA, '#334155', 0.35);

    const accentBHsl = hexToHsl(accentB);
    if (accentBHsl.s < 30 || accentBHsl.l < 20) {
        accentB = blendHex(accentA, '#ff5a22', 0.42);
    }

    const ctaText = readableTextForBackground(blendHex(accentA, accentB, 0.5));

    document.documentElement.style.setProperty('--hero-accent', accentA);
    document.documentElement.style.setProperty('--hero-accent-alt', accentB);
    document.documentElement.style.setProperty('--hero-cta-text', ctaText);
}

async function preloadAndComputeColors() {
    const items = HERO_DATA.slice();
    const promises = items.map(async (it) => {
        await recomputeItemColors(it);
    });
    await Promise.all(promises);
    // write computed colors back to HERO_DATA (preserve original order)
    for (let i=0;i<items.length;i++){
        HERO_DATA[i].computedPrimary = items[i].computedPrimary;
        HERO_DATA[i].computedSecondary = items[i].computedSecondary;
    }
}

function buildCategoryLink(categoriaSlug) {
    return `${FRONTEND_BASE_URL}/paginas/categoria.html?categoria=${encodeURIComponent(categoriaSlug)}`;
}

function atualizarCorFundo(corHex, categoryBase, headerOverride, paletteOverride) {
    // accept either single color or array [primary,secondary]
    if (Array.isArray(corHex)){
        atualizarPaletaCTA(corHex[0], corHex[1], categoryBase, paletteOverride);
        document.documentElement.style.setProperty('--cor-pagina-primary', corHex[0]);
        document.documentElement.style.setProperty('--cor-pagina-secondary', corHex[1]);
        document.body.style.background = `linear-gradient(180deg, ${corHex[0]} 0%, ${corHex[1]} 100%)`;
        const nav = document.querySelector('.site-navbar');
        if (nav) {
            // compute a safe header color: blend image primary with category base when available
            const headerBlendWeight = 0.36; // proportion of category base in the blend
            // if headerOverride is provided, prefer it (immediate per-slide override)
            let headerColor = headerOverride || corHex[0];
            if (!headerOverride && categoryBase) {
                headerColor = blendHex(corHex[0], categoryBase, headerBlendWeight);
            }
            // if result still looks brownish, fallback to a softened category base or a neutral dark
            if (isBrownish(headerColor)) {
                if (categoryBase) headerColor = blendHex(categoryBase, '#222222', 0.18);
                else headerColor = 'rgba(31,41,55,0.92)';
            }
            nav.style.background = headerColor;
        }
    } else {
        atualizarPaletaCTA(corHex, corHex, categoryBase, paletteOverride);
        document.documentElement.style.setProperty('--cor-pagina-primary', corHex);
        document.documentElement.style.setProperty('--cor-pagina-secondary', corHex);
        document.body.style.background = corHex;
        const nav2 = document.querySelector('.site-navbar');
        if (nav2) {
            let headerColor = headerOverride || corHex;
            if (!headerOverride && categoryBase) headerColor = blendHex(corHex, categoryBase, 0.36);
            if (isBrownish(headerColor)) headerColor = (categoryBase ? blendHex(categoryBase, '#222222', 0.18) : 'rgba(31,41,55,0.92)');
            nav2.style.background = headerColor;
        }
    }
}

function carregarHero() {
    const heroContainer = document.getElementById('hero-container');
    if (!heroContainer) return;

    heroContainer.innerHTML = '';

    HERO_DATA.forEach((item, index) => {
        const heroItem = document.createElement('div');
        heroItem.className = 'hero-item';
        if (index === 0) heroItem.classList.add('active', 'hero-item-inicial');

        heroItem.setAttribute('data-index', String(index));
        heroItem.style.backgroundImage = `url('${item.imagem}')`;

        if (item.tipo === 'slide-inicial') {
            if (item.heroStyle === 'inicial-premium') {
                heroItem.innerHTML = `
                    <div class="hero-content hero-content--home-premium">
                        <div class="hero-badge-premium"><i class='bx bx-ticket'></i>${item.destaque || ''}</div>
                        <h1 class="hero-title-premium">${item.titulo}</h1>
                        <p class="hero-description">${item.descricao}</p>
                        <div class="hero-actions">
                            <button class="hero-btn hero-btn--home-premium" data-action="explorar">${item.ctaLabel || 'Explorar Experiências'} &rarr;</button>
                        </div>
                    </div>
                `;
            } else {
                heroItem.innerHTML = `
                    <div class="hero-content">
                        <h1>${item.titulo}</h1>
                        <p class="hero-description">${item.descricao}</p>
                        <div class="hero-actions">
                            <button class="hero-btn primary" data-action="explorar">Explorar Categorias</button>
                            <a class="hero-btn secondary" href="${FRONTEND_BASE_URL}/paginas/login.html">Entrar</a>
                        </div>
                    </div>
                `;
            }
        } else {
            const linkCategoria = buildCategoryLink(item.slug);

            if (item.heroStyle === 'musica-spotlight') {
                heroItem.innerHTML = `
                    <div class="hero-content hero-content--musica-spotlight">
                        <h1 class="hero-title-musica">${item.titulo || item.nome}</h1>
                        <p class="hero-description">${item.descricao}</p>
                        <div class="hero-actions">
                            <a class="hero-btn hero-btn--musica" href="${linkCategoria}">
                                <i class='bx bx-ticket'></i>${item.ctaLabel || 'Garanta seu ingresso'}
                                <i class='bx bx-right-arrow-alt'></i>
                            </a>
                        </div>
                    </div>
                `;
            } else if (item.heroStyle === 'viagens') {
                heroItem.innerHTML = `
                    <div class="hero-content hero-content--viagens">
                        <h1 class="hero-title-viagens">${item.titulo || item.nome}</h1>
                        <p class="hero-description">${item.descricao}</p>
                        <div class="hero-actions">
                            <a class="hero-btn hero-btn--viagens" href="${linkCategoria}">${item.ctaLabel || 'Acessar ofertas'} &rarr;</a>
                        </div>
                    </div>
                `;
            } else {
                heroItem.innerHTML = `
                    <div class="hero-content">
                        <div class="hero-artist">${item.destaque}</div>
                        <h1>${item.nome}</h1>
                        <p class="hero-description">${item.descricao}</p>
                        <div class="hero-actions">
                            <a class="hero-btn primary" href="${linkCategoria}">Abrir Categoria</a>
                            <a class="hero-btn secondary" href="${linkCategoria}">Ver Eventos</a>
                        </div>
                    </div>
                `;
            }
        }

        heroContainer.appendChild(heroItem);
    });

    carregarDots();
    iniciarHeroControles();
    const first = HERO_DATA[0];
    atualizarCorFundo([first.computedPrimary || first.cor, first.computedSecondary || first.cor], first.cor, first.headerCor, first.paletteOverride);
    atualizarLogoPorSlide(first);

    const explorarBtn = heroContainer.querySelector('[data-action="explorar"]');
    explorarBtn?.addEventListener('click', () => {
        document.querySelector('.events-grid-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
}

function atualizarLogoPorSlide(itemAtivo) {
    const logoImg = document.querySelector('.navbar-logo .logo-img');
    if (!logoImg) return;

    const categoria = itemAtivo?.slug;
    const logoSrc = LOGO_BY_CATEGORY[categoria] || LOGO_BY_CATEGORY.default;
    logoImg.src = logoSrc;
    logoImg.alt = categoria ? `Logo TicketPrime ${categoria}` : 'Logo TicketPrime';

    const pill = document.querySelector('.navbar-logo .navbar-category-pill');
    if (pill) {
        const pillMeta = categoria ? NAV_PILL_BY_CATEGORY[categoria] : null;
        if (!pillMeta) {
            pill.textContent = '';
            pill.className = 'navbar-category-pill is-hidden';
            pill.setAttribute('aria-hidden', 'true');
            pill.removeAttribute('style');
        } else {
            pill.textContent = pillMeta.label;
            pill.className = `navbar-category-pill ${pillMeta.className}`;
            pill.setAttribute('aria-hidden', 'false');
            const primary = itemAtivo?.computedPrimary || itemAtivo?.cor || getComputedStyle(document.documentElement).getPropertyValue('--cor-pagina-primary').trim();
            const secondary = itemAtivo?.computedSecondary || itemAtivo?.cor || getComputedStyle(document.documentElement).getPropertyValue('--cor-pagina-secondary').trim();
            applyComplementaryPillStyle(pill, primary, secondary, itemAtivo?.cor, itemAtivo?.paletteOverride?.pillHighlight);
        }
    }
}

function carregarDots() {
    const dotsContainer = document.getElementById('hero-dots');
    if (!dotsContainer) return;

    dotsContainer.innerHTML = '';

    HERO_DATA.forEach((_, index) => {
        const dot = document.createElement('button');
        dot.type = 'button';
        dot.className = 'hero-dot';
        if (index === 0) dot.classList.add('active');
        dot.setAttribute('aria-label', `Ir para categoria ${index + 1}`);
        dot.addEventListener('click', () => {
            mudarHero(index);
            reiniciarAutoPlay();
        });
        dotsContainer.appendChild(dot);
    });
}

function mudarHero(index) {
    heroCurrentIndex = index;
    const heroItems = document.querySelectorAll('.hero-item');
    const heroDots = document.querySelectorAll('.hero-dot');

    heroItems.forEach((item) => item.classList.remove('active'));
    heroDots.forEach((dot) => dot.classList.remove('active'));

    heroItems[index]?.classList.add('active');
    heroDots[index]?.classList.add('active');

    const itemAtivo = HERO_DATA[index];
    const newPrimary = itemAtivo?.computedPrimary || itemAtivo?.cor;
    const newSecondary = itemAtivo?.computedSecondary || itemAtivo?.cor;
    if (newPrimary) atualizarCorFundo([newPrimary, newSecondary], itemAtivo?.cor, itemAtivo?.headerCor, itemAtivo?.paletteOverride);
    atualizarLogoPorSlide(itemAtivo);

    refreshCategorySlideImage(itemAtivo, index);
}

function startAutoPlay() {
    if (autoPlayInterval) clearInterval(autoPlayInterval);

    autoPlayInterval = setInterval(() => {
        heroCurrentIndex = (heroCurrentIndex + 1) % HERO_DATA.length;
        mudarHero(heroCurrentIndex);
    }, 8000);
}

function reiniciarAutoPlay() {
    startAutoPlay();
}

function iniciarHeroControles() {
    const prevBtn = document.querySelector('.hero-prev');
    const nextBtn = document.querySelector('.hero-next');

    prevBtn?.addEventListener('click', () => {
        heroCurrentIndex = (heroCurrentIndex - 1 + HERO_DATA.length) % HERO_DATA.length;
        mudarHero(heroCurrentIndex);
        reiniciarAutoPlay();
    });

    nextBtn?.addEventListener('click', () => {
        heroCurrentIndex = (heroCurrentIndex + 1) % HERO_DATA.length;
        mudarHero(heroCurrentIndex);
        reiniciarAutoPlay();
    });

    startAutoPlay();
}

function carregarGridCategorias() {
    const eventsGrid = document.getElementById('events-grid');
    if (!eventsGrid) return;

    eventsGrid.innerHTML = '';

    CATEGORY_DATA.forEach((categoria) => {
        const linkCategoria = buildCategoryLink(categoria.slug);
        const card = document.createElement('a');
        card.href = linkCategoria;
        card.className = 'event-card';

        card.innerHTML = `
            <img src="${categoria.imagem}" alt="${categoria.nome}" class="event-card-image" />
            <div class="event-card-content">
                <div class="event-card-artist">Categoria</div>
                <h3 class="event-card-title">${categoria.nome}</h3>
                <div class="event-card-meta">
                    <div class="event-card-meta-item">
                        <i class="bx ${categoria.icone}"></i>
                        ${categoria.destaque}
                    </div>
                    <div class="event-card-meta-item">
                        <i class="bx bx-right-arrow-alt"></i>
                        Acessar página da categoria
                    </div>
                </div>
                <div class="event-card-footer">
                    <span class="event-card-price">Explorar</span>
                    <button class="event-card-btn" type="button">Entrar</button>
                </div>
            </div>
        `;

        card.querySelector('.event-card-btn')?.addEventListener('click', (event) => {
            event.preventDefault();
            window.location.href = linkCategoria;
        });

        eventsGrid.appendChild(card);
    });
}

function inicializarPagina() {
    (async () => {
        try {
            await carregarImagensCategoriaPool();
            definirImagemInicialCategorias();
            await preloadAndComputeColors();
        } catch (e) {
            // ignore
        }
        carregarHero();
        carregarGridCategorias();
    })();

    const navbarLogo = document.querySelector('.navbar-logo');
    if (navbarLogo) {
        navbarLogo.style.cursor = 'pointer';
        navbarLogo.addEventListener('click', () => {
            document.getElementById('hero-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => inicializarPagina(), { once: true });
} else {
    inicializarPagina();
}
