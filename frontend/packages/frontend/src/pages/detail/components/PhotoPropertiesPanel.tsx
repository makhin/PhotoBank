import { useTranslation } from 'react-i18next';
import { getOrientation } from '@photobank/shared';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Label } from '@/shared/ui/label';
import { Input } from '@/shared/ui/input';
import { Badge } from '@/shared/ui/badge';
import { Textarea } from '@/shared/ui/textarea';
import { ScoreBar } from '@/components/ScoreBar';

import type { PhotoDetails } from '../types';

interface PhotoPropertiesPanelProps {
    photo: PhotoDetails;
    formattedTakenDate: string;
}

export const PhotoPropertiesPanel = ({ photo, formattedTakenDate }: PhotoPropertiesPanelProps) => {
    const { t } = useTranslation();

    return (
        <div className="space-y-4">
            <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                    <CardTitle className="text-lg">{t('photoPropertiesTitle')}</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="grid gap-3 sm:grid-cols-2">
                        <div className="sm:col-span-2">
                            <Label className="text-muted-foreground text-xs">{t('nameLabel')}</Label>
                            <Input value={photo.name ?? ''} readOnly className="mt-1 bg-muted" />
                        </div>

                        {photo.id != null && (
                            <div>
                                <Label className="text-muted-foreground text-xs">{t('idLabel')}</Label>
                                <Input value={photo.id.toString()} readOnly className="mt-1 bg-muted" />
                            </div>
                        )}

                        <div>
                            <Label className="text-muted-foreground text-xs">{t('takenDateLabel')}</Label>
                            <Input value={formattedTakenDate} readOnly className="mt-1 bg-muted" />
                        </div>

                        {photo.width != null && (
                            <div>
                                <Label className="text-muted-foreground text-xs">{t('widthLabel')}</Label>
                                <Input value={`${photo.width.toString()}px`} readOnly className="mt-1 bg-muted" />
                            </div>
                        )}
                        {photo.height != null && (
                            <div>
                                <Label className="text-muted-foreground text-xs">{t('heightLabel')}</Label>
                                <Input value={`${photo.height.toString()}px`} readOnly className="mt-1 bg-muted" />
                            </div>
                        )}

                        {photo.scale != null && (
                            <div>
                                <Label className="text-muted-foreground text-xs">{t('scaleLabel')}</Label>
                                <Input value={photo.scale.toString()} readOnly className="mt-1 bg-muted" />
                            </div>
                        )}

                        {photo.orientation != null && (
                            <div>
                                <Label className="text-muted-foreground text-xs">{t('orientationLabel')}</Label>
                                <Input value={getOrientation(photo.orientation)} readOnly className="mt-1 bg-muted" />
                            </div>
                        )}
                    </div>
                </CardContent>
            </Card>

            {photo.tags && photo.tags.length > 0 && (
                <Card className="bg-card border-border">
                    <CardHeader className="pb-3">
                        <CardTitle className="text-lg">{t('tagsTitle')}</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="flex flex-wrap gap-2">
                            {photo.tags.map((tag) => (
                                <Badge key={tag} variant="secondary" className="bg-secondary text-secondary-foreground">
                                    {tag}
                                </Badge>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {photo.captions && photo.captions.length > 0 && (
                <Card className="bg-card border-border">
                    <CardHeader className="pb-3">
                        <CardTitle className="text-lg">{t('captionsTitle')}</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-2">
                            {photo.captions.map((caption) => (
                                <Textarea key={caption} value={caption} readOnly className="min-h-[60px] resize-none bg-muted" />
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                    <CardTitle className="text-lg">{t('contentAnalysisTitle')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                    <ScoreBar label={t('adultScoreLabel')} score={photo.adultScore ?? 0} colorClass="bg-orange-500" />
                    <ScoreBar label={t('racyScoreLabel')} score={photo.racyScore ?? 0} colorClass="bg-red-500" />
                </CardContent>
            </Card>
        </div>
    );
};
