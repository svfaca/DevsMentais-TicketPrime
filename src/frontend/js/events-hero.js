// Dados fictícios dos eventos
const EVENTS_DATA = [
    {
        id: 0,
        nome: 'TicketPrime - Bem-vindo',
        artista: 'Bem-vindo',
        data: '2026-04-01',
        local: 'Plataforma Online',
        descricao: 'Descubra os melhores eventos e artistas em nossa plataforma. Compre seus ingressos com segurança e conforto.',
        preco: 0.00,
        imagem: './imagens/fundo.jpg',
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
        imagem: './imagens/luan.jpg',
        tipo: 'evento'
    },
    {
        id: 2,
        nome: 'Leo Santana - Energia Total',
        artista: 'Leo Santana',
        data: '2026-04-22',
        local: 'Estádio da Juventude, Salvador - BA',
        descricao: 'Leo Santana apresenta um show energético com participações especiais e muita animação.',
        preco: 120.00,
        imagem: './imagens/leo.jpg',
        tipo: 'evento'
    },
    {
        id: 3,
        nome: 'Matue & Brandão - Encontro de Gerações',
        artista: 'Matue & Brandão',
        data: '2026-05-10',
        local: 'Ginásio do Morumbi, São Paulo - SP',
        descricao: 'Um encontro épico entre gerações da música brasileira, com dj sets incríveis e surpresas.',
        preco: 180.00,
        imagem: './imagens/matueebrandao.jpg',
        tipo: 'evento'
    },
    {
        id: 4,
        nome: 'Luan Santana - Acústico',
        artista: 'Luan Santana',
        data: '2026-06-05',
        local: 'Teatro Bradesco, Rio de Janeiro - RJ',
        descricao: 'Apresentação intimista do artista em ambiente acústico com suas músicas mais emocionantes.',
        preco: 200.00,
        imagem: './imagens/luan.jpg',
        tipo: 'evento'
    },
    {
        id: 5,
        nome: 'Leo Santana - Festa do Povo',
        artista: 'Leo Santana',
        data: '2026-07-12',
        local: 'Campo Grande, Brasília - DF',
        descricao: 'Festival ao ar livre com Leo Santana e artistas convidados em grande produção.',
        preco: 80.00,
        imagem: './imagens/leo.jpg',
        tipo: 'evento'
    },
    {
        id: 6,
        nome: 'Matue - Ultrassom Tour',
        artista: 'Matue',
        data: '2026-08-20',
        local: 'Sala São Jorge, São Paulo - SP',
        descricao: 'Turnê especial com som envolvente e performance de tirar o fôlego do artista.',
        preco: 160.00,
        imagem: './imagens/matueebrandao.jpg',
        tipo: 'evento'
    }
];

// Função para carregar hero dinâmico
function carregarHero() {
    const heroContainer = document.getElementById('hero-container');
    if (!heroContainer) return;

    heroContainer.innerHTML = '';

    EVENTS_DATA.forEach((evento, index) => {
        const heroItem = document.createElement('div');
        heroItem.className = 'hero-item';
        if (index === 0) heroItem.classList.add('active');
        
        // Se for slide inicial ou tem imagem de fundo, usa a imagem como background
        if (evento.tipo === 'slide-inicial') {
            heroItem.style.backgroundImage = `url('${evento.imagem}')`;
            heroItem.classList.add('hero-item-inicial');
        } else {
            heroItem.style.backgroundImage = `url('${evento.imagem}')`;
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

// Função para mudar o hero
function mudarHero(index) {
    const heroItems = document.querySelectorAll('.hero-item');
    const heroGots = document.querySelectorAll('.hero-dot');

    heroItems.forEach(item => item.classList.remove('active'));
    heroGots.forEach(dot => dot.classList.remove('active'));

    if (heroItems[index]) heroItems[index].classList.add('active');
    if (heroGots[index]) heroGots[index].classList.add('active');
}

// Função para iniciar controles do hero
function iniciarHeroControles() {
    const prevBtn = document.querySelector('.hero-prev');
    const nextBtn = document.querySelector('.hero-next');

    let currentIndex = 0;
    const totalEventos = EVENTS_DATA.length;

    if (prevBtn) {
        prevBtn.addEventListener('click', () => {
            currentIndex = (currentIndex - 1 + totalEventos) % totalEventos;
            mudarHero(currentIndex);
        });
    }

    if (nextBtn) {
        nextBtn.addEventListener('click', () => {
            currentIndex = (currentIndex + 1) % totalEventos;
            mudarHero(currentIndex);
        });
    }

    // Auto-change a cada 8 segundos
    setInterval(() => {
        currentIndex = (currentIndex + 1) % totalEventos;
        mudarHero(currentIndex);
    }, 8000);
}

// Função para carregar grid de eventos
function carregarGridEventos() {
    const eventsGrid = document.getElementById('events-grid');
    if (!eventsGrid) return;

    eventsGrid.innerHTML = '';

    // Filtra apenas os eventos (exclui o slide inicial)
    const eventos = EVENTS_DATA.filter(e => e.tipo === 'evento');

    eventos.forEach((evento) => {
        const card = document.createElement('a');
        card.href = `./paginas/painel-ingressos.html?id=${evento.id}`;
        card.className = 'event-card';

        card.innerHTML = `
            <img src="${evento.imagem}" alt="${evento.nome}" class="event-card-image" />
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
    });
}

// Initialize quando DOM carregar
document.addEventListener('DOMContentLoaded', () => {
    carregarHero();
    carregarGridEventos();
});
