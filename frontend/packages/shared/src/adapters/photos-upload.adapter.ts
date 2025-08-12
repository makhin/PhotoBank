import { photosUpload } from '../api/photobank';

export type UploadFile = { buffer: Buffer; name: string };

export async function uploadPhotosAdapter(params: {
  files: UploadFile[];
  storageId: number;
  path: string;
}) {
  const { files, storageId, path } = params;
  const blobs = files.map(({ buffer, name }) => new File([buffer], name));
  await photosUpload({ files: blobs, storageId, path });
}
