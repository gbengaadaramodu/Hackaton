import client from './client';
import type { Lesson } from '../types';

export const listLessons = async (params?: Record<string, string>): Promise<Lesson[]> => {
  const { data } = await client.get<any>('/api/v1/lessons', { params });
  return Array.isArray(data) ? data : (data?.items ?? []);
};

export const getLesson = async (id: string): Promise<Lesson> => {
  const { data } = await client.get<Lesson>(`/api/v1/lessons/${id}`);
  return data;
};
