import client from './client';
import type { Assignment, CreateAssignmentRequest } from '../types';

export const listAssignments = async (classId?: string): Promise<Assignment[]> => {
  const params = classId ? { classId } : {};
  const { data } = await client.get<any>('/api/v1/assignments', { params });
  return Array.isArray(data) ? data : (data?.items ?? []);
};

export const createAssignment = async (req: CreateAssignmentRequest): Promise<Assignment> => {
  const { data } = await client.post<Assignment>('/api/v1/assignments', req);
  return data;
};
