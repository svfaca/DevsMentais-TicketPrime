/**
 * Configuração centralizada da API Base URL
 * Define o endpoint base do backend com fallback automático
 */
export const API_BASE_URL = (() => {
  const configuredUrl = localStorage.getItem('API_URL');
  if (configuredUrl) {
    return configuredUrl.replace(/\/+$/, '');
  }

  if (window.location.hostname === 'localhost') {
    return 'http://localhost:5129';
  }

  return 'https://devsmentais-ticketprime-production.up.railway.app';
})();
