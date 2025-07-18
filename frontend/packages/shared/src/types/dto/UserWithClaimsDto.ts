export interface UserWithClaimsDto {
  id: string;
  email: string;
  phoneNumber?: string;
  telegram?: string;
  claims: { type: string; value: string }[];
}
