/**
 * Script de inicialização que importa API_BASE_URL e exponha como variável global
 * Deve ser carregado como módulo antes dos scripts que usam API_BASE_URL
 */
import { API_BASE_URL } from '../config/api.js';

// Expor como variável global para scripts inline
window.API_BASE_URL = API_BASE_URL;
