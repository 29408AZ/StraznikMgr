namespace CommonUI.Utilities
{
    /// <summary>
    /// Mapuje skróty stanowisk z arkusza Świadectwa na pełne nazwy z arkusza Zalogi
    /// </summary>
    public static class StanowiskoMapper
    {
        private static readonly Dictionary<string, string> _mapping = new(StringComparer.OrdinalIgnoreCase)
        {
            // Zastępca dowódcy
            { "ZD pokład", "Zastępca dowódcy" },
            { "ZD pokład pokład.", "Zastępca dowódcy" },
            
            // Marynarz pokładowy
            { "Mar. pokładowy", "Marynarz pokładowy" },
            { "Mar.pokładowy", "Marynarz pokładowy" },
            
            // Mechanik
            { "Oficer mechanik", "Mechanik" },
            
            // Motorzysta (literówka w danych)
            { "Motoszysta", "Motorzysta" }
        };

        /// <summary>
        /// Normalizuje nazwę stanowiska ze świadectwa do nazwy z załogi.
        /// Jeśli nie ma mapowania, zwraca oryginalną nazwę.
        /// </summary>
        public static string Normalize(string stanowisko)
        {
            if (string.IsNullOrWhiteSpace(stanowisko))
                return stanowisko;

            var trimmed = stanowisko.Trim();
            return _mapping.TryGetValue(trimmed, out var normalized) 
                ? normalized 
                : trimmed;
        }

        /// <summary>
        /// Sprawdza czy dwa stanowiska są równoważne (po normalizacji).
        /// </summary>
        public static bool AreEquivalent(string stanowisko1, string stanowisko2)
        {
            var normalized1 = Normalize(stanowisko1);
            var normalized2 = Normalize(stanowisko2);
            return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
