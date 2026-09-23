import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { getErrorMessage } from '../api/axiosInstance';
import { useAuth } from '../hooks/useAuth';

export default function Login() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [serverError, setServerError] = useState(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ defaultValues: { username: '', password: '' } });

  const redirectTo = location.state?.from?.pathname ?? '/products';

  if (isAuthenticated) {
    return <Navigate to={redirectTo} replace />;
  }

  const onSubmit = async ({ username, password }) => {
    setServerError(null);
    try {
      await login(username, password);
      navigate(redirectTo, { replace: true });
    } catch (error) {
      setServerError(getErrorMessage(error, 'No se pudo iniciar sesión.'));
    }
  };

  return (
    <div className="login-page">
      <form className="card login-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <h1>Iniciar sesión</h1>

        <label htmlFor="username">Usuario</label>
        <input
          id="username"
          autoComplete="username"
          aria-invalid={Boolean(errors.username)}
          {...register('username', { required: 'El usuario es obligatorio.' })}
        />
        {errors.username && <p className="field-error">{errors.username.message}</p>}

        <label htmlFor="password">Contraseña</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          aria-invalid={Boolean(errors.password)}
          {...register('password', { required: 'La contraseña es obligatoria.' })}
        />
        {errors.password && <p className="field-error">{errors.password.message}</p>}

        {serverError && <p className="alert alert-error">{serverError}</p>}

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando…' : 'Ingresar'}
        </button>
      </form>
    </div>
  );
}
