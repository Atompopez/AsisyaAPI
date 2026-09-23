import { useCallback, useEffect, useMemo, useState } from 'react';
import { login as loginRequest } from '../api/authApi';
import { decodeToken, isTokenValid, tokenStorage } from '../auth/tokenStorage';
import { AuthContext } from './AuthContext';

function readStoredToken() {
  const token = tokenStorage.get();
  if (isTokenValid(token)) {
    return token;
  }
  tokenStorage.clear();
  return null;
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(readStoredToken);

  const logout = useCallback(() => {
    tokenStorage.clear();
    setToken(null);
  }, []);

  const login = useCallback(async (username, password) => {
    const response = await loginRequest(username, password);
    tokenStorage.set(response.token);
    setToken(response.token);
    return response;
  }, []);

  // El interceptor de axios emite este evento cuando la API responde 401.
  useEffect(() => {
    const onExpired = () => setToken(null);
    window.addEventListener(tokenStorage.EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(tokenStorage.EXPIRED_EVENT, onExpired);
  }, []);

  const value = useMemo(
    () => ({
      token,
      username: token ? decodeToken(token)?.unique_name : null,
      isAuthenticated: isTokenValid(token),
      login,
      logout,
    }),
    [token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
