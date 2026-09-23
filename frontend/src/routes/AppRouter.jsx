import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import AuthGuard from '../components/AuthGuard';
import Layout from '../components/Layout';
import Login from '../pages/Login';
import NotFound from '../pages/NotFound';
import productRoutes from './productRoutes';

/**
 * Enrutamiento raíz (equivalente al AppRoutingModule de Angular): rutas públicas y, bajo
 * AuthGuard, los módulos protegidos.
 */
const router = createBrowserRouter([
  { path: '/login', element: <Login /> },
  {
    element: <AuthGuard />,
    children: [
      {
        element: <Layout />,
        children: [
          { index: true, element: <Navigate to="/products" replace /> },
          ...productRoutes,
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
]);

export default function AppRouter() {
  return <RouterProvider router={router} />;
}
