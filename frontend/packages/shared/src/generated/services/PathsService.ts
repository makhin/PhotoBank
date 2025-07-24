/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PathDto } from '../models/PathDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PathsService {
    /**
     * @returns PathDto OK
     * @throws ApiError
     */
    public static getApiPaths(): CancelablePromise<Array<PathDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/paths',
        });
    }
}
