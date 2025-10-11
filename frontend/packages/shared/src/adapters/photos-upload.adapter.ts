import { photosUpload } from '../api/photobank';

export type UploadFileData = BlobPart | ArrayBuffer | Uint8Array;

export type UploadFile = { data: UploadFileData; name: string };

function normalizeToBlobPart(data: UploadFileData): BlobPart {
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

export async function uploadPhotosAdapter(params: {
  files: UploadFile[];
  storageId: number;
  path: string;
}) {
  const { files, storageId, path } = params;
  const blobs = files.map(({ data, name }) => new File([normalizeToBlobPart(data)], name));
  return photosUpload({ files: blobs, storageId, path });
}
