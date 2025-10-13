import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest';
import { useCallback, useMemo, useRef, useState } from 'react';
import type { FaceBoxDto } from '@photobank/shared/api/photobank';

import { I18nProvider } from '@/app/providers/I18nProvider';
import { useImageContainerSizing } from '@/hooks/useImageContainerSizing';
import { PhotoViewer } from '@/pages/detail/components/PhotoViewer';
import type { PhotoDetails } from '@/pages/detail/types';

const ContainerHarness = ({ photo }: { photo: PhotoDetails }) => {
    const containerRef = useRef<HTMLDivElement | null>(null);
    const [measuredSize, setMeasuredSize] = useState<{ width: number; height: number } | null>(null);

    const imageNaturalSize = useMemo(() => {
        if (photo.width && photo.height && photo.scale) {
            return { width: photo.width * photo.scale, height: photo.height * photo.scale };
        }

        return { width: 0, height: 0 };
    }, [photo.height, photo.scale, photo.width]);

    const { containerSize, imageDisplaySize } = useImageContainerSizing({
        containerRef,
        imageNaturalSize,
        imageMeasuredSize: measuredSize,
    });

    const calculateFacePosition = useCallback(
        (faceBox: FaceBoxDto) => {
            if (!imageDisplaySize.width || !imageDisplaySize.height) {
                return { display: 'none' };
            }

            const scale = imageDisplaySize.scale;
            const offsetLeft = (containerSize.width - imageDisplaySize.width) / 2;
            const offsetTop = (containerSize.height - imageDisplaySize.height) / 2;

            const left = faceBox.left * scale + offsetLeft;
            const top = faceBox.top * scale + offsetTop;
            const width = faceBox.width * scale;
            const height = faceBox.height * scale;

            return {
                left: `${left}px`,
                top: `${top}px`,
                width: `${width}px`,
                height: `${height}px`,
            };
        },
        [containerSize.height, containerSize.width, imageDisplaySize.height, imageDisplaySize.scale, imageDisplaySize.width],
    );

    return (
        <PhotoViewer
            photo={photo}
            containerRef={containerRef}
            showFaceBoxes
            calculateFacePosition={calculateFacePosition}
            onOpenViewer={() => {}}
            onImageLoad={(size) => setMeasuredSize(size)}
        />
    );
};

describe('PhotoDetailsPage orientation handling', () => {
    const originalResizeObserver = globalThis.ResizeObserver;
    let getBoundingClientRectSpy: ReturnType<typeof vi.spyOn>;

    beforeEach(() => {
        class ResizeObserverMock {
            private readonly callback: ResizeObserverCallback;

            constructor(callback: ResizeObserverCallback) {
                this.callback = callback;
            }

            observe() {
                this.callback([], this as unknown as ResizeObserver);
            }

            unobserve() {}

            disconnect() {}
        }

        globalThis.ResizeObserver = ResizeObserverMock as unknown as typeof ResizeObserver;

        getBoundingClientRectSpy = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => ({
            width: 300,
            height: 200,
            top: 0,
            left: 0,
            bottom: 200,
            right: 300,
            x: 0,
            y: 0,
            toJSON() {
                return {};
            },
        }));
    });

    afterEach(() => {
        getBoundingClientRectSpy.mockRestore();

        if (originalResizeObserver) {
            globalThis.ResizeObserver = originalResizeObserver;
        } else {
            delete (globalThis as { ResizeObserver?: typeof ResizeObserver }).ResizeObserver;
        }
    });

    it('realigns face overlays after on-load dimensions override rotated metadata guesses', async () => {
        const photo: PhotoDetails = {
            id: 101,
            name: 'Rotate 270° sample',
            previewUrl: '/rotate-270.jpg',
            scale: 1,
            width: 4000,
            height: 3000,
            orientation: 8,
            faces: [
                {
                    id: 1,
                    faceBox: {
                        left: 300,
                        top: 600,
                        width: 400,
                        height: 800,
                    },
                },
            ],
        };

        render(
            <I18nProvider>
                <ContainerHarness photo={photo} />
            </I18nProvider>,
        );

        const image = screen.getByRole('img', { name: 'Rotate 270° sample' });

        Object.defineProperty(image, 'naturalWidth', { configurable: true, value: 3000 });
        Object.defineProperty(image, 'naturalHeight', { configurable: true, value: 4000 });

        fireEvent.load(image);

        const overlayLabel = await screen.findByText('1');
        const overlay = overlayLabel.parentElement as HTMLElement;

        await waitFor(() => {
            expect(overlay.style.display).not.toBe('none');
            expect(Number.parseFloat(overlay.style.width)).toBeCloseTo(20);
            expect(Number.parseFloat(overlay.style.height)).toBeCloseTo(40);
            expect(Number.parseFloat(overlay.style.left)).toBeCloseTo(90);
            expect(Number.parseFloat(overlay.style.top)).toBeCloseTo(30);
        });
    });
});
