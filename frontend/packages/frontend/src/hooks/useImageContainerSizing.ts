import {
    type RefObject,
    useCallback,
    useEffect,
    useLayoutEffect,
    useMemo,
    useRef,
    useState,
} from 'react';

interface Size {
    width: number;
    height: number;
}

interface ImageSize extends Size {
    scale: number;
}

const defaultContainerSize: Size = { width: 0, height: 0 };
const defaultImageSize: ImageSize = { width: 0, height: 0, scale: 1 };

const calculateImageSize = (naturalWidth: number, naturalHeight: number, containerWidth: number, containerHeight: number) => {
    if (naturalWidth <= containerWidth && naturalHeight <= containerHeight) {
        return { width: naturalWidth, height: naturalHeight, scale: 1 };
    }

    const scaleByWidth = containerWidth / naturalWidth;
    const scaleByHeight = containerHeight / naturalHeight;
    const scale = Math.min(scaleByWidth, scaleByHeight);

    return {
        width: naturalWidth * scale,
        height: naturalHeight * scale,
        scale,
    };
};

type HTMLElementRef =
    | RefObject<HTMLElement | null>
    | { current: HTMLElement | null };

interface UseImageContainerSizingParams {
    containerRef: HTMLElementRef;
    imageNaturalSize: Size;
}

export const useImageContainerSizing = ({ containerRef, imageNaturalSize }: UseImageContainerSizingParams) => {
    const [containerSize, setContainerSize] = useState<Size>(defaultContainerSize);
    const [imageDisplaySize, setImageDisplaySize] = useState<ImageSize>(defaultImageSize);
    const [containerElement, setContainerElement] = useState<HTMLElement | null>(null);

    const containerSizeRef = useRef(containerSize);
    const imageDisplaySizeRef = useRef(imageDisplaySize);
    const imageNaturalSizeRef = useRef(imageNaturalSize);

    const updateSizes = useCallback(() => {
        const container = containerRef.current;

        if (!container) {
            return;
        }

        const containerRect = container.getBoundingClientRect();
        const nextContainerSize = {
            width: containerRect.width,
            height: containerRect.height,
        };

        if (
            nextContainerSize.width !== containerSizeRef.current.width ||
            nextContainerSize.height !== containerSizeRef.current.height
        ) {
            containerSizeRef.current = nextContainerSize;
            setContainerSize(nextContainerSize);
        }

        const { width: naturalWidth, height: naturalHeight } = imageNaturalSizeRef.current;
        const calculatedSize = calculateImageSize(
            naturalWidth,
            naturalHeight,
            nextContainerSize.width,
            nextContainerSize.height,
        );

        if (
            calculatedSize.width !== imageDisplaySizeRef.current.width ||
            calculatedSize.height !== imageDisplaySizeRef.current.height ||
            calculatedSize.scale !== imageDisplaySizeRef.current.scale
        ) {
            imageDisplaySizeRef.current = calculatedSize;
            setImageDisplaySize(calculatedSize);
        }
    }, [containerRef]);

    useEffect(() => {
        imageNaturalSizeRef.current = imageNaturalSize;
        updateSizes();
    }, [imageNaturalSize.height, imageNaturalSize.width, updateSizes]);

    useLayoutEffect(() => {
        if (containerRef.current !== containerElement) {
            setContainerElement(containerRef.current);
        }
    });

    useEffect(() => {
        const container = containerElement;
        const resizeObserver = container ? new ResizeObserver(updateSizes) : null;

        if (container && resizeObserver) {
            resizeObserver.observe(container);
        }

        window.addEventListener('resize', updateSizes);
        updateSizes();

        return () => {
            if (resizeObserver && container) {
                resizeObserver.unobserve(container);
                resizeObserver.disconnect();
            }

            window.removeEventListener('resize', updateSizes);
        };
    }, [containerElement, updateSizes]);

    return useMemo(
        () => ({
            containerSize,
            imageDisplaySize,
        }),
        [containerSize, imageDisplaySize],
    );
};
