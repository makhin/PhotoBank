import type { Context } from 'grammy';
import { type UploadFile } from '@photobank/shared';
import {
  photosGetPhoto,
  photosSearchPhotos,
  photosUpload,
} from '../api/photobank/photos/photos';
import { ProblemDetailsError } from '@photobank/shared/types/problem';
import type {
  FilterDto,
  PhotoDto,
  PhotoItemDtoPageResponse,
  ProblemDetails as ApiProblemDetails,
} from '@photobank/shared/api/photobank';

import { callWithContext } from './call-with-context';

function normalizeToBlobPart(data: UploadFile['data']): BlobPart {
  if (ArrayBuffer.isView(data)) {
    const { buffer, byteOffset, byteLength } = data;
    if (buffer instanceof ArrayBuffer) {
      return buffer.slice(byteOffset, byteOffset + byteLength);
    }

    const copy = new Uint8Array(byteLength);
    copy.set(new Uint8Array(buffer, byteOffset, byteLength));
    return copy.buffer;
  }

  if (data instanceof ArrayBuffer) {
    return data;
  }

  return data;
}

type ProblemPayload = ConstructorParameters<typeof ProblemDetailsError>[0];

function toProblemDetails(problem: ApiProblemDetails) {
  const { type, title, status, detail, instance, ...extensions } = problem;
  const result: ProblemPayload = {
    ...extensions,
    title: title ?? 'Unknown error',
    status: status ?? 500,
  };

  if (type != null) {
    result.type = type;
  }
  if (detail != null) {
    result.detail = detail;
  }
  if (instance != null) {
    result.instance = instance;
  }

  return result;
}

export async function searchPhotos(
  ctx: Context,
  filter: FilterDto,
): Promise<PhotoItemDtoPageResponse> {
  const response = await callWithContext(ctx, () => photosSearchPhotos(filter));
  if (response.status !== 200) {
    throw new ProblemDetailsError(toProblemDetails(response.data));
  }
  return response.data;
}

export async function getPhoto(
  ctx: Context,
  id: number,
): Promise<PhotoDto> {
  const response = await callWithContext(ctx, () => photosGetPhoto(id));
  if (response.status !== 200) {
    throw new ProblemDetailsError(toProblemDetails(response.data));
  }
  return response.data;
}

export async function uploadPhotos(
  ctx: Context,
  options: { files: UploadFile[]; storageId: number; path: string },
) {
  const { files, storageId, path } = options;
  const normalizedFiles = files.map(({ data, name }) => {
    const blob = normalizeToBlobPart(data);
    return new File([blob], name);
  });

  await callWithContext(ctx, () => photosUpload({ files: normalizedFiles, storageId, path }));
}
