namespace PhotoBank.Services
{
    public enum BitmapDensity
    {
        /// <summary>
        /// Ignore the density of the image when creating the bitmap.
        /// </summary>
        Ignore,

        /// <summary>
        /// Use the density of the image when creating the bitmap.
        /// </summary>
        Use,
    }
}