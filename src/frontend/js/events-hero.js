// Dados dos eventos (carregados da API)
let EVENTS_DATA = [];

// Base URL do backend (API + arquivos estaticos)
const API_BASE_URL = (() => {
    const configuredUrl = localStorage.getItem('API_URL');
    if (configuredUrl) {
        return configuredUrl.replace(/\/+$/, '');
    }

    if (window.location.port === '3000') {
        return 'http://localhost:5129';
    }

    const origin = window.location.origin;
    return (origin && origin !== 'null') ? origin : 'http://localhost:5129';
})();
const API_URL = `${API_BASE_URL}/api/eventos/publico`;

function toAbsoluteImageUrl(path) {
    if (!path) return path;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
}

// Array de imagens disponíveis para fallback circular
const IMAGENS_DISPONIVEIS = [
    `${API_BASE_URL}/imagens/luan.jpg`,
    `${API_BASE_URL}/imagens/matueebrandao.jpg`,
    `${API_BASE_URL}/imagens/leo.jpg`,
    `${API_BASE_URL}/imagens/bienal.jpg`,
    `${API_BASE_URL}/imagens/futurecom.png`,
    `${API_BASE_URL}/imagens/ccxp.jpg`,
    `${API_BASE_URL}/imagens/kanye.jpg`,
    `${API_BASE_URL}/imagens/SWING.png`
];

// Mapear imagens dos eventos por nome (opcional)
const IMAGENS_EVENTOS = {
    'Luan Santana - Para Sempre': `${API_BASE_URL}/imagens/luan.jpg`,
    'Matue & Brandão - Encontro de Gerações': `${API_BASE_URL}/imagens/matueebrandao.jpg`,
    'Leo Santana - Festa do Povo': `${API_BASE_URL}/imagens/leo.jpg`,
    'Bienal Internacional do Livro - São Paulo 2026': `${API_BASE_URL}/imagens/bienal.jpg`,
    'Futurecom 2026 - Festival de Tecnologia e Inovação': `${API_BASE_URL}/imagens/futurecom.png`,
    'CCXP 2026 - Comic Con Experience': `${API_BASE_URL}/imagens/ccxp.jpg`,
    'Kanye West - O Retorno': `${API_BASE_URL}/imagens/kanye.jpg`,
    'Swing Gala Festival 2026': `${API_BASE_URL}/imagens/SWING.png`
};

