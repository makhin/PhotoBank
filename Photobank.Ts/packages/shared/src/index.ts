export function formatDate(date: string) {
  return new Date(date).toLocaleDateString("ru-RU");
}
