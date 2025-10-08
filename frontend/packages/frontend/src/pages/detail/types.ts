import * as PhotosApi from '@photobank/shared/api/photobank';

export type PhotoDetails = NonNullable<PhotosApi.photosGetPhotoResponse200['data']>;
