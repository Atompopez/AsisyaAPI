const TOKEN_KEY = 'asisya_token';

/** Decodifica el payload de un JWT (sin validar la firma: eso lo hace la API). */
export function decodeToken(token) {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(payload));
  } catch {
    return null;
  }
}

export function isTokenValid(token) {
  const payload = token ? decodeToken(token) : null;
  return Boolean(payload?.exp && payload.exp * 1000 > Date.now());
}

export const tokenStorage = {
  EXPIRED_EVENT: 'auth:expired',
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};
