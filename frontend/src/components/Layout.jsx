import { NavLink, Outlet, useNavigate } from 'react-router';
import { useAuth } from '../hooks/useAuth';

export default function Layout() {
  const { username, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app">
      <header className="topbar">
        <span className="brand">Asisya · Productos</span>
        <nav>
          <NavLink to="/products" end>Listado</NavLink>
          <NavLink to="/products/new">Nuevo producto</NavLink>
        </nav>
        <div className="session">
          <span>{username}</span>
          <button type="button" className="btn btn-ghost" onClick={handleLogout}>Salir</button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
