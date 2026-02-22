using System;
using System.Collections.Generic;

namespace CommonUI.Utilities
{
    /// <summary>
    /// Klasa pomocnicza do konwersji numerów miesięcy na polskie nazwy
    /// </summary>
    public static class MiesiacHelper
    {
        private static readonly Dictionary<PolskiMiesiac, string> _nazwyMiesiecy = new()
        {
            { PolskiMiesiac.Styczen, "STYCZEŃ" },
            { PolskiMiesiac.Luty, "LUTY" },
            { PolskiMiesiac.Marzec, "MARZEC" },
            { PolskiMiesiac.Kwiecien, "KWIECIEŃ" },
            { PolskiMiesiac.Maj, "MAJ" },
            { PolskiMiesiac.Czerwiec, "CZERWIEC" },
            { PolskiMiesiac.Lipiec, "LIPIEC" },
            { PolskiMiesiac.Sierpien, "SIERPIEŃ" },
            { PolskiMiesiac.Wrzesien, "WRZESIEŃ" },
            { PolskiMiesiac.Pazdziernik, "PAŹDZIERNIK" },
            { PolskiMiesiac.Listopad, "LISTOPAD" },
            { PolskiMiesiac.Grudzien, "GRUDZIEŃ" }
        };

        /// <summary>
        /// Konwertuje numer miesiąca (1-12) na polską nazwę w wielkich literach
        /// </summary>
        /// <param name="miesiacNumer">Numer miesiąca (1 = styczeń, 12 = grudzień)</param>
        /// <returns>Polska nazwa miesiąca w wielkich literach</returns>
        /// <exception cref="ArgumentOutOfRangeException">Gdy numer miesiąca jest poza zakresem 1-12</exception>
        public static string GetNazwa(int miesiacNumer)
        {
            if (miesiacNumer < 1 || miesiacNumer > 12)
                throw new ArgumentOutOfRangeException(nameof(miesiacNumer), 
                    "Numer miesiąca musi być w zakresie 1-12");

            var miesiac = (PolskiMiesiac)miesiacNumer;
            return GetNazwa(miesiac);
        }

        /// <summary>
        /// Konwertuje enum PolskiMiesiac na polską nazwę w wielkich literach
        /// </summary>
        /// <param name="miesiac">Wartość enum PolskiMiesiac</param>
        /// <returns>Polska nazwa miesiąca w wielkich literach</returns>
        public static string GetNazwa(PolskiMiesiac miesiac)
        {
            return _nazwyMiesiecy[miesiac];
        }

        /// <summary>
        /// Konwertuje numer miesiąca (1-12) na wartość enum PolskiMiesiac
        /// </summary>
        /// <param name="miesiacNumer">Numer miesiąca (1 = styczeń, 12 = grudzień)</param>
        /// <returns>Wartość enum PolskiMiesiac</returns>
        /// <exception cref="ArgumentOutOfRangeException">Gdy numer miesiąca jest poza zakresem 1-12</exception>
        public static PolskiMiesiac GetMiesiac(int miesiacNumer)
        {
            if (miesiacNumer < 1 || miesiacNumer > 12)
                throw new ArgumentOutOfRangeException(nameof(miesiacNumer), 
                    "Numer miesiąca musi być w zakresie 1-12");

            return (PolskiMiesiac)miesiacNumer;
        }
    }
}
