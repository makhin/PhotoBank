import type { ClaimDto } from './ClaimDto';

export interface RoleDto {
  name: string;
  claims: ClaimDto[];
}
