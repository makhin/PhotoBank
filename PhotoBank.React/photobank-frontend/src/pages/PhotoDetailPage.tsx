import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { fetchPhoto, fetchPersons } from '../services/photoApi';
import { Badge } from '@/components/ui/badge';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type {PersonDto, PhotoDto} from "@/types/api";

export default function PhotoDetailPage() {
    const { id } = useParams();
    const [photo, setPhoto] = useState<PhotoDto | null>(null);
    const [persons, setPersons] = useState<PersonDto[]>([]);

    useEffect(() => {
        if (!id) return;
        fetchPhoto(id).then(setPhoto);
        fetchPersons().then(setPersons);
    }, [id]);

    const handlePersonChange = (faceIndex: number, personId: number) => {
        if (!photo) return;
        const updatedFaces = [...(photo.faces || [])];
        updatedFaces[faceIndex] = {
            ...updatedFaces[faceIndex],
            personId: personId === -1 ? undefined : personId,
        };
        setPhoto({ ...photo, faces: updatedFaces });
    };

    if (!photo) return <p className="p-4">Загрузка...</p>;

    return (
        <TooltipProvider>
            <div className="p-6 space-y-4">
                <div className="relative w-full max-w-3xl mx-auto">
                    {photo.previewImage && (
                        <img
                            src={`data:image/jpeg;base64,${photo.previewImage}`}
                            alt={photo.name ?? 'photo'}
                            className="w-full rounded shadow"
                        />
                    )}

                    {photo.faces?.map((face, index) => {
                        const box = face.faceBox;
                        if (!box) return null;

                        const lines = [
                            face.personId ? `ID ${face.personId}` : `#${index + 1}`,
                            face.gender !== undefined ? (face.gender ? 'Мужчина' : 'Женщина') : '',
                            face.age !== undefined ? `Возраст: ${face.age.toFixed(1)}` : '',
                            face.friendlyFaceAttributes ?? face.faceAttributes ?? ''
                        ].filter(Boolean);

                        return (
                            <Tooltip key={index}>
                                <TooltipTrigger asChild>
                                    <div
                                        className="absolute border-2 border-red-500 text-white text-xs bg-red-500/80 px-1"
                                        style={{
                                            top: box.top,
                                            left: box.left,
                                            width: box.width,
                                            height: box.height,
                                        }}
                                    >
                                        {face.personId ?? index + 1}
                                    </div>
                                </TooltipTrigger>
                                <TooltipContent>
                                    <div className="whitespace-pre-line text-sm">
                                        {lines.join('\n')}
                                    </div>
                                </TooltipContent>
                            </Tooltip>
                        );
                    })}
                </div>

                {Array.isArray(photo.faces) && photo.faces.length > 0 && (
                    <div className="space-y-4">
                        <h2 className="font-semibold">Связать лица с людьми:</h2>
                        {photo.faces.map((face, index) => (
                            <div key={index} className="flex items-center gap-4">
                                <span className="text-sm w-10">#{index + 1}</span>
                                <Select
                                    value={face.personId?.toString() ?? '-1'}
                                    onValueChange={(value) => handlePersonChange(index, parseInt(value))}
                                >
                                    <SelectTrigger className="w-64">
                                        <SelectValue placeholder="Не выбрано" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="-1">Не выбрано</SelectItem>
                                        {persons.map(p => (
                                            <SelectItem key={p.id} value={p.id.toString()}>{p.name ?? `ID ${p.id}`}</SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                        ))}
                    </div>
                )}

                <div className="text-xl font-bold">{photo.name}</div>
                <div className="text-sm text-muted-foreground">Дата: {photo.takenDate}</div>
                <div className="text-sm text-muted-foreground">Размер: {photo.width}×{photo.height}</div>

                {Array.isArray(photo.tags) && photo.tags.length > 0 && (
                    <div>
                        <div className="font-semibold mb-1">Теги:</div>
                        <div className="flex flex-wrap gap-2">
                            {photo.tags.map((t, i) => <Badge key={i}>{t}</Badge>)}
                        </div>
                    </div>
                )}
            </div>
        </TooltipProvider>
    );
}
