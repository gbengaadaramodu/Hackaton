import client from './client';
import type { ClassInfo, ClassMember, JoinClassRequest, CreateClassRequest } from '../types';

export const listClasses = async (): Promise<ClassInfo[]> => {
  const { data } = await client.get<ClassInfo[]>('/api/v1/classes');
  return data;
};

export const getClass = async (id: string): Promise<ClassInfo> => {
  const { data } = await client.get<ClassInfo>(`/api/v1/classes/${id}`);
  return data;
};

export const getClassMembers = async (id: string): Promise<ClassMember[]> => {
  const { data } = await client.get<ClassMember[]>(`/api/v1/classes/${id}/members`);
  return data;
};

export const joinClass = async (req: JoinClassRequest): Promise<ClassInfo> => {
  const { data } = await client.post<ClassInfo>('/api/v1/classes/join', req);
  return data;
};

export const createClass = async (req: CreateClassRequest): Promise<ClassInfo> => {
  const { data } = await client.post<ClassInfo>('/api/v1/classes', req);
  return data;
};
