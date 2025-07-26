import type { UpdateFaceDto } from '../generated';
import { FacesService } from '../generated';

export const updateFace = async (dto: UpdateFaceDto): Promise<void> => {
  await FacesService.putApiFaces(dto);
};
