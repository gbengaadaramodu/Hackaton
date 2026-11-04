import client from './client';

export const renderScene = async (modelKey, state, frame) => {
  const body = frame === undefined || frame === null
    ? { modelKey, state }
    : { modelKey, state, frame };
  const { data } = await client.post('/api/v1/simulations/render', body);
  return data;
};

export const renderImageScene = async (modelKey, state) => {
  const { data } = await client.post('/api/v1/simulations/render-image', { modelKey, state });
  return data;
};
