import { useTranslation } from 'react-i18next';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';

import type { PhotoDetails } from '../types';

interface PhotoGeodataPanelProps {
    photo: PhotoDetails;
    placeName: string;
    hasValidLocation: boolean;
}

export const PhotoGeodataPanel = ({ photo, placeName, hasValidLocation }: PhotoGeodataPanelProps) => {
    const { t } = useTranslation();
    const location = photo.location;

    if (!hasValidLocation || !location || !placeName) {
        return null;
    }

    return (
        <Card className="bg-card border-border">
            <CardHeader className="pb-3">
                <CardTitle className="text-lg">{t('locationLabel')}</CardTitle>
            </CardHeader>
            <CardContent>
                <a
                    href={`https://www.google.com/maps?q=${location.latitude},${location.longitude}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-1 block text-primary underline"
                >
                    {t('openInMapsText')}: {placeName}
                </a>
            </CardContent>
        </Card>
    );
};
