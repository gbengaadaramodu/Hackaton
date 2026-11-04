import client from './client';
import type {
  SimulationSession,
  StartSessionRequest,
  CommandRequest,
  MessageRequest,
  ChatMessage,
} from '../types';

export const startSession = async (req: StartSessionRequest): Promise<SimulationSession> => {
  const { data } = await client.post<SimulationSession>('/api/v1/sessions', req);
  return data;
};

export const listSessions = async (lessonId?: string): Promise<SimulationSession[]> => {
  const params = lessonId ? { lessonId } : {};
  const { data } = await client.get<any>('/api/v1/sessions', { params });
  return Array.isArray(data) ? data : (data?.items ?? []);
};

export const getSession = async (id: string): Promise<SimulationSession> => {
  const { data } = await client.get<SimulationSession>(`/api/v1/sessions/${id}`);
  return data;
};

export const submitCommand = async (id: string, req: CommandRequest): Promise<SimulationSession> => {
  const { data } = await client.post<SimulationSession>(`/api/v1/sessions/${id}/commands`, req);
  return data;
};

export const sendMessage = async (id: string, req: MessageRequest): Promise<ChatMessage> => {
  const { data } = await client.post<ChatMessage>(`/api/v1/sessions/${id}/messages`, req);
  return data;
};

export const getMessages = async (id: string): Promise<ChatMessage[]> => {
  const { data } = await client.get<ChatMessage[]>(`/api/v1/sessions/${id}/messages`);
  return data;
};

export const changeStatus = async (id: string, status: string): Promise<SimulationSession> => {
  const { data } = await client.post<SimulationSession>(`/api/v1/sessions/${id}/status`, { status });
  return data;
};
