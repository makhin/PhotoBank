import { photosUpload } from '../api/photobank';

export type UploadFile = { data: BlobPart | ArrayBuffer | Uint8Array; name: string };

export async function uploadPhotosAdapter(params: {
  files: UploadFile[];
  storageId: number;
  path: string;
}) {
  const { files, storageId, path } = params;
  const blobs = files.map(({ data, name }) => new File([data], name));
  await photosUpload({ files: blobs, storageId, path });
}
