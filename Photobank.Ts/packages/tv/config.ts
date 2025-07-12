export const API_EMAIL: string = process.env.API_EMAIL || '';
export const API_PASSWORD: string = process.env.API_PASSWORD || '';
if (!API_EMAIL || !API_PASSWORD) {
    throw new Error('API_EMAIL or API_PASSWORD is not defined');
}
