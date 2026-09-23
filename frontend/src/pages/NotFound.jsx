import { Link } from 'react-router';

export default function NotFound() {
  return (
    <div className="login-page">
      <div className="card login-card">
        <h1>Página no encontrada</h1>
        <Link to="/products" className="btn btn-primary">Ir a productos</Link>
      </div>
    </div>
  );
}
