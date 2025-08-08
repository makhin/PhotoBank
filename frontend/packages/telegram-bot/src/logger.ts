export const logger = {
  info: (...args: unknown[]) => console.log(new Date().toISOString(), ...args),
  error: (...args: unknown[]) => console.error(new Date().toISOString(), ...args),
};
