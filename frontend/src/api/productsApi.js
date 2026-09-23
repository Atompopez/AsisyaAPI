import axiosInstance from './axiosInstance';

/** Quita los parámetros vacíos para no enviar `?search=&minPrice=`. */
function cleanParams(params) {
  return Object.fromEntries(
    Object.entries(params).filter(([, value]) => value !== '' && value !== null && value !== undefined),
  );
}

export async function getProducts(params) {
  const { data } = await axiosInstance.get('/Products', { params: cleanParams(params) });
  return data;
}

export async function getProduct(id) {
  const { data } = await axiosInstance.get(`/Products/${id}`);
  return data;
}

export async function createProduct(product) {
  const { data } = await axiosInstance.post('/Product', product);
  return data;
}

export async function updateProduct(id, product) {
  const { data } = await axiosInstance.put(`/Products/${id}`, product);
  return data;
}

export async function deleteProduct(id) {
  await axiosInstance.delete(`/Products/${id}`);
}

export async function getCategories() {
  const { data } = await axiosInstance.get('/Category');
  return data;
}
