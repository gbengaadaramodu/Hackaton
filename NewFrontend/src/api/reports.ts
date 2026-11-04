import client from './client';
import type { StudentReport } from '../types';

export const getStudentReport = async (id: string): Promise<StudentReport> => {
  const { data } = await client.get<StudentReport>(`/api/v1/reports/students/${id}`);
  return data;
};
