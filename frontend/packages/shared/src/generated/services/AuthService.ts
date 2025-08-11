/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ClaimDto } from '../models/ClaimDto';
import type { LoginRequestDto } from '../models/LoginRequestDto';
import type { LoginResponseDto } from '../models/LoginResponseDto';
import type { RegisterRequestDto } from '../models/RegisterRequestDto';
import type { RoleDto } from '../models/RoleDto';
import type { UpdateUserDto } from '../models/UpdateUserDto';
import type { UserDto } from '../models/UserDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthService {
    /**
     * @param requestBody
     * @returns LoginResponseDto OK
     * @throws ApiError
     */
    public static authLogin(
        requestBody?: LoginRequestDto,
    ): CancelablePromise<LoginResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/login',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static authRegister(
        requestBody?: RegisterRequestDto,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/auth/register',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @returns UserDto OK
     * @throws ApiError
     */
    public static authGetUser(): CancelablePromise<UserDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/auth/user',
        });
    }
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static authUpdateUser(
        requestBody?: UpdateUserDto,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/auth/user',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @returns ClaimDto OK
     * @throws ApiError
     */
    public static authGetUserClaims(): CancelablePromise<Array<ClaimDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/auth/claims',
        });
    }
    /**
     * @returns RoleDto OK
     * @throws ApiError
     */
    public static authGetUserRoles(): CancelablePromise<Array<RoleDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/auth/roles',
        });
    }
}
