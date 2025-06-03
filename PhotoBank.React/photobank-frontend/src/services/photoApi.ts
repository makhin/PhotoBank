import type {
    FilterDto,
    QueryResult,
    PhotoDto,
    TagDto,
    PersonDto,
    StorageDto,
    PathDto,
} from "@/types/api";
import { api } from "./axiosInstance";


export async function fetchPhotos(filter: FilterDto): Promise<QueryResult> {
    const res = await api.post<QueryResult>("/api/Photos/GetPhotos", filter);
    return res.data;
}

export async function fetchPhoto(id: string): Promise<PhotoDto> {
    const res = await api.get<PhotoDto>(`/api/Photos/GetPhoto?id=${id}`);
    return res.data;
}

export async function fetchTags(): Promise<TagDto[]> {
    const res = await api.get<TagDto[]>("/api/Photos/GetTags");
    return res.data;
}

export async function fetchPersons(): Promise<PersonDto[]> {
    const res = await api.get<PersonDto[]>("/api/Photos/GetPersons");
    return res.data;
}

export async function fetchStorages(): Promise<StorageDto[]> {
    const res = await api.get<StorageDto[]>("/api/Photos/GetStorages");
    return res.data;
}

export async function fetchPaths(): Promise<PathDto[]> {
    const res = await api.get<PathDto[]>("/api/Photos/GetPaths");
    return res.data;
}
