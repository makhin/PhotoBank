import axios from 'axios';
import {API_BASE_URL} from "@photobank/shared/config";

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api/`,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});
