using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ImageMagick;

namespace PhotoBank.Services
{
    public static class MagickImageExtensions
    {
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
        public static Bitmap ToBitmap(this MagickImage imageMagick, BitmapDensity bitmapDensity)
        {
            string mapping = "BGR";
            var format = PixelFormat.Format24bppRgb;

            var image = imageMagick;

            try
            {
                if (image.ColorSpace != ColorSpace.sRGB)
                {
                    image = (MagickImage)imageMagick.Clone();
                    image.ColorSpace = ColorSpace.sRGB;
                }

                if (image.HasAlpha)
                {
                    mapping = "BGRA";
                    format = PixelFormat.Format32bppArgb;
                }

                using (IPixelCollection<ushort> pixels = image.GetPixelsUnsafe())
                {
                    var bitmap = new Bitmap(image.Width, image.Height, format);
                    var data = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, format);
                    var destination = data.Scan0;
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte[] bytes = pixels.ToByteArray(0, y, image.Width, 1, mapping);
                        Marshal.Copy(bytes, 0, destination, bytes.Length);

                        destination = new IntPtr(destination.ToInt64() + data.Stride);
                    }

                    bitmap.UnlockBits(data);

                    SetBitmapDensity(imageMagick, bitmap, bitmapDensity);
                    return bitmap;
                }
            }
            finally
            {
                if (!ReferenceEquals(image, imageMagick))
                    image.Dispose();
            }
        }

        public static Bitmap ToBitmap(this MagickImage imageMagick) => ToBitmap(imageMagick, BitmapDensity.Ignore);

        public static Bitmap ToBitmap(this MagickImage imageMagick, ImageFormat imageFormat) => ToBitmap(imageMagick, imageFormat, BitmapDensity.Ignore);

        public static Bitmap ToBitmap(this MagickImage imageMagick, ImageFormat imageFormat, BitmapDensity bitmapDensity)
        {
            imageMagick.Format = InternalMagickFormatInfo.GetFormat(imageFormat);

            MemoryStream memStream = new MemoryStream();
            imageMagick.Write(memStream);
            memStream.Position = 0;

            /* Do not dispose the memStream, the bitmap owns it. */
            var bitmap = new Bitmap(memStream);

            SetBitmapDensity(imageMagick, bitmap, bitmapDensity);

            return bitmap;
        }

        private static void SetBitmapDensity(MagickImage imageMagick, Bitmap bitmap, BitmapDensity bitmapDensity)
        {
            if (bitmapDensity == BitmapDensity.Use)
            {
                var dpi = GetDpi(imageMagick, bitmapDensity);
                bitmap.SetResolution((float)dpi.X, (float)dpi.Y);
            }
        }

        private static Density GetDpi(MagickImage imageMagick, BitmapDensity bitmapDensity)
        {
            if (bitmapDensity == BitmapDensity.Ignore || (imageMagick.Density.Units == DensityUnit.Undefined && imageMagick.Density.X == 0 && imageMagick.Density.Y == 0))
                return new Density(96);

            return imageMagick.Density.ChangeUnits(DensityUnit.PixelsPerInch);
        }
    }
}