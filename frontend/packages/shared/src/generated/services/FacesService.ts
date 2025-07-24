/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { UpdateFaceDto } from '../models/UpdateFaceDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class FacesService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static putApiFaces(
        requestBody?: UpdateFaceDto,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/faces',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
