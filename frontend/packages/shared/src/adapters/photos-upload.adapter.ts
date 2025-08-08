import axios from 'axios';
import FormData from 'form-data';

import { OpenAPI } from '../generated';
import type { ApiRequestOptions } from '../generated/core/ApiRequestOptions';

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

  let token: string | undefined;
  if (typeof OpenAPI.TOKEN === 'function') {
    const options: ApiRequestOptions = {
      method: 'POST',
      url: '/api/photos/upload',
    };
    token = await OpenAPI.TOKEN(options);
  } else {
    token = OpenAPI.TOKEN;
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return axios.post(`${OpenAPI.BASE}/api/photos/upload`, form, {
    headers,
  });
}
