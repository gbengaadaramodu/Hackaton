import client from './client';
import type { CatalogResponse } from '../types';

export const getCatalog = async (): Promise<CatalogResponse> => {
  const { data } = await client.get<CatalogResponse>('/api/v1/catalog');
  return data;
};
