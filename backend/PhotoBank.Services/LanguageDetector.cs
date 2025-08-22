using System;

namespace PhotoBank.Services
{
    public static class LanguageDetector
    {
        /// <summary>
        /// Detects whether the provided text contains predominantly Russian or English letters.
        /// Returns "ru" for Russian, "en" for English, or "unknown" if neither language letters are found.
        /// </summary>
        /// <param name="text">Input text to analyze.</param>
        /// <returns>"ru", "en" or "unknown".</returns>
        public static string DetectRuEn(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "unknown";
            int cyr = 0, lat = 0;
            foreach (char c in text)
            {
                if ((c >= 'А' && c <= 'я') || c is 'ё' or 'Ё') cyr++;
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) lat++;
            }
            if (cyr == 0 && lat == 0) return "unknown";
            return cyr >= lat ? "ru" : "en";
        }
    }
}