// Função para buscar eventos da API
async function carregarEventosDaAPI() {
    try {
        console.log('🔄 Buscando eventos de:', API_URL);
        const response = await fetch(API_URL);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const eventosAPI = await response.json();
        console.log('✅ Resposta bruta da API:', eventosAPI);

        if (!Array.isArray(eventosAPI)) {
            throw new Error('Resposta da API em formato invalido');
        }

        const semEventos = eventosAPI.length === 0;
        if (eventosAPI.length === 0) {
            console.warn('⚠️ Nenhum evento disponível');
        }

        // Slide inicial
        const slideInicial = {
            id: 0,
            nome: semEventos ? 'TicketPrime - Sem eventos no momento' : 'TicketPrime - Bem-vindo',
            artista: 'Bem-vindo',
            data: '2026-04-01',
            local: 'Plataforma Online',
            descricao: semEventos
                ? 'Ainda nao ha eventos publicados. Volte em breve ou confira com o administrador.'
                : 'Descubra os melhores eventos e artistas em nossa plataforma. Compre seus ingressos com seguranca e conforto.',
            preco: 0.00,
            imagem: `${API_BASE_URL}/imagens/fundo.jpg`,
            tipo: 'slide-inicial'
        };

        // Mapear dados do banco para formato do frontend
        const eventosFormatados = eventosAPI
            .filter(evento => evento) // Remove valores nulos
            .map((evento, index) => {
                console.log(`Processando evento ${index}:`, evento);
                
                const id = evento.id ?? evento.Id ?? index;
                const nome = evento.nome ?? evento.Nome ?? 'Sem nome';
                const dataEvento = evento.dataEvento ?? evento.DataEvento ?? evento.data ?? new Date().toISOString();
                const capacidadeTotal = evento.capacidadeTotal ?? evento.CapacidadeTotal ?? 0;
                const precoPadrao = evento.precoPadrao ?? evento.PrecoPadrao ?? 0;
                const local = evento.local ?? evento.Local ?? evento.endereco ?? evento.Endereco ?? 'Local a confirmar';
                
                // Usar imagem do banco de dados, com fallback
                let imagem = evento.imagemUrl ?? evento.ImagemUrl;
                if (!imagem) {
                    // Fallback: tentar mapear por nome ou usar fallback circular
                    imagem = IMAGENS_EVENTOS[nome] || IMAGENS_DISPONIVEIS[index % IMAGENS_DISPONIVEIS.length];
                }
                imagem = toAbsoluteImageUrl(imagem);

                // Garantir que dataEvento é uma string
                const dataStr = typeof dataEvento === 'string' ? dataEvento : dataEvento.toString();
                const data = dataStr.includes('T') ? dataStr.split('T')[0] : dataStr.split(' ')[0];

                return {
                    id: id,
                    nome: nome,
                    artista: nome.split('-')[0]?.trim() || 'Artista',
                    data: data,
                    local: local,
                    descricao: `${nome}`,
                    preco: parseFloat(precoPadrao),
                    imagem: imagem,
                    tipo: 'evento',
                    capacidadeTotal: capacidadeTotal,
                    fullImage: nome.includes('Swing') // Flag para SWING
                };
            });

        console.log('✨ Eventos formatados:', eventosFormatados);
        EVENTS_DATA = [slideInicial, ...eventosFormatados];
        return true;
    } catch (error) {
        console.error('❌ Erro ao carregar eventos:', error);
        console.error('Stack:', error.stack);
        // Fallback: usar dados fictícios se falhar
        console.warn('⚠️ Carregando dados fictícios como fallback...');
        carregarEventosFictícios();
        return false;
    }
}

// Função fallback com dados fictícios
function carregarEventosFictícios() {
    EVENTS_DATA = [
        {
            id: 0,
            nome: 'TicketPrime - Bem-vindo',
            artista: 'Bem-vindo',
            data: '2026-04-01',
            local: 'Plataforma Online',
            descricao: 'Descubra os melhores eventos e artistas em nossa plataforma. Compre seus ingressos com segurança e conforto.',
            preco: 0.00,
            imagem: `${API_BASE_URL}/imagens/fundo.jpg`,
            tipo: 'slide-inicial'
        },
        {
            id: 1,
            nome: 'Luan Santana - Para Sempre',
            artista: 'Luan Santana',
            data: '2026-04-15',
            local: 'Allianz Parque, São Paulo - SP',
            descricao: 'Luan Santana traz sua maior turnê com sucessos de sua carreira e lançamentos exclusivos.',
            preco: 150.00,
            imagem: `${API_BASE_URL}/imagens/luan.jpg`,
            tipo: 'evento'
        },
        {
            id: 2,
            nome: 'Matue & Brandão - Encontro de Gerações',
            artista: 'Matue & Brandão',
            data: '2026-05-10',
            local: 'Ginásio do Morumbi, São Paulo - SP',
            descricao: 'Um encontro épico entre gerações da música brasileira, com dj sets incríveis e surpresas.',
            preco: 180.00,
            imagem: `${API_BASE_URL}/imagens/matueebrandao.jpg`,
            tipo: 'evento'
        },
        {
            id: 3,
            nome: 'Leo Santana - Festa do Povo',
            artista: 'Leo Santana',
            data: '2026-07-12',
            local: 'Campo Grande, Brasília - DF',
            descricao: 'Festival ao ar livre com Leo Santana e artistas convidados em grande produção.',
            preco: 80.00,
            imagem: `${API_BASE_URL}/imagens/leo.jpg`,
            tipo: 'evento'
        },
        {
            id: 4,
            nome: 'Bienal Internacional do Livro - São Paulo 2026',
            artista: 'Diversos Autores',
            data: '2026-09-02',
            local: 'Expo Center Norte, São Paulo - SP',
            descricao: 'A maior celebração da literatura brasileira e internacional. Autores renomados, palestras, lançamentos de obras e experiências imersivas.',
            preco: 45.00,
            imagem: `${API_BASE_URL}/imagens/bienal.jpg`,
            tipo: 'evento'
        },
        {
            id: 5,
            nome: 'Futurecom 2026 - Festival de Tecnologia e Inovação',
            artista: 'Tech Leaders',
            data: '2026-09-15',
            local: 'Riocentro, Rio de Janeiro - RJ',
            descricao: 'Imersão no futuro com palestras sobre IA, blockchain, realidade virtual e as maiores tendências tecnológicas do ano.',
            preco: 120.00,
            imagem: `${API_BASE_URL}/imagens/futurecom.png`,
            tipo: 'evento'
        },
        {
            id: 6,
            nome: 'CCXP 2026 - Comic Con Experience',
            artista: 'Criadores de Conteúdo',
            data: '2026-10-05',
            local: 'São Paulo Expo, São Paulo - SP',
            descricao: 'O maior festival de cultura pop, quadrinhos, filmes e séries. Encontro com criadores, cosplayers e fãs apaixonados.',
            preco: 95.00,
            imagem: `${API_BASE_URL}/imagens/ccxp.jpg`,
            tipo: 'evento'
        },
        {
            id: 7,
            nome: 'Kanye West - O Retorno',
            artista: 'Kanye West',
            data: '2026-10-28',
            local: 'Estádio do Morumbi, São Paulo - SP',
            descricao: 'Show épico do produtor e artista que revolucionou a música. Hits clássicos, novos lançamentos e colaborações especiais.',
            preco: 350.00,
            imagem: `${API_BASE_URL}/imagens/kanye.jpg`,
            tipo: 'evento'
        },
        {
            id: 8,
            nome: 'Swing Gala Festival 2026',
            artista: 'Jazz Legends',
            data: '2026-11-12',
            local: 'Teatro Municipal, Rio de Janeiro - RJ',
            descricao: 'Noite memorável de swing para casais é solteiros liberais. Dança, música ao vivo e muita elegância.',
            preco: 180.00,
            imagem: `${API_BASE_URL}/imagens/SWING.png`,
            tipo: 'evento',
            fullImage: true
        }
    ];
}

