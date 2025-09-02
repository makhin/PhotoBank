import { createPortal } from 'react-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { X, ChevronLeft, ChevronRight } from 'lucide-react';
import { useEffect } from 'react';

import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/shared/ui/dialog';
import { useAppDispatch, useAppSelector } from '@/app/hook';

import ImageCanvas from './ImageCanvas';
import { close, next, prev } from './viewerSlice';
import { prefetchAround } from './prefetch';

const Lightbox = () => {
  const dispatch = useAppDispatch();
  const { isOpen, items, index } = useAppSelector((s) => s.viewer);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (!isOpen) return;
      if (e.key === 'Escape') dispatch(close());
      if (e.key === 'ArrowRight') dispatch(next());
      if (e.key === 'ArrowLeft') dispatch(prev());
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [dispatch, isOpen]);

  useEffect(() => {
    if (isOpen) prefetchAround(items, index);
  }, [isOpen, items, index]);

  if (!isOpen) return null;
  const item = items[index];
  if (!item) return null;

  return createPortal(
    <Dialog open={isOpen} onOpenChange={(o) => !o && dispatch(close())}>
      <DialogContent className="p-0 bg-black/90 text-white max-w-none w-screen h-screen flex items-center justify-center" showCloseButton={false}>
        <DialogTitle className="sr-only">Lightbox</DialogTitle>
        <DialogDescription className="sr-only">Image viewer</DialogDescription>
        <button aria-label="Close" className="absolute top-4 right-4 text-white" onClick={() => dispatch(close())}>
          <X className="w-6 h-6" />
        </button>
        <button aria-label="Previous image" className="absolute left-4 top-1/2 -translate-y-1/2 text-white" onClick={() => dispatch(prev())}>
          <ChevronLeft className="w-8 h-8" />
        </button>
        <button aria-label="Next image" className="absolute right-4 top-1/2 -translate-y-1/2 text-white" onClick={() => dispatch(next())}>
          <ChevronRight className="w-8 h-8" />
        </button>
        <div className="absolute top-4 left-4 text-sm">
          {index + 1} / {items.length}
        </div>
        <AnimatePresence mode="wait">
          <motion.div
            key={item.id}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="max-w-full max-h-full"
          >
            <ImageCanvas
              thumbSrc={item.preview}
              src={item.preview}
              alt={item.title}
              fetchPriority="high"
            />
          </motion.div>
        </AnimatePresence>
      </DialogContent>
    </Dialog>,
    document.body
  );
};

export default Lightbox;
