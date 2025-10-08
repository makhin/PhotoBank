import { useEffect, useMemo, useState } from 'react';
import { getPlaceByGeoPoint } from '@photobank/shared';
import type { GeoPointDto } from '@photobank/shared/api/photobank';

type GeoPointLike = Pick<GeoPointDto, 'latitude' | 'longitude'>;

export function usePhotoGeodata(location: GeoPointLike | null | undefined) {
    const [placeName, setPlaceName] = useState('');

    const latitude = location?.latitude ?? null;
    const longitude = location?.longitude ?? null;

    const hasValidLocation = useMemo(() => {
        if (latitude == null || longitude == null) {
            return false;
        }

        return !(latitude === 0 && longitude === 0);
    }, [latitude, longitude]);

    useEffect(() => {
        if (!hasValidLocation || latitude == null || longitude == null) {
            setPlaceName('');
            return;
        }

        let isCancelled = false;

        void (async () => {
            const name = await getPlaceByGeoPoint({
                latitude,
                longitude,
            });

            if (!isCancelled) {
                setPlaceName(name);
            }
        })();

        return () => {
            isCancelled = true;
        };
    }, [hasValidLocation, latitude, longitude]);

    return { placeName, hasValidLocation } as const;
}
