import axios, { AxiosRequestConfig } from 'axios';

export const photobankAxios = <T = unknown>(config: AxiosRequestConfig) => {
  return axios<T>(config);
};
