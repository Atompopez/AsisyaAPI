# Frontend Asisya (React + Vite)

SPA para autenticarse contra la API y gestionar productos. Ver el [README principal](../README.md)
para la arquitectura completa y la adaptación de términos Angular → React.

```bash
cp .env.example .env      # VITE_API_URL apunta a la API
npm install
npm run dev               # http://localhost:5173
npm run lint
npm run build
```

Estructura:

| Ruta | Rol |
|------|-----|
| `src/api/axiosInstance.js` | instancia de axios con interceptor que agrega el JWT de `localStorage` y cierra sesión ante un 401 |
| `src/context/AuthContext.jsx`, `src/hooks/useAuth.js` | estado de autenticación (login/logout) |
| `src/components/AuthGuard.jsx` | protege las rutas de productos y redirige a `/login` |
| `src/routes/AppRouter.jsx`, `src/routes/productRoutes.jsx` | enrutamiento modular con React Router |
| `src/pages/Login.jsx`, `ProductList.jsx`, `ProductForm.jsx` | pantallas; los formularios usan React Hook Form |
