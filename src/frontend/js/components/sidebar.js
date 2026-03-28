/**
 * sidebar.js — Componente reutilizável da sidebar + modal de configurações.
 *
 * Exporta:  window.initAppShell({ role, apiBaseUrl })
 *
 * Uso em cada página autenticada:
 *   const shell = await initAppShell({ role: 'usuario', apiBaseUrl });
 *   // shell.session  → dados atualizados do usuário
 *   // shell.getAuthHeaders() → { Authorization: 'Bearer ...' }
 *
 * Papéis suportados: 'usuario' | 'adm'
 */
(function () {
  'use strict';

  // ─── Itens do menu por papel ──────────────────────────────────────────────

  function buildNavItems(role) {
    if (role === 'adm') {
      return `
        <a href="./Homeauthadm.html" class="user-sidebar-link">
          <i class="bx bxs-home"></i><span>Painel</span>
        </a>
        <a href="./painel-ingressos.html" class="user-sidebar-link">
          <i class="bx bx-calendar-plus"></i><span>Criar evento</span>
        </a>
        <a href="./painel-ingressos.html" class="user-sidebar-link">
          <i class="bx bxs-calendar-event"></i><span>Meus eventos</span>
        </a>
        <button class="user-sidebar-link" type="button" data-open-settings="true">
          <i class="bx bx-cog"></i><span>Configuracoes</span>
        </button>
      `;
    }

    // usuario
    return `
      <a href="./meus-ingressos.html" class="user-sidebar-link">
        <i class="bx bx-ticket"></i><span>Meus ingressos</span>
      </a>
      <button class="user-sidebar-link" id="createEventButton" type="button">
        <i class="bx bx-calendar-plus"></i><span>Criar evento</span>
      </button>
      <button class="user-sidebar-link" type="button" data-open-settings="true">
        <i class="bx bx-cog"></i><span>Configuracoes</span>
      </button>
    `;
  }

  // ─── Injeção de HTML ──────────────────────────────────────────────────────

  function mountSidebarHTML(role) {
    if (document.getElementById('userSidebar')) return;

    const aside = document.createElement('aside');
    aside.className = 'user-sidebar';
    aside.id = 'userSidebar';
    aside.setAttribute('aria-hidden', 'true');
    aside.innerHTML = `
      <div class="user-sidebar-header">
        <h2>Minha Conta</h2>
        <button class="menu-close" id="menuClose" type="button" aria-label="Fechar menu lateral">
          <i class="bx bx-x"></i>
        </button>
      </div>

      <div class="user-profile-card">
        <div class="profile-avatar" id="profileAvatar" aria-label="Foto de perfil">
          <img id="profileAvatarImage" alt="Foto de perfil" hidden />
          <i class="bx bx-user" id="profileAvatarFallback"></i>
        </div>
        <h3 id="profileName">Usuario TicketPrime</h3>
        <p id="profileEmail">usuario@ticketprime.com.br</p>
      </div>

      <nav class="user-sidebar-nav" aria-label="Opcoes da conta">
        ${buildNavItems(role)}
      </nav>

      <button class="sidebar-logout" id="logoutButton" type="button">
        <i class="bx bx-log-out"></i><span>Sair</span>
      </button>
    `;

    const backdrop = document.getElementById('sidebarBackdrop');
    backdrop.insertAdjacentElement('afterend', aside);

    // Modal de configurações (compartilhado entre user e adm)
    const modal = document.createElement('section');
    modal.className = 'profile-settings-modal';
    modal.id = 'profileSettingsModal';
    modal.setAttribute('hidden', '');
    modal.innerHTML = `
      <div class="profile-settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settingsTitle">
        <div class="profile-settings-head">
          <h2 id="settingsTitle">Configuracoes da conta</h2>
          <button class="settings-close" id="settingsClose" type="button" aria-label="Fechar configuracoes">
            <i class="bx bx-x"></i>
          </button>
        </div>

        <form id="profileSettingsForm" class="profile-settings-form">
          <div class="settings-photo-row">
            <div class="settings-photo-current" aria-label="Foto atual do usuario">
              <img id="settingsPhotoPreviewImage" alt="Foto atual do usuario" hidden />
              <i class="bx bx-user" id="settingsPhotoPreviewFallback"></i>
            </div>
            <div class="settings-photo-actions">
              <p>Foto atual do perfil</p>
              <label for="settingsPhoto" class="settings-photo-button">Alterar foto</label>
              <input
                id="settingsPhoto"
                name="settingsPhoto"
                type="file"
                accept="image/*"
                class="settings-photo-input"
              />
              <span class="profile-photo-help">Escolha uma imagem para atualizar sua foto.</span>
            </div>
          </div>

          <label for="settingsName">Nome</label>
          <input id="settingsName" name="settingsName" type="text" placeholder="Seu nome" />

          <label for="settingsEmail">Email</label>
          <input id="settingsEmail" name="settingsEmail" type="email" placeholder="email@exemplo.com" />

          <label for="settingsPhone">Telefone</label>
          <input id="settingsPhone" name="settingsPhone" type="text" placeholder="(11) 99999-9999" />

          <button class="btn" id="settingsSaveButton" type="submit" disabled>Salvar alteracoes</button>
          <button class="btn btn-danger" id="deleteAccountButton" type="button">Excluir conta</button>
        </form>
      </div>
    `;

    document.body.appendChild(modal);
  }

  // ─── Lógica principal ─────────────────────────────────────────────────────

  /**
   * Inicializa o shell completo (header, sidebar, modal, avatar, perfil, logout).
   *
   * @param {{ role: 'usuario'|'adm', apiBaseUrl: string }} options
   * @returns {Promise<{ session: object, getAuthHeaders: () => object, apiBaseUrl: string } | null>}
   *   Retorna null e redireciona se não houver sessão válida.
   */
  async function initAppShell({ role, apiBaseUrl }) {
    let session;

    try {
      session = JSON.parse(localStorage.getItem('ticketprimeUser') || 'null');
    } catch {
      session = null;
    }

    if (!session || typeof session !== 'object') {
      window.location.replace('./login.html');
      return null;
    }

    // Injeta header (se ainda não existir)
    if (typeof window.mountHeader === 'function') {
      window.mountHeader();
    }

    // Injeta sidebar + modal
    mountSidebarHTML(role);

    // ── Referências de DOM ────────────────────────────────────────────────

    const body = document.body;
    const sidebar = document.getElementById('userSidebar');
    const backdrop = document.getElementById('sidebarBackdrop');
    const menuToggle = document.getElementById('menuToggle');
    const menuClose = document.getElementById('menuClose');
    const profileName = document.getElementById('profileName');
    const profileEmail = document.getElementById('profileEmail');
    const profileAvatarImage = document.getElementById('profileAvatarImage');
    const profileAvatarFallback = document.getElementById('profileAvatarFallback');
    const settingsModal = document.getElementById('profileSettingsModal');
    const settingsClose = document.getElementById('settingsClose');
    const settingsForm = document.getElementById('profileSettingsForm');
    const settingsSaveButton = document.getElementById('settingsSaveButton');
    const logoutButton = document.getElementById('logoutButton');
    const settingsNameInput = document.getElementById('settingsName');
    const settingsEmailInput = document.getElementById('settingsEmail');
    const settingsPhoneInput = document.getElementById('settingsPhone');
    const settingsPhotoInput = document.getElementById('settingsPhoto');
    const settingsPhotoPreviewImage = document.getElementById('settingsPhotoPreviewImage');
    const settingsPhotoPreviewFallback = document.getElementById('settingsPhotoPreviewFallback');
    const deleteAccountButton = document.getElementById('deleteAccountButton');
    const createEventButton = document.getElementById('createEventButton'); // só existe no papel 'usuario'
    const openSettingsButtons = document.querySelectorAll('[data-open-settings="true"]');

    let selectedProfilePhoto = '';
    let selectedProfilePhotoFile = null;
    let photoPreviewObjectUrl = '';
    let initialProfileState = { nome: '', email: '', telefone: '', fotoPerfil: '' };

    // ── Helpers de autenticação ───────────────────────────────────────────

    function getToken() {
      return String(session?.token ?? session?.Token ?? '').trim();
    }

    function getAuthHeaders() {
      const token = getToken();
      return token ? { Authorization: `Bearer ${token}` } : {};
    }

    function persistSession() {
      localStorage.setItem('ticketprimeUser', JSON.stringify(session));
    }

    // ── Avatar ────────────────────────────────────────────────────────────

    function addCacheBust(url) {
      if (!url || url.startsWith('blob:') || url.startsWith('data:')) return url;
      const [withoutHash, hashFragment = ''] = String(url).split('#');
      const separator = withoutHash.includes('?') ? '&' : '?';
      return `${withoutHash}${separator}t=${Date.now()}${hashFragment ? `#${hashFragment}` : ''}`;
    }

    function getDefaultHeaderAvatar() {
      return "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Crect width='100' height='100' fill='%23242323'/%3E%3Ccircle cx='50' cy='37' r='20' fill='%23f2f2f2'/%3E%3Cpath d='M16 92c5-18 19-30 34-30s29 12 34 30' fill='%23f2f2f2'/%3E%3C/svg%3E";
    }

    function resolvePhotoUrl(photoUrl) {
      if (!photoUrl) return '';
      return photoUrl.startsWith('/') ? apiBaseUrl + photoUrl : photoUrl;
    }

    function renderHeaderAvatar(photoUrl, { forceRefresh = false } = {}) {
      const fullUrl = resolvePhotoUrl(photoUrl);
      const src = forceRefresh ? addCacheBust(fullUrl) : fullUrl;
      menuToggle.onerror = () => {
        menuToggle.onerror = null;
        menuToggle.src = getDefaultHeaderAvatar();
      };
      menuToggle.src = src || getDefaultHeaderAvatar();
    }

    function updateAvatar(photoUrl, { forceRefresh = false } = {}) {
      const fullUrl = resolvePhotoUrl(photoUrl);
      const src = forceRefresh ? addCacheBust(fullUrl) : fullUrl;

      if (src) {
        profileAvatarImage.onerror = () => {
          profileAvatarImage.hidden = true;
          profileAvatarFallback.hidden = false;
        };
        profileAvatarImage.src = src;
        profileAvatarImage.hidden = false;
        profileAvatarFallback.hidden = true;

        settingsPhotoPreviewImage.onerror = () => {
          settingsPhotoPreviewImage.hidden = true;
          settingsPhotoPreviewFallback.hidden = false;
        };
        settingsPhotoPreviewImage.src = src;
        settingsPhotoPreviewImage.hidden = false;
        settingsPhotoPreviewFallback.hidden = true;

        renderHeaderAvatar(photoUrl, { forceRefresh });
        return;
      }

      profileAvatarImage.hidden = true;
      profileAvatarImage.removeAttribute('src');
      profileAvatarFallback.hidden = false;

      settingsPhotoPreviewImage.hidden = true;
      settingsPhotoPreviewImage.removeAttribute('src');
      settingsPhotoPreviewFallback.hidden = false;

      renderHeaderAvatar('');
    }

    function syncSessionProfilePhoto(photoUrl) {
      const normalized = String(photoUrl || '').trim();
      selectedProfilePhoto = normalized;
      session.fotoPerfil = normalized;
      persistSession();
      updateAvatar(normalized, { forceRefresh: true });
    }

    // ── Dados do perfil ───────────────────────────────────────────────────

    function getUserValue(keys, fallback) {
      for (const key of keys) {
        const value = session?.[key];
        if (value !== undefined && value !== null && String(value).trim() !== '') {
          return String(value).trim();
        }
      }
      return fallback;
    }

    function refreshProfileInfo() {
      const nome = getUserValue(['nome', 'Nome', 'name', 'Name'], 'Usuario TicketPrime');
      const email = getUserValue(['email', 'Email'], 'usuario@ticketprime.com.br');
      const telefone = getUserValue(['telefone', 'Telefone', 'phone', 'Phone'], '');
      const foto = getUserValue(
        ['fotoPerfil', 'FotoPerfil', 'foto', 'Foto', 'avatar', 'Avatar', 'profileImage'],
        ''
      );

      profileName.textContent = nome;
      profileEmail.textContent = email;
      updateAvatar(foto, { forceRefresh: true });

      settingsNameInput.value = nome;
      settingsEmailInput.value = email;
      settingsPhoneInput.value = telefone;
      selectedProfilePhoto = foto;
      selectedProfilePhotoFile = null;
      if (photoPreviewObjectUrl) {
        URL.revokeObjectURL(photoPreviewObjectUrl);
        photoPreviewObjectUrl = '';
      }
      settingsPhotoInput.value = '';

      initialProfileState = {
        nome: settingsNameInput.value.trim(),
        email: settingsEmailInput.value.trim(),
        telefone: settingsPhoneInput.value.trim(),
        fotoPerfil: String(selectedProfilePhoto || '').trim()
      };
      updateSaveButtonState();
    }

    // ── Estado do formulário de configurações ─────────────────────────────

    function getCurrentProfileState() {
      return {
        nome: settingsNameInput.value.trim(),
        email: settingsEmailInput.value.trim(),
        telefone: settingsPhoneInput.value.trim(),
        fotoPerfil: String(selectedProfilePhoto || '').trim()
      };
    }

    function hasProfileChanges() {
      const current = getCurrentProfileState();
      return (
        current.nome !== initialProfileState.nome ||
        current.email !== initialProfileState.email ||
        current.telefone !== initialProfileState.telefone ||
        current.fotoPerfil !== initialProfileState.fotoPerfil ||
        selectedProfilePhotoFile !== null
      );
    }

    function updateSaveButtonState() {
      settingsSaveButton.disabled = !hasProfileChanges();
    }

    // ── Sidebar open/close ────────────────────────────────────────────────

    function openSidebar() {
      body.classList.add('sidebar-open');
      sidebar.setAttribute('aria-hidden', 'false');
      menuToggle.setAttribute('aria-expanded', 'true');
    }

    function closeSidebar() {
      body.classList.remove('sidebar-open');
      sidebar.setAttribute('aria-hidden', 'true');
      menuToggle.setAttribute('aria-expanded', 'false');
    }

    function openSettings() {
      settingsModal.hidden = false;
      body.classList.add('settings-open');
      setTimeout(() => settingsNameInput.focus(), 20);
    }

    function closeSettings() {
      settingsModal.hidden = true;
      body.classList.remove('settings-open');
    }

    // ── Event listeners ───────────────────────────────────────────────────

    menuToggle.addEventListener('click', openSidebar);
    menuToggle.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); openSidebar(); }
    });
    menuClose.addEventListener('click', closeSidebar);
    backdrop.addEventListener('click', closeSidebar);

    for (const btn of openSettingsButtons) {
      btn.addEventListener('click', () => { closeSidebar(); openSettings(); });
    }

    settingsNameInput.addEventListener('input', updateSaveButtonState);
    settingsEmailInput.addEventListener('input', updateSaveButtonState);
    settingsPhoneInput.addEventListener('input', updateSaveButtonState);

    settingsPhotoInput.addEventListener('change', () => {
      const [file] = settingsPhotoInput.files || [];
      if (!file) {
        selectedProfilePhotoFile = null;
        if (photoPreviewObjectUrl) { URL.revokeObjectURL(photoPreviewObjectUrl); photoPreviewObjectUrl = ''; }
        updateAvatar(selectedProfilePhoto);
        updateSaveButtonState();
        return;
      }

      if (!file.type.startsWith('image/')) {
        alert('Selecione um arquivo de imagem valido.');
        settingsPhotoInput.value = '';
        selectedProfilePhotoFile = null;
        updateAvatar(selectedProfilePhoto);
        updateSaveButtonState();
        return;
      }

      selectedProfilePhotoFile = file;
      if (photoPreviewObjectUrl) URL.revokeObjectURL(photoPreviewObjectUrl);
      photoPreviewObjectUrl = URL.createObjectURL(file);
      settingsPhotoPreviewImage.src = photoPreviewObjectUrl;
      settingsPhotoPreviewImage.hidden = false;
      settingsPhotoPreviewFallback.hidden = true;
      updateSaveButtonState();
    });

    settingsClose.addEventListener('click', closeSettings);
    settingsModal.addEventListener('click', (e) => { if (e.target === settingsModal) closeSettings(); });

    // ── Salvar perfil ─────────────────────────────────────────────────────

    settingsForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      if (!hasProfileChanges()) return;

      const nome = settingsNameInput.value.trim();
      const email = settingsEmailInput.value.trim();
      const telefone = settingsPhoneInput.value.trim();
      let fotoPerfil = selectedProfilePhoto;

      if (!nome || !email) {
        alert('Nome e email sao obrigatorios.');
        return;
      }

      settingsSaveButton.disabled = true;
      settingsSaveButton.textContent = 'Salvando...';

      try {
        if (selectedProfilePhotoFile) {
          const formData = new FormData();
          formData.append('file', selectedProfilePhotoFile);

          const photoRes = await fetch(`${apiBaseUrl}/api/me/foto`, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: formData
          });

          if (!photoRes.ok) {
            const payload = await photoRes.json().catch(() => ({}));
            alert(typeof payload === 'string' ? payload : payload?.mensagem || 'Erro ao enviar foto.');
            settingsSaveButton.textContent = 'Salvar alteracoes';
            updateSaveButtonState();
            return;
          }

          const photoData = await photoRes.json();
          fotoPerfil = String(photoData?.url || '').trim();
          syncSessionProfilePhoto(fotoPerfil);
          selectedProfilePhotoFile = null;
          settingsPhotoInput.value = '';
          if (photoPreviewObjectUrl) { URL.revokeObjectURL(photoPreviewObjectUrl); photoPreviewObjectUrl = ''; }
        }

        const res = await fetch(`${apiBaseUrl}/api/me`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
          body: JSON.stringify({ nome, email, telefone, ...(fotoPerfil ? { fotoPerfil } : {}) })
        });

        if (!res.ok) {
          const payload = await res.json().catch(() => ({}));
          alert(typeof payload === 'string' ? payload : payload?.mensagem || 'Nao foi possivel salvar.');
          settingsSaveButton.textContent = 'Salvar alteracoes';
          updateSaveButtonState();
          return;
        }

        // Preserva tipoConta — não rebaixa admin para usuario
        session = { ...session, nome, email, telefone, fotoPerfil };
        persistSession();
        refreshProfileInfo();
        closeSettings();
        closeSidebar();
        alert('Configuracoes salvas com sucesso.');
      } catch {
        alert('Nao foi possivel conectar com a API.');
        updateSaveButtonState();
        settingsSaveButton.textContent = 'Salvar alteracoes';
        return;
      }

      settingsSaveButton.textContent = 'Salvar alteracoes';
    });

    // ── Excluir conta ─────────────────────────────────────────────────────

    deleteAccountButton.addEventListener('click', async () => {
      if (!window.confirm('Tem certeza que deseja excluir sua conta? Esta acao nao pode ser desfeita.')) return;

      try {
        const res = await fetch(`${apiBaseUrl}/api/me`, {
          method: 'DELETE',
          headers: getAuthHeaders()
        });

        if (!res.ok) {
          const payload = await res.json().catch(() => ({}));
          alert(typeof payload === 'string' ? payload : payload?.mensagem || 'Nao foi possivel excluir sua conta.');
          return;
        }

        localStorage.removeItem('ticketprimeUser');
        alert('Conta excluida com sucesso.');
        window.location.replace('./login.html');
      } catch {
        alert('Nao foi possivel conectar com a API.');
      }
    });

    // ── Criar evento (promover para admin) — apenas papel 'usuario' ───────

    if (createEventButton) {
      createEventButton.addEventListener('click', async () => {
        if (!window.confirm('Sua conta esta prestes a se tornar uma conta adm. Deseja continuar?')) return;

        try {
          const res = await fetch(`${apiBaseUrl}/api/me/promover-admin`, {
            method: 'POST',
            headers: getAuthHeaders()
          });

          if (!res.ok) {
            const payload = await res.json().catch(() => ({}));
            alert(typeof payload === 'string' ? payload : payload?.mensagem || 'Nao foi possivel promover sua conta.');
            return;
          }

          const upgraded = await res.json();
          session = {
            ...session,
            ...upgraded,
            tipoConta: 'adm',
            token: upgraded?.token ?? upgraded?.Token ?? session?.token
          };
          persistSession();
          alert('Conta promovida para administrador com sucesso.');
          window.location.replace('./Homeauthadm.html');
        } catch {
          alert('Nao foi possivel conectar com a API.');
        }
      });
    }

    // ── Logout ────────────────────────────────────────────────────────────

    logoutButton.addEventListener('click', () => {
      localStorage.removeItem('ticketprimeUser');
      window.location.replace('../index.html');
    });

    // ── Fechar com Escape ─────────────────────────────────────────────────

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') { closeSettings(); closeSidebar(); }
    });

    // ── Sincronizar dados do banco ────────────────────────────────────────

    async function syncUserDataFromBank() {
      const token = getToken();
      if (!token) return;

      try {
        const res = await fetch(`${apiBaseUrl}/api/me`, {
          method: 'GET',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }
        });

        if (res.ok) {
          const data = await res.json();
          const bankFoto = String(data.fotoPerfil ?? data.FotoPerfil ?? '').trim();
          session = {
            ...session,
            nome: data.nome ?? data.Nome ?? session?.nome,
            email: data.email ?? data.Email ?? session?.email,
            telefone: data.telefone ?? data.Telefone ?? session?.telefone,
            fotoPerfil: bankFoto || session?.fotoPerfil
          };
          persistSession();
        }
      } catch {
        // silent — usa dados do localStorage
      }
    }

    // ── Inicialização ─────────────────────────────────────────────────────

    await syncUserDataFromBank();
    refreshProfileInfo();

    return { session, getAuthHeaders, apiBaseUrl };
  }

  window.initAppShell = initAppShell;
})();
