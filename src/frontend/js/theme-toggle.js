(function setupThemeToggle() {
  const STORAGE_KEY = 'ticketprimeTheme';

  function getInitialTheme() {
    const stored = String(localStorage.getItem(STORAGE_KEY) || '').trim().toLowerCase();
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    return 'light';
  }

  function applyTheme(theme) {
    const isLight = theme === 'light';
    document.body.classList.toggle('light-mode', isLight);

    document.querySelectorAll('.logo-img').forEach((img) => {
      img.src = img.src.replace(
        isLight ? 'logo.png' : 'logo-modified.png',
        isLight ? 'logo-modified.png' : 'logo.png'
      );
    });

    const toggles = document.querySelectorAll('.theme-toggle');
    toggles.forEach((toggleButton) => {
      const icon = toggleButton.querySelector('.theme-toggle-icon');
      toggleButton.setAttribute('aria-pressed', String(isLight));
      toggleButton.setAttribute(
        'aria-label',
        isLight ? 'Mudar para modo escuro' : 'Mudar para modo claro'
      );

      if (icon) {
        icon.className = `bx ${isLight ? 'bx-moon' : 'bx-sun'} theme-toggle-icon`;
      }
    });
  }

  function persistTheme(theme) {
    localStorage.setItem(STORAGE_KEY, theme);
  }

  function toggleTheme() {
    const nextTheme = document.body.classList.contains('light-mode') ? 'dark' : 'light';
    applyTheme(nextTheme);
    persistTheme(nextTheme);
  }

  document.addEventListener('DOMContentLoaded', () => {
    const toggles = document.querySelectorAll('.theme-toggle');
    if (!toggles.length) {
      return;
    }

    const initialTheme = getInitialTheme();
    applyTheme(initialTheme);

    toggles.forEach((toggleButton) => {
      toggleButton.addEventListener('click', toggleTheme);
    });
  });
})();
