import React, { useCallback, useEffect, useState } from 'react';
import clsx from 'clsx';

export type SmartImageProps = Omit<
  React.ImgHTMLAttributes<HTMLImageElement>,
  'src' | 'srcSet' | 'sizes' | 'loading' | 'decoding' | 'fetchPriority'
> & {
  alt: string;
  thumbSrc: string;
  src: string;
  srcSet?: string;
  sizes?: string;
  className?: string;
  fit?: 'cover' | 'contain';
  onLoadFull?: () => void;
  fetchPriority?: 'high' | 'auto' | 'low';
  decoding?: 'async' | 'auto' | 'sync';
  loading?: 'eager' | 'lazy';
  width?: number | string;
  height?: number | string;
};

const SmartImage = React.memo(
  React.forwardRef<HTMLImageElement, SmartImageProps>(
    (
      {
        alt,
        thumbSrc,
        src,
        srcSet,
        sizes,
        className,
        fit = 'cover',
        onLoadFull,
        fetchPriority = 'low',
        decoding = 'async',
        loading = 'lazy',
        width,
        height,
        ...imgProps
      },
      ref
    ) => {
      const [loaded, setLoaded] = useState(false);

      const handleLoad = useCallback(() => {
        requestAnimationFrame(() => {
          setLoaded(true);
          onLoadFull?.();
        });
      }, [onLoadFull]);

      useEffect(() => {
        setLoaded(false);
      }, [src, srcSet]);

      const fitClass = fit === 'contain' ? 'object-contain' : 'object-cover';

      return (
        <div className={clsx('relative overflow-hidden', className)}>
          <img
            src={thumbSrc}
            alt={alt}
            width={width}
            height={height}
            className={clsx(
              'absolute inset-0 w-full h-full transition-all duration-300',
              fitClass,
              loaded ? 'opacity-0 blur-none scale-100' : 'opacity-100 blur-md scale-105'
            )}
            aria-hidden={loaded}
          />
          <img
            ref={ref}
            src={src}
            srcSet={srcSet}
            sizes={sizes}
            alt={alt}
            width={width}
            height={height}
            loading={loading}
            decoding={decoding}
            fetchPriority={fetchPriority}
            onLoad={handleLoad}
            className={clsx(
              'w-full h-full transition-opacity duration-300',
              fitClass,
              loaded ? 'opacity-100' : 'opacity-0'
            )}
            {...imgProps}
          />
        </div>
      );
    }
  )
);

export default SmartImage;
