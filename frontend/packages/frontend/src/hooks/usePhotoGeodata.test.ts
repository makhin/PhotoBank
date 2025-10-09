import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { getPlaceByGeoPoint } from '@photobank/shared';

import { usePhotoGeodata } from './usePhotoGeodata';

vi.mock('@photobank/shared', () => ({
    getPlaceByGeoPoint: vi.fn(),
}));

const mockedGetPlaceByGeoPoint = vi.mocked(getPlaceByGeoPoint);

describe('usePhotoGeodata', () => {
    beforeEach(() => {
        mockedGetPlaceByGeoPoint.mockReset();
    });

    it('returns defaults when location is missing', () => {
        const { result } = renderHook(() => usePhotoGeodata(undefined));

        expect(result.current.hasValidLocation).toBe(false);
        expect(result.current.placeName).toBe('');
        expect(mockedGetPlaceByGeoPoint).not.toHaveBeenCalled();
    });

    it('resolves place name for valid coordinates', async () => {
        mockedGetPlaceByGeoPoint.mockResolvedValue('Test Place');

        const { result } = renderHook(() =>
            usePhotoGeodata({ latitude: 12.34, longitude: 56.78 }),
        );

        await waitFor(() => {
            expect(result.current.placeName).toBe('Test Place');
        });

        expect(result.current.hasValidLocation).toBe(true);
        expect(mockedGetPlaceByGeoPoint).toHaveBeenCalledWith({
            latitude: 12.34,
            longitude: 56.78,
        });
    });

    it('clears place name when location becomes invalid', async () => {
        mockedGetPlaceByGeoPoint.mockResolvedValue('Initial Place');

        type HookProps = { location: { latitude: number; longitude: number } | null };

        const initialProps: HookProps = { location: { latitude: 1, longitude: 2 } };

        const { result, rerender } = renderHook(
            ({ location }: HookProps) => usePhotoGeodata(location),
            {
                initialProps,
            },
        );

        await waitFor(() => {
            expect(result.current.placeName).toBe('Initial Place');
        });

        mockedGetPlaceByGeoPoint.mockClear();
        rerender({ location: null });

        await waitFor(() => {
            expect(result.current.placeName).toBe('');
            expect(result.current.hasValidLocation).toBe(false);
        });

        expect(mockedGetPlaceByGeoPoint).not.toHaveBeenCalled();
    });
});
