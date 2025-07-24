/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PersonDto } from '../models/PersonDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PersonsService {
    /**
     * @returns PersonDto OK
     * @throws ApiError
     */
    public static getApiPersons(): CancelablePromise<Array<PersonDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/persons',
        });
    }
}
