import client from './client';
import type { Assessment, AssessmentAttempt, SubmitAttemptRequest } from '../types';

export const listAssessments = async (lessonId?: string): Promise<Assessment[]> => {
  const params = lessonId ? { lessonId } : {};
  const { data } = await client.get<any>('/api/v1/assessments', { params });
  return Array.isArray(data) ? data : (data?.items ?? []);
};

export const getAssessment = async (id: string): Promise<Assessment> => {
  const { data } = await client.get<Assessment>(`/api/v1/assessments/${id}`);
  return data;
};

export const submitAttempt = async (id: string, req: SubmitAttemptRequest): Promise<AssessmentAttempt> => {
  const { data } = await client.post<AssessmentAttempt>(`/api/v1/assessments/${id}/attempts`, req);
  return data;
};

export const listMyAttempts = async (): Promise<AssessmentAttempt[]> => {
  const { data } = await client.get<any>('/api/v1/assessments/attempts/mine');
  return Array.isArray(data) ? data : (data?.items ?? []);
};
