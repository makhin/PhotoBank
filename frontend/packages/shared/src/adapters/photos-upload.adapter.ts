import { photosUpload } from '../api/photobank';

export type UploadFileData = BlobPart | ArrayBuffer | Uint8Array;

export type UploadFile = { data: UploadFileData; name: string };

function normalizeToBlobPart(data: UploadFileData): BlobPart {
  if (data instanceof Uint8Array) {
    return data;
  }

  if (data instanceof ArrayBuffer) {
    return new Uint8Array(data);
  }

  return data;
}

export async function uploadPhotosAdapter(params: {
  files: UploadFile[];
  storageId: number;
  path: string;
}) {
  const { files, storageId, path } = params;
  const blobs = files.map(({ data, name }) => new File([normalizeToBlobPart(data)], name));
  return photosUpload({ files: blobs, storageId, path });
}
