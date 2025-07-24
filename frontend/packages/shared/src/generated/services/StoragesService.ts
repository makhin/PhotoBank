/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { StorageDto } from '../models/StorageDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class StoragesService {
    /**
     * @returns StorageDto OK
     * @throws ApiError
     */
    public static getApiStorages(): CancelablePromise<Array<StorageDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/storages',
        });
    }
}
