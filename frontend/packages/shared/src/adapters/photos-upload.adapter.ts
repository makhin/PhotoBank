import axios from 'axios';
import FormData from 'form-data';

import { OpenAPI } from '../generated';

type UploadFile = { buffer: Buffer; name: string };

export async function uploadPhotosAdapter(
  params: {
    files: UploadFile[];
    storageId: number;
    path: string;
  }
) {
  const { files, storageId, path } = params;

  const form = new FormData();
  for (const { buffer, name } of files) {
    form.append('files', buffer, name);
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
