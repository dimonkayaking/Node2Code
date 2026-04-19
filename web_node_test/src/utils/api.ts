export type ApiUser = {
  id: number;
  name: string;
  lastName: string;
  email: string;
};

type ApiErrorPayload = {
  error?: string;
  fields?: Record<string, string>;
};

export class ApiError extends Error {
  status: number;
  fields?: Record<string, string>;

  constructor(message: string, status: number, fields?: Record<string, string>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fields = fields;
  }
}

const API_BASE_URL =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() ||
  (typeof window !== 'undefined' && window.location.hostname === 'localhost'
    ? 'http://localhost:5000/api'
    : '/api');

const request = async <TResponse>(path: string, init: RequestInit): Promise<TResponse> => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init.headers || {}),
    },
  });

  const data = (await response.json().catch(() => ({}))) as TResponse & ApiErrorPayload;

  if (!response.ok) {
    throw new ApiError(data.error || 'Request failed', response.status, data.fields);
  }

  return data;
};

export const registerRequest = (payload: {
  name: string;
  lastName: string;
  email: string;
  password: string;
}) =>
  request<{ message: string; user: ApiUser }>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  });

export const loginRequest = (payload: { email: string; password: string }) =>
  request<{ message: string; user: ApiUser }>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  });

export const getProgressRequest = (userId: number) =>
  request<{ userId: number; completedLessons: number[] }>(`/progress/${userId}`, {
    method: 'GET',
  });

export const updateProgressRequest = (payload: {
  userId: number;
  lessonId: number;
  completed: boolean;
}) =>
  request<{
    message: string;
    progress: {
      userId: number;
      lessonId: number;
      completed: boolean;
      completed_at: string | null;
    };
  }>('/progress', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
