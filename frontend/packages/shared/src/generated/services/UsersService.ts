/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ClaimDto } from '../models/ClaimDto';
import type { UpdateUserDto } from '../models/UpdateUserDto';
import type { UserWithClaimsDto } from '../models/UserWithClaimsDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class UsersService {
    /**
     * @returns UserWithClaimsDto OK
     * @throws ApiError
     */
    public static usersGetAll(): CancelablePromise<Array<UserWithClaimsDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/admin/users',
        });
    }
    /**
     * @param id
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static usersUpdate(
        id: string,
        requestBody?: UpdateUserDto,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/admin/users/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }
    /**
     * @param id
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static usersSetClaims(
        id: string,
        requestBody?: Array<ClaimDto>,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/admin/users/{id}/claims',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }
}
