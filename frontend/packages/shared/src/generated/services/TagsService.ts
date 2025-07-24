/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { TagDto } from '../models/TagDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TagsService {
    /**
     * @returns TagDto OK
     * @throws ApiError
     */
    public static getApiTags(): CancelablePromise<Array<TagDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/tags',
        });
    }
}
