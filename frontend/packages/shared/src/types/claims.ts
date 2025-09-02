import type { UserDto } from '../api/photobank';

export interface Claim {
  type?: string | null;
  value?: string | null;
}

export type UserWithClaims = UserDto & {
  claims?: Claim[];
};
