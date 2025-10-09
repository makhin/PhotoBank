import { useEffect, useMemo, useState } from 'react';
import { getPlaceByGeoPoint, isValidGeoPoint, type GeoPointLike } from '@photobank/shared';
import type { GeoPointDto } from '@photobank/shared/api/photobank';

export function usePhotoGeodata(location: GeoPointLike) {
    const [placeName, setPlaceName] = useState('');

    const latitude = location?.latitude ?? null;
    const longitude = location?.longitude ?? null;

    const point = useMemo(() => {
        if (!isValidGeoPoint(location)) {
            return null;
        }

        return {
            latitude: location.latitude,
            longitude: location.longitude,
        } satisfies GeoPointDto;
    }, [latitude, longitude]);

    const hasValidLocation = point !== null;

    useEffect(() => {
        if (!point) {
            setPlaceName('');
            return;
        }

        let isCancelled = false;

        void (async () => {
            const name = await getPlaceByGeoPoint(point);

            if (!isCancelled) {
                setPlaceName(name);
            }
        })();

        return () => {
            isCancelled = true;
        };
    }, [point]);

    return { placeName, hasValidLocation } as const;
}
