import type { PathDto, PersonDto, StorageDto, TagDto } from './photoBankApi.schemas';

export type ReferenceDataFetchers = {
  fetchTags: () => Promise<TagDto[]>;
  fetchPersons: () => Promise<PersonDto[]>;
  fetchStorages: () => Promise<StorageDto[]>;
  fetchPaths: () => Promise<PathDto[]>;
};

export type ReferenceData = {
  tags: TagDto[];
  persons: PersonDto[];
  storages: StorageDto[];
  paths: PathDto[];
};

export async function fetchReferenceData({
  fetchTags,
  fetchPersons,
  fetchStorages,
  fetchPaths,
}: ReferenceDataFetchers): Promise<ReferenceData> {
  const [tags, persons, storages, paths] = await Promise.all([
    fetchTags(),
    fetchPersons(),
    fetchStorages(),
    fetchPaths(),
  ]);

  return { tags, persons, storages, paths };
}
