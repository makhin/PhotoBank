import { useRef, useState, useEffect } from 'react';

import SmartImage from '@/components/SmartImage';

interface Props {
  thumbSrc: string;
  src: string;
  alt?: string;
  fetchPriority?: 'high' | 'auto' | 'low';
  onLoaded?: (w: number, h: number) => void;
}

const ImageCanvas = ({ thumbSrc, src, alt, fetchPriority, onLoaded }: Props) => {
  const imgRef = useRef<HTMLImageElement>(null);
  const [scale, setScale] = useState(1);
  const [translate, setTranslate] = useState({ x: 0, y: 0 });
  const [panning, setPanning] = useState(false);
  const last = useRef({ x: 0, y: 0 });

  useEffect(() => {
    const img = imgRef.current;
    if (img && img.complete) {
      onLoaded?.(img.naturalWidth, img.naturalHeight);
    }
  }, [src, onLoaded]);

  const handleWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? -0.1 : 0.1;
    setScale((s) => Math.min(Math.max(0.5, s + delta), 5));
  };

  const handlePointerDown = (e: React.PointerEvent) => {
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
    setPanning(true);
    last.current = { x: e.clientX, y: e.clientY };
  };

  const handlePointerMove = (e: React.PointerEvent) => {
    if (!panning) return;
    const dx = e.clientX - last.current.x;
    const dy = e.clientY - last.current.y;
    setTranslate((t) => ({ x: t.x + dx, y: t.y + dy }));
    last.current = { x: e.clientX, y: e.clientY };
  };

  const endPan = () => setPanning(false);

  const handleDoubleClick = () => {
    setScale((s) => (s !== 1 ? 1 : 2));
    setTranslate({ x: 0, y: 0 });
  };

  return (
    <div className="flex items-center justify-center max-h-full max-w-full">
      <SmartImage
        ref={imgRef}
        thumbSrc={thumbSrc}
        src={src}
        alt={alt || ''}
        loading="eager"
        fetchPriority={fetchPriority}
        onLoadFull={() => {
          const img = imgRef.current;
          if (img) onLoaded?.(img.naturalWidth, img.naturalHeight);
        }}
        onWheel={handleWheel}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={endPan}
        onPointerCancel={endPan}
        onDoubleClick={handleDoubleClick}
        fit="contain"
        className="max-h-[95vh] max-w-[95vw] select-none"
        style={{
          transform: `translate(${translate.x}px, ${translate.y}px) scale(${scale})`,
          cursor: panning ? 'grabbing' : scale > 1 ? 'grab' : 'zoom-in',
        }}
      />
    </div>
  );
};

export default ImageCanvas;
