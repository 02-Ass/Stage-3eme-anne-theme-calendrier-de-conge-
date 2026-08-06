import type {
  Employee,
  EmployeeUpsertRequest,
  Me,
  Project,
  Vacation,
  VacationUpsertRequest,
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api';

type TokenGetter = () => string | undefined;

let getToken: TokenGetter | null = null;

/** Вызывается один раз из App, чтобы прокинуть access_token из react-oidc-context в API-клиент. */
export function registerTokenGetter(fn: TokenGetter): void {
  getToken = fn;
}

function authHeaders(): HeadersInit {
  const token = getToken?.();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...authHeaders(),
    ...options.headers,
  };

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });

  if (!response.ok) {
    const message = await response.text().catch(() => response.statusText);
    throw new Error(message || `Ошибка запроса: ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** Отдельно от request(), так как ответ — файл (Blob), а не JSON. */
async function requestBlob(path: string): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}${path}`, { headers: authHeaders() });
  if (!response.ok) {
    const message = await response.text().catch(() => response.statusText);
    throw new Error(message || `Ошибка запроса: ${response.status}`);
  }
  return response.blob();
}

export const api = {
  me: {
    get: () => request<Me>('/me'),
  },
  employees: {
    list: () => request<Employee[]>('/employees'),
    create: (body: EmployeeUpsertRequest) =>
      request<Employee>('/employees', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: EmployeeUpsertRequest) =>
      request<Employee>(`/employees/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    remove: (id: number) => request<void>(`/employees/${id}`, { method: 'DELETE' }),
  },
  vacations: {
    list: (year?: number) => request<Vacation[]>(`/vacations${year ? `?year=${year}` : ''}`),
    upcoming: (daysAhead: number) => request<Vacation[]>(`/vacations/upcoming?daysAhead=${daysAhead}`),
    exportCsv: (year: number) => requestBlob(`/vacations/export?year=${year}`),
    create: (body: VacationUpsertRequest) =>
      request<Vacation>('/vacations', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: VacationUpsertRequest) =>
      request<Vacation>(`/vacations/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    remove: (id: number) => request<void>(`/vacations/${id}`, { method: 'DELETE' }),
  },
  projects: {
    list: () => request<Project[]>('/projects'),
  },
};