// Função para carregar hero dinâmico
function carregarHero() {
    const heroContainer = document.getElementById('hero-container');
    if (!heroContainer) {
        console.error('❌ Elemento hero-container não encontrado!');
        return;
    }

    console.log(`🎨 Renderizando ${EVENTS_DATA.length} itens no hero...`);
    heroContainer.innerHTML = '';

    EVENTS_DATA.forEach((evento, index) => {
        const heroItem = document.createElement('div');
        heroItem.className = 'hero-item';
        if (index === 0) heroItem.classList.add('active');
        
        // Definir background image
        if (evento.imagem) {
            heroItem.style.backgroundImage = `url('${evento.imagem}')`;
            console.log(`✅ Evento ${index}: ${evento.nome} - Imagem: ${evento.imagem}`);
        } else {
            console.warn(`⚠️ Evento ${index}: ${evento.nome} - Sem imagem!`);
        }
        
        // Se for slide inicial ou tem imagem de fundo, usa a imagem como background
        if (evento.tipo === 'slide-inicial') {
            heroItem.classList.add('hero-item-inicial');
        } else {
            if (evento.fullImage) {
                heroItem.classList.add('hero-item-full');
            }
        }
        
        heroItem.setAttribute('data-index', index);

        heroItem.innerHTML = `
            <div class="hero-content">
                ${evento.tipo !== 'slide-inicial' ? `<div class="hero-artist">${evento.artista}</div>` : ''}
                <h1>${evento.nome}</h1>
                ${evento.tipo !== 'slide-inicial' ? `
                    <div class="hero-date">
                        <i class="bx bx-calendar"></i>
                        ${formatarData(evento.data)}
                    </div>
                    <div class="hero-location">
                        <i class="bx bx-map"></i>
                        ${evento.local}
                    </div>
                ` : ''}
                <p class="hero-description">${evento.descricao}</p>
                ${evento.tipo !== 'slide-inicial' ? `
                    <div class="hero-actions">
                        <button class="hero-btn primary">
                            Comprar Ingressos - <span class="hero-price">R$ ${evento.preco.toFixed(2)}</span>
                        </button>
                        <button class="hero-btn secondary">Mais Informações</button>
                    </div>
                ` : `
                    <div class="hero-actions">
                        <button class="hero-btn primary">Explorar Eventos</button>
                        <button class="hero-btn secondary">Sobre Nós</button>
                    </div>
                `}
            </div>
        `;

        heroContainer.appendChild(heroItem);
    });

    // Carregar dots
    carregarDots();
    // Inicializar controles do hero
    iniciarHeroControles();
    
    // Adicionar eventos aos botões do hero para cada item
    EVENTS_DATA.forEach((evento, index) => {
        const heroItem = heroContainer.querySelector(`.hero-item[data-index="${index}"]`);
        if (!heroItem) return;
        
        const botaoPrimario = heroItem.querySelector('.hero-btn.primary');
        const botaoSecundario = heroItem.querySelector('.hero-btn.secondary');
        
        if (botaoPrimario) {
            if (evento.tipo === 'slide-inicial') {
                // Botão "Explorar Eventos" do slide inicial
                if (botaoPrimario.textContent.includes('Explorar Eventos')) {
                    botaoPrimario.addEventListener('click', () => {
                        const eventsSection = document.querySelector('.events-grid-section');
                        if (eventsSection) {
                            eventsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        }
                    });
                }
            } else {
                // Botão "Comprar Ingressos" dos eventos
                if (botaoPrimario.textContent.includes('Comprar Ingressos')) {
                    botaoPrimario.addEventListener('click', () => {
                        window.location.href = `./paginas/painel-ingressos.html?id=${evento.id}`;
                    });
                }
            }
        }
    });
}

