import axios from 'axios';
import { LocalVariables } from './Enums';

export interface DemoUser { id: string; name: string; token: string }
export const apiUrl = (import.meta.env.VITE_API_URL || 'http://localhost:8081/').replace(/\/?$/, '/');
export const http = axios.create({ baseURL: apiUrl, timeout: 15000 });
export function selectDemoUser(user: DemoUser): void {
  localStorage.setItem(LocalVariables.UserId, user.id);
  localStorage.setItem('DemoToken', user.token);
  if (!localStorage.getItem(LocalVariables.DeviceId))
    localStorage.setItem(LocalVariables.DeviceId, crypto.randomUUID());
}
function authentication(config: any) {
  const token = localStorage.getItem('DemoToken');
  if (token) config.headers.set('Authorization', `Bearer ${token}`);
  config.headers.set('deviceId', localStorage.getItem(LocalVariables.DeviceId) || '');
  config.headers.set('correlationId', crypto.randomUUID());
  return config;
}
http.interceptors.request.use(authentication);
// Existing tracing components use the same identity and never invent caller IDs.
axios.interceptors.request.use(authentication);
export function describeError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const detail = error.response?.data;
    return typeof detail === 'string' && detail.trim() ? detail : detail?.detail || detail?.error || error.message;
  }
  return error instanceof Error ? error.message : 'The request failed.';
}
