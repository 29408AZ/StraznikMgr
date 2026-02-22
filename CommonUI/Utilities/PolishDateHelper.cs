namespace CommonUI.Utilities
{
    /// <summary>
    /// Helper do parsowania polskich nazw miesięcy
    /// </summary>
    public static class PolishDateHelper
    {
        private static readonly IReadOnlyDictionary<string, int> PolishMonths =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "STYCZEN", 1 }, { "STYCZEŃ", 1 },
                { "LUTY", 2 },
                { "MARZEC", 3 },
                { "KWIECIEN", 4 }, { "KWIECIEŃ", 4 },
                { "MAJ", 5 },
                { "CZERWIEC", 6 },
                { "LIPIEC", 7 },
                { "SIERPIEN", 8 }, { "SIERPIEŃ", 8 },
                { "WRZESIEN", 9 }, { "WRZESIEŃ", 9 },
                { "PAZDZIERNIK", 10 }, { "PAŹDZIERNIK", 10 },
                { "LISTOPAD", 11 },
                { "GRUDZIEN", 12 }, { "GRUDZIEŃ", 12 }
            };

        /// <summary>
        /// Próbuje sparsować polską nazwę miesiąca na numer miesiąca
        /// </summary>
        public static bool TryParsePolishMonth(string monthName, out int month)
        {
            month = 0;
            if (string.IsNullOrWhiteSpace(monthName))
                return false;

            return PolishMonths.TryGetValue(monthName.Trim(), out month);
        }

        /// <summary>
        /// Próbuje sparsować polską nazwę miesiąca i dzień na DateTime
        /// </summary>
        public static bool TryParseMiesiacToDate(string miesiac, int dzien, out DateTime data)
        {
            data = default;

            if (!TryParsePolishMonth(miesiac, out var month))
                return false;

            try
            {
                var currentYear = DateTime.Now.Year;
                data = new DateTime(currentYear, month, dzien);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Zwraca nazwę polskiego miesiąca dla podanego numeru
        /// </summary>
        public static string GetPolishMonthName(int month)
        {
            return month switch
            {
                1 => "STYCZEŃ",
                2 => "LUTY",
                3 => "MARZEC",
                4 => "KWIECIEŃ",
                5 => "MAJ",
                6 => "CZERWIEC",
                7 => "LIPIEC",
                8 => "SIERPIEŃ",
                9 => "WRZESIEŃ",
                10 => "PAŹDZIERNIK",
                11 => "LISTOPAD",
                12 => "GRUDZIEŃ",
                _ => throw new ArgumentOutOfRangeException(nameof(month), "Miesiąc musi być w zakresie 1-12")
            };
        }
    }
}