// Função para carregar dots (indicadores)
function carregarDots() {
    const dotsContainer = document.getElementById('hero-dots');
    if (!dotsContainer) return;

    dotsContainer.innerHTML = '';

    EVENTS_DATA.forEach((_, index) => {
        const dot = document.createElement('button');
        dot.type = 'button';
        dot.className = 'hero-dot';
        if (index === 0) dot.classList.add('active');
        dot.setAttribute('aria-label', `Ir para evento ${index + 1}`);
        dot.addEventListener('click', () => {
            mudarHero(index);
        });
        dotsContainer.appendChild(dot);
    });
}

// Função para formatar data
function formatarData(dataStr) {
    const data = new Date(dataStr + 'T00:00:00');
    return data.toLocaleDateString('pt-BR', { 
        weekday: 'long', 
        day: '2-digit', 
        month: 'long', 
        year: 'numeric' 
    });
}

// Variável global para controlar o autoplay
let autoPlayInterval = null;
let heroCurrentIndex = 0;

// Função para reiniciar o autoplay
function reiniciarAutoPlay() {
    if (autoPlayInterval) clearInterval(autoPlayInterval);
    startAutoPlay();
}

// Função para mudar o hero
function mudarHero(index) {
    heroCurrentIndex = index;
    const heroItems = document.querySelectorAll('.hero-item');
    const heroGots = document.querySelectorAll('.hero-dot');

    heroItems.forEach(item => item.classList.remove('active'));
    heroGots.forEach(dot => dot.classList.remove('active'));

    if (heroItems[index]) heroItems[index].classList.add('active');
    if (heroGots[index]) heroGots[index].classList.add('active');
}

// Função para iniciar o autoplay
function startAutoPlay() {
    const totalEventos = EVENTS_DATA.length;

    autoPlayInterval = setInterval(() => {
        heroCurrentIndex = (heroCurrentIndex + 1) % totalEventos;
        mudarHero(heroCurrentIndex);
    }, 8000);
}

// Função para iniciar controles do hero
function iniciarHeroControles() {
    const prevBtn = document.querySelector('.hero-prev');
    const nextBtn = document.querySelector('.hero-next');
    const totalEventos = EVENTS_DATA.length;

    if (prevBtn) {
        prevBtn.addEventListener('click', () => {
            heroCurrentIndex = (heroCurrentIndex - 1 + totalEventos) % totalEventos;
            mudarHero(heroCurrentIndex);
            reiniciarAutoPlay();
        });
    }

    if (nextBtn) {
        nextBtn.addEventListener('click', () => {
            heroCurrentIndex = (heroCurrentIndex + 1) % totalEventos;
            mudarHero(heroCurrentIndex);
            reiniciarAutoPlay();
        });
    }

    // Adicionar listener aos dots para reiniciar autoplay ao clicar
    const dots = document.querySelectorAll('.hero-dot');
    dots.forEach((dot, index) => {
        const originalClickHandler = dot.onclick;
        dot.addEventListener('click', () => {
            reiniciarAutoPlay();
        });
    });

    // Iniciar autoplay
    startAutoPlay();
}

