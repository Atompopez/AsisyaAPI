import ProductForm from '../pages/ProductForm';
import ProductList from '../pages/ProductList';

/** Módulo de rutas de productos (equivalente a un módulo de rutas hijo en Angular). */
const productRoutes = [
  { path: 'products', element: <ProductList /> },
  { path: 'products/new', element: <ProductForm /> },
  { path: 'products/:id/edit', element: <ProductForm /> },
];

export default productRoutes;
