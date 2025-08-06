import axios from 'axios';
import FormData from 'form-data';

import { OpenAPI } from '../generated';

export async function uploadPhotosAdapter(
  params: {
    files: File[];
    storageId: number;
    path: string;
  }
) {
  const { files, storageId, path } = params;

  const form = new FormData();
  for (const file of files) {
    form.append('files', file, file.name);
  }
  form.append('storageId', storageId.toString());
  form.append('path', path);

  const headers: Record<string, string> = {
    ...form.getHeaders(),
  };

  if (OpenAPI.TOKEN) {
    headers['Authorization'] = `Bearer ${String(OpenAPI.TOKEN)}`;
  }

  return axios.post(`${OpenAPI.BASE}/api/photos/upload`, form, {
    headers,
  });
}