// Função para carregar grid de eventos
function carregarGridEventos() {
    const eventsGrid = document.getElementById('events-grid');
    if (!eventsGrid) {
        console.error('❌ Elemento events-grid não encontrado!');
        return;
    }

    eventsGrid.innerHTML = '';

    // Filtra apenas os eventos (exclui o slide inicial)
    const eventos = EVENTS_DATA.filter(e => e.tipo !== 'slide-inicial');

    console.log(`📋 Renderizando grid com ${eventos.length} eventos...`);

    if (eventos.length === 0) {
        eventsGrid.innerHTML = '<p style="text-align: center; grid-column: 1/-1; color: #fff; padding: 40px;">Nenhum evento disponível no momento.</p>';
        return;
    }

    eventos.forEach((evento) => {
        const card = document.createElement('a');
        card.href = `./paginas/painel-ingressos.html?id=${evento.id}`;
        card.className = 'event-card';

        card.innerHTML = `
            <img src="${evento.imagem}" alt="${evento.nome}" class="event-card-image" onerror="this.style.backgroundColor='#333'" />
            <div class="event-card-content">
                <div class="event-card-artist">${evento.artista}</div>
                <h3 class="event-card-title">${evento.nome}</h3>
                <div class="event-card-meta">
                    <div class="event-card-meta-item">
                        <i class="bx bx-calendar"></i>
                        ${formatarData(evento.data)}
                    </div>
                    <div class="event-card-meta-item">
                        <i class="bx bx-map"></i>
                        ${evento.local}
                    </div>
                </div>
                <div class="event-card-footer">
                    <span class="event-card-price">R$ ${evento.preco.toFixed(2)}</span>
                    <button class="event-card-btn" onclick="event.preventDefault(); alert('Redirecionando para ingressos...');">Ingressos</button>
                </div>
            </div>
        `;

        eventsGrid.appendChild(card);
        console.log(`✅ Card criado: ${evento.nome}`);
    });
}

// Initialize quando DOM carregar
document.addEventListener('DOMContentLoaded', async () => {
    console.log('🚀 Iniciando carregamento de eventos...');
    
    // Carregar eventos da API
    const apiCarregouSucesso = await carregarEventosDaAPI();
    console.log('📊 Eventos carregados:', EVENTS_DATA.length, 'eventos');
    
    // Se não conseguiu carregar da API, carregar dados fictícios
    if (!apiCarregouSucesso) {
        console.warn('⚠️ Usando dados fictícios como fallback');
    }
    
    // Embaralhar eventos em ordem aleatória
    if (EVENTS_DATA.length > 1) {
        const eventosSlide = EVENTS_DATA[0]; // Preservar slide inicial
        const eventos = EVENTS_DATA.slice(1); // Pegar apenas os eventos

        for (let i = eventos.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [eventos[i], eventos[j]] = [eventos[j], eventos[i]];
        }

        EVENTS_DATA = [eventosSlide, ...eventos]; // Recolocar slide inicial no início
        console.log('✨ Eventos embaralhados');
    }
    
    console.log('🎬 Carregando Hero...');
    carregarHero();
    
    console.log('🎯 Carregando Grid de Eventos...');
    carregarGridEventos();
    
    // Adicionar evento ao logo/TicketPrime para voltar ao topo
    const navbarLogo = document.querySelector('.navbar-logo');
    if (navbarLogo) {
        navbarLogo.style.cursor = 'pointer';
        navbarLogo.addEventListener('click', () => {
            const heroSection = document.getElementById('hero-section');
            if (heroSection) {
                heroSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    }
    
    console.log('✅ Inicialização completa!');
});
