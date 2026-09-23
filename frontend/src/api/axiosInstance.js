import axios from 'axios';
import { tokenStorage } from '../auth/tokenStorage';

export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5152';

const axiosInstance = axios.create({
  baseURL: API_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Agrega el JWT guardado en localStorage a cada request.
axiosInstance.interceptors.request.use((config) => {
  const token = tokenStorage.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Si la API responde 401 (token vencido o inválido) se cierra la sesión y AuthGuard redirige a /login.
axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLogin = error.config?.url?.includes('/api/auth/login');
    if (error.response?.status === 401 && !isLogin) {
      tokenStorage.clear();
      window.dispatchEvent(new Event(tokenStorage.EXPIRED_EVENT));
    }
    return Promise.reject(error);
  },
);

/** Extrae un mensaje legible de un error de la API (ProblemDetails / ValidationProblemDetails). */
export function getErrorMessage(error, fallback = 'Ocurrió un error inesperado.') {
  const data = error?.response?.data;
  if (data?.errors) {
    return Object.values(data.errors).flat().join(' ');
  }
  return data?.detail ?? data?.title ?? error?.message ?? fallback;
}

export default axiosInstance;
