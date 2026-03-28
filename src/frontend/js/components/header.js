/**
 * header.js — Componente reutilizável do cabeçalho (navbar) + backdrop.
 *
 * Exporta: window.mountHeader()
 * Chamado internamente por sidebar.js via initAppShell().
 */
(function () {
  'use strict';

  /**
   * Injeta a <header class="site-navbar"> e o botão de backdrop no início do <body>.
   * Idempotente: não duplica se já existir.
   */
  function mountHeader() {
    if (document.querySelector('header.site-navbar')) return;

    const header = document.createElement('header');
    header.className = 'site-navbar';
    header.innerHTML = `
      <div class="navbar-logo">
        <img src="../imagens/logo.png" alt="Logo TicketPrime" class="logo-img" />
        <span>TicketPrime</span>
      </div>
      <img
        id="menuToggle"
        class="menu-toggle profile-avatar-button"
        src=""
        alt="Abrir menu lateral"
        aria-label="Abrir menu lateral"
        aria-controls="userSidebar"
        aria-expanded="false"
        role="button"
        tabindex="0"
      />
    `;

    const backdrop = document.createElement('button');
    backdrop.className = 'sidebar-backdrop';
    backdrop.id = 'sidebarBackdrop';
    backdrop.type = 'button';
    backdrop.setAttribute('aria-label', 'Fechar menu lateral');

    document.body.insertBefore(header, document.body.firstChild);
    document.body.insertBefore(backdrop, header.nextSibling);
  }

  window.mountHeader = mountHeader;
})();
