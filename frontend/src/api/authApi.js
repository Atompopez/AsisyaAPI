import axiosInstance from './axiosInstance';

export async function login(username, password) {
  const { data } = await axiosInstance.post('/api/auth/login', { username, password });
  return data;
}
