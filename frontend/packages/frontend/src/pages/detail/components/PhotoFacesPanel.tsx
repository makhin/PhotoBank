import { useTranslation } from 'react-i18next';
import type { FaceDto } from '@photobank/shared/api/photobank';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Checkbox } from '@/shared/ui/checkbox';
import { Label } from '@/shared/ui/label';
import { FacePersonSelector } from '@/components/FacePersonSelector';

import type { RootState } from '@/app/store';
import type { PhotoDetails } from '../types';

interface PhotoFacesPanelProps {
    faces: PhotoDetails['faces'];
    showFaceBoxes: boolean;
    onShowFaceBoxesChange: (value: boolean) => void;
    onFacePersonChange: (face: FaceDto, personId: number | undefined) => void;
    persons: RootState['metadata']['persons'];
    isAdmin: boolean;
}

export const PhotoFacesPanel = ({
    faces,
    showFaceBoxes,
    onShowFaceBoxesChange,
    onFacePersonChange,
    persons,
    isAdmin,
}: PhotoFacesPanelProps) => {
    const { t } = useTranslation();

    if (!faces || faces.length === 0) {
        return null;
    }

    return (
        <Card className="bg-card border-border">
            <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                    <CardTitle className="text-lg">
                        {t('detectedFacesTitle')} ({faces.length})
                    </CardTitle>
                    <div className="flex items-center space-x-2">
                        <Checkbox
                            id="show-face-boxes"
                            checked={showFaceBoxes}
                            onCheckedChange={(value) => onShowFaceBoxesChange(!!value)}
                        />
                        <Label htmlFor="show-face-boxes" className="text-sm">
                            {t('showFaceBoxesLabel')}
                        </Label>
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                <p className="text-sm text-muted-foreground mb-3">{t('hoverFaceHint')}</p>
                <div className="space-y-2">
                    {faces.map((face, index) => {
                        if (face.id === undefined) return null;
                        return (
                            <FacePersonSelector
                                key={face.id}
                                faceIndex={index}
                                personId={face.personId ?? undefined}
                                persons={persons}
                                disabled={!showFaceBoxes || !isAdmin}
                                onChange={(personId) => onFacePersonChange(face, personId ?? undefined)}
                            />
                        );
                    })}
                </div>
            </CardContent>
        </Card>
    );
};
