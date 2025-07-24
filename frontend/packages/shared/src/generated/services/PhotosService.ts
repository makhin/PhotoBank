/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { FilterDto } from '../models/FilterDto';
import type { PhotoDto } from '../models/PhotoDto';
import type { QueryResult } from '../models/QueryResult';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PhotosService {
    /**
     * @param requestBody
     * @returns QueryResult OK
     * @throws ApiError
     */
    public static postApiPhotosSearch(
        requestBody?: FilterDto,
    ): CancelablePromise<QueryResult> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/photos/search',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @param id
     * @returns PhotoDto OK
     * @throws ApiError
     */
    public static getApiPhotos(
        id: number,
    ): CancelablePromise<PhotoDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/photos/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `Not Found`,
            },
        });
    }
}
