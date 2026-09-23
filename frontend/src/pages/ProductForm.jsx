import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router';
import { getErrorMessage } from '../api/axiosInstance';
import { createProduct, getCategories, getProduct, updateProduct } from '../api/productsApi';

const EMPTY = { name: '', description: '', price: '', stock: '', categoryId: '' };

/** Formulario de creación (/products/new) y edición (/products/:id/edit). */
export default function ProductForm() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const [categories, setCategories] = useState([]);
  const [category, setCategory] = useState(null);
  const [loadError, setLoadError] = useState(null);
  const [serverError, setServerError] = useState(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm({ defaultValues: EMPTY, mode: 'onTouched' });

  useEffect(() => {
    getCategories().then(setCategories).catch((err) => setLoadError(getErrorMessage(err)));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    getProduct(id)
      .then((product) => {
        setCategory(product.category);
        reset({
          name: product.name,
          description: product.description,
          price: product.price,
          stock: product.stock,
          categoryId: String(product.category.id),
        });
      })
      .catch((err) => setLoadError(getErrorMessage(err)));
  }, [id, isEdit, reset]);

  const onSubmit = async (values) => {
    setServerError(null);
    const payload = {
      name: values.name.trim(),
      description: values.description.trim(),
      price: Number(values.price),
      stock: Number(values.stock),
      categoryId: Number(values.categoryId),
    };
    try {
      if (isEdit) {
        await updateProduct(id, payload);
      } else {
        await createProduct(payload);
      }
      navigate('/products');
    } catch (err) {
      setServerError(getErrorMessage(err));
    }
  };

  if (loadError) {
    return <p className="alert alert-error">{loadError}</p>;
  }

  return (
    <section>
      <div className="page-header">
        <h1>{isEdit ? `Editar producto #${id}` : 'Nuevo producto'}</h1>
        <Link to="/products" className="btn btn-ghost">← Volver</Link>
      </div>

      <div className="form-layout">
        <form className="card form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <label htmlFor="name">Nombre *</label>
          <input
            id="name"
            aria-invalid={Boolean(errors.name)}
            {...register('name', {
              required: 'El nombre es obligatorio.',
              validate: (v) => v.trim().length >= 2 || 'Debe tener al menos 2 caracteres.',
              maxLength: { value: 200, message: 'Máximo 200 caracteres.' },
            })}
          />
          {errors.name && <p className="field-error">{errors.name.message}</p>}

          <label htmlFor="description">Descripción</label>
          <textarea
            id="description"
            rows={3}
            aria-invalid={Boolean(errors.description)}
            {...register('description', { maxLength: { value: 2000, message: 'Máximo 2000 caracteres.' } })}
          />
          {errors.description && <p className="field-error">{errors.description.message}</p>}

          <div className="row">
            <div>
              <label htmlFor="price">Precio *</label>
              <input
                id="price"
                type="number"
                step="0.01"
                min="0"
                aria-invalid={Boolean(errors.price)}
                {...register('price', {
                  required: 'El precio es obligatorio.',
                  min: { value: 0, message: 'El precio no puede ser negativo.' },
                  max: { value: 99999999, message: 'Precio demasiado alto.' },
                  validate: (v) => /^\d+(\.\d{1,2})?$/.test(String(v)) || 'Máximo 2 decimales.',
                })}
              />
              {errors.price && <p className="field-error">{errors.price.message}</p>}
            </div>
            <div>
              <label htmlFor="stock">Stock *</label>
              <input
                id="stock"
                type="number"
                step="1"
                min="0"
                aria-invalid={Boolean(errors.stock)}
                {...register('stock', {
                  required: 'El stock es obligatorio.',
                  min: { value: 0, message: 'El stock no puede ser negativo.' },
                  validate: (v) => Number.isInteger(Number(v)) || 'Debe ser un número entero.',
                })}
              />
              {errors.stock && <p className="field-error">{errors.stock.message}</p>}
            </div>
          </div>

          <label htmlFor="categoryId">Categoría *</label>
          <select
            id="categoryId"
            aria-invalid={Boolean(errors.categoryId)}
            {...register('categoryId', { required: 'Selecciona una categoría.' })}
          >
            <option value="">Selecciona…</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
          {errors.categoryId && <p className="field-error">{errors.categoryId.message}</p>}
          {categories.length === 0 && (
            <p className="muted small">No hay categorías. Créalas con <code>scripts/seed.sh</code>.</p>
          )}

          {serverError && <p className="alert alert-error">{serverError}</p>}

          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear producto'}
          </button>
        </form>

        {isEdit && category && (
          <aside className="card category-card">
            <img src={category.photoUrl} alt={`Foto de la categoría ${category.name}`} />
            <p>Categoría actual: <strong>{category.name}</strong></p>
          </aside>
        )}
      </div>
    </section>
  );
}
