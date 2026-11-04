import client from './client';

export const renderScene = async (modelKey, state) => {
  const { data } = await client.post('/api/v1/simulations/render', { modelKey, state });
  return data;
};
