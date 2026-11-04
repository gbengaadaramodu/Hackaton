import client from './client';
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from '../types';

export const login = async (req: LoginRequest): Promise<AuthResponse> => {
  const { data } = await client.post<AuthResponse>('/api/v1/auth/login', req);
  return data;
};

export const register = async (req: RegisterRequest): Promise<AuthResponse> => {
  const { data } = await client.post<AuthResponse>('/api/v1/auth/register', req);
  return data;
};

export const getMe = async (): Promise<UserProfile> => {
  const { data } = await client.get<UserProfile>('/api/v1/auth/me');
  return data;
};
