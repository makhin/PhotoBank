/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ClaimDto } from './ClaimDto';
export type UserWithClaimsDto = {
    id: string | null;
    email: string | null;
    phoneNumber?: string | null;
    telegram?: string | null;
    claims?: Array<ClaimDto> | null;
};

