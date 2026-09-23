import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { getErrorMessage } from '../api/axiosInstance';
import { deleteProduct, getCategories, getProducts } from '../api/productsApi';

const SORT_OPTIONS = [
  { value: 'id', label: 'Más antiguos' },
  { value: 'createdAt_desc', label: 'Más recientes' },
  { value: 'name', label: 'Nombre A-Z' },
  { value: 'name_desc', label: 'Nombre Z-A' },
  { value: 'price', label: 'Precio menor' },
  { value: 'price_desc', label: 'Precio mayor' },
];

const FILTER_KEYS = ['search', 'categoryId', 'minPrice', 'maxPrice', 'sortBy'];
const PAGE_SIZE = 20;

const currency = new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'USD' });

export default function ProductList() {
  // Los filtros viven en la URL: se pueden compartir y sobreviven a recargar la página.
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get('page') ?? 1);
  const filters = Object.fromEntries(FILTER_KEYS.map((key) => [key, searchParams.get(key) ?? '']));

  const [draft, setDraft] = useState(filters);
  const [categories, setCategories] = useState([]);
  const [pendingDelete, setPendingDelete] = useState(null);
  const [actionError, setActionError] = useState(null);
  const [reloadCount, setReloadCount] = useState(0);
  const [response, setResponse] = useState({ key: null, result: null, error: null });

  const queryKey = searchParams.toString();
  const requestKey = `${queryKey}#${reloadCount}`;
  // "Cargando" se deriva: la última respuesta recibida no corresponde a la consulta actual.
  const loading = response.key !== requestKey;
  const result = response.result;
  const error = actionError ?? response.error;

  useEffect(() => {
    let cancelled = false;
    const params = Object.fromEntries(new URLSearchParams(queryKey));
    getProducts({ pageSize: PAGE_SIZE, ...params })
      .then((data) => !cancelled && setResponse({ key: requestKey, result: data, error: null }))
      .catch((err) => !cancelled && setResponse({ key: requestKey, result: null, error: getErrorMessage(err) }));
    return () => {
      cancelled = true;
    };
  }, [queryKey, requestKey]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const applyFilters = (event) => {
    event.preventDefault();
    const next = Object.fromEntries(Object.entries(draft).filter(([, value]) => value !== ''));
    setSearchParams(next);
  };

  const clearFilters = () => {
    setDraft(Object.fromEntries(FILTER_KEYS.map((key) => [key, ''])));
    setSearchParams({});
  };

  const goToPage = (nextPage) => {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(nextPage));
    setSearchParams(next);
  };

  const confirmDelete = async (id) => {
    setActionError(null);
    try {
      await deleteProduct(id);
      setPendingDelete(null);
      setReloadCount((count) => count + 1);
    } catch (err) {
      setActionError(getErrorMessage(err));
    }
  };

  const onDraftChange = (event) => setDraft({ ...draft, [event.target.name]: event.target.value });

  return (
    <section>
      <div className="page-header">
        <h1>Productos</h1>
        <Link to="/products/new" className="btn btn-primary">+ Nuevo producto</Link>
      </div>

      <form className="card filters" onSubmit={applyFilters}>
        <input name="search" placeholder="Buscar por nombre o descripción" value={draft.search} onChange={onDraftChange} />
        <select name="categoryId" value={draft.categoryId} onChange={onDraftChange}>
          <option value="">Todas las categorías</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <input name="minPrice" type="number" min="0" step="0.01" placeholder="Precio mín." value={draft.minPrice} onChange={onDraftChange} />
        <input name="maxPrice" type="number" min="0" step="0.01" placeholder="Precio máx." value={draft.maxPrice} onChange={onDraftChange} />
        <select name="sortBy" value={draft.sortBy} onChange={onDraftChange}>
          <option value="">Ordenar…</option>
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <div className="filters-actions">
          <button type="submit" className="btn btn-primary">Filtrar</button>
          <button type="button" className="btn btn-ghost" onClick={clearFilters}>Limpiar</button>
        </div>
      </form>

      {error && <p className="alert alert-error">{error}</p>}

      <div className="card table-wrapper">
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Categoría</th>
              <th className="num">Precio</th>
              <th className="num">Stock</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={6} className="muted">Cargando…</td></tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr><td colSpan={6} className="muted">No hay productos que coincidan con los filtros.</td></tr>
            )}
            {!loading && result?.items.map((product) => (
              <tr key={product.id}>
                <td>{product.id}</td>
                <td>
                  <div>{product.name}</div>
                  <div className="muted small">{product.description}</div>
                </td>
                <td><span className="badge">{product.categoryName}</span></td>
                <td className="num">{currency.format(product.price)}</td>
                <td className="num">{product.stock}</td>
                <td className="actions">
                  {pendingDelete === product.id ? (
                    <>
                      <span className="small">¿Eliminar?</span>
                      <button type="button" className="btn btn-danger btn-sm" onClick={() => confirmDelete(product.id)}>Sí</button>
                      <button type="button" className="btn btn-ghost btn-sm" onClick={() => setPendingDelete(null)}>No</button>
                    </>
                  ) : (
                    <>
                      <Link to={`/products/${product.id}/edit`} className="btn btn-ghost btn-sm">Editar</Link>
                      <button type="button" className="btn btn-ghost btn-sm" onClick={() => setPendingDelete(product.id)}>Eliminar</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result && result.totalPages > 0 && (
        <div className="pagination">
          <button type="button" className="btn btn-ghost" disabled={!result.hasPreviousPage} onClick={() => goToPage(page - 1)}>
            ← Anterior
          </button>
          <span>
            Página {result.page} de {result.totalPages.toLocaleString('es-CO')} · {result.totalCount.toLocaleString('es-CO')} productos
          </span>
          <button type="button" className="btn btn-ghost" disabled={!result.hasNextPage} onClick={() => goToPage(page + 1)}>
            Siguiente →
          </button>
        </div>
      )}
    </section>
  );
}
