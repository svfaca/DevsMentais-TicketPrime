/**
 * Configuração centralizada da API Base URL
 * Define o endpoint base do backend com fallback automático
 */
export const API_BASE_URL = (() => {
  const configuredUrl = localStorage.getItem('API_URL');
  if (configuredUrl) {
    return configuredUrl.replace(/\/+$/, '');
  }

  if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    // local backend (when running via dotnet run) listens on 8080 in this project
    return 'http://localhost:8080';
  }

  return 'https://devsmentais-ticketprime-production.up.railway.app';
})();
