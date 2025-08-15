export const pushPhotoId = (id: number) => {
  const url = new URL(window.location.href);
  url.searchParams.set('photoId', String(id));
  window.history.pushState({}, '', `${url.pathname}${url.search}`);
};

export const readPhotoId = (search: string): number | null => {
  const params = new URLSearchParams(search);
  const id = params.get('photoId');
  return id ? Number(id) : null;
};

export const clearPhotoId = () => {
  const url = new URL(window.location.href);
  url.searchParams.delete('photoId');
  window.history.replaceState({}, '', `${url.pathname}${url.search}`);
};
