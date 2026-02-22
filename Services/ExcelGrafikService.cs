using CommonUI.Common;
using CommonUI.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Prism.Events;

namespace Services
{
    public class ExcelGrafikService : BaseExcelService, IGrafikService
    {
        private const int WIERSZ_NAGLOWKA = 1;
        private const int PIERWSZY_WIERSZ_DANYCH = 2;
        private const int OFFSET_KOLUMNY_SLUZB = 4;
        private const int OFFSET_KOLUMN_KONCOWYCH = 13;

        private const string KOLUMNA_NAZWA = "C";
        private const string KOLUMNA_POCZATEK = "D";
        private const string KOLUMNA_KONIEC = "AK";
        private const string KOLUMNA_SLUZBA = "AM";
        private const string KOLUMNA_DYZUR = "AN";
        private const string KOLUMNA_URLOP = "AO";
        private const string KOLUMNA_ODDELEGOWANIE = "AP";
        private const string KOLUMNA_L4 = "AQ";

        private volatile HashSet<string> _dostepneMiesiace = new();

        public ExcelGrafikService(
            IExcelFileService excelFileService,
            IEventAggregator eventAggregator,
            ILogger<ExcelGrafikService> logger)
            : base(excelFileService, eventAggregator, logger)
        {
        }

        protected override Task LoadDataInternalAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var package = _excelFileService.GetPackage();
            var newMiesiace = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(worksheet.Name))
                {
                    newMiesiace.Add(worksheet.Name.ToUpper());
                    _logger.LogDebug("Znaleziono arkusz {Miesiac}", worksheet.Name);
                }
            }

            if (newMiesiace.Count == 0)
            {
                throw new InvalidOperationException("Nie znaleziono żadnych arkuszy grafiku.");
            }

            // Atomowa zamiana
            _dostepneMiesiace = newMiesiace;
            _logger.LogInformation("Załadowano {Count} miesięcy grafiku", _dostepneMiesiace.Count);

            return Task.CompletedTask;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            _isLoaded = false;
            await LoadDataAsync(cancellationToken);
        }

        public async Task<IEnumerable<string>> GetDostepneMiesiaceAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return _dostepneMiesiace.OrderBy(m => m);
        }

        public async Task<Result<GrafikMiesieczny>> WczytajGrafikAsync(
            string miesiac,
            string nazwa,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(miesiac))
            {
                _logger.LogWarning("Próba wczytania grafiku z pustą nazwą miesiąca");
                return Result<GrafikMiesieczny>.Failure("Nazwa miesiąca nie może być pusta");
            }

            if (string.IsNullOrWhiteSpace(nazwa))
            {
                _logger.LogWarning("Próba wczytania grafiku z pustą nazwą marynarza");
                return Result<GrafikMiesieczny>.Failure("Nazwa marynarza nie może być pusta");
            }

            var miesiacUpper = miesiac.ToUpper();

            // Ensure data is loaded
            await EnsureLoadedAsync(cancellationToken);

            if (!_dostepneMiesiace.Contains(miesiacUpper))
            {
                _logger.LogWarning("Miesiąc {Miesiac} nie jest dostępny", miesiac);
                return Result<GrafikMiesieczny>.Failure($"Miesiąc '{miesiac}' nie jest dostępny");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var worksheet = GetWorksheet(miesiacUpper);
                var wiersz = ZnajdzWierszMarynarza(worksheet, nazwa);

                if (wiersz == -1)
                {
                    _logger.LogWarning("Nie znaleziono marynarza {Nazwa} w arkuszu {Miesiac}", nazwa, miesiac);
                    return Result<GrafikMiesieczny>.Failure($"Nie znaleziono marynarza '{nazwa}' w miesiącu '{miesiac}'");
                }

                var grafik = new GrafikMiesieczny
                {
                    GodzinyPoczatek = PobierzDecimal(worksheet, wiersz, KOLUMNA_POCZATEK),
                    GodzinyKoniec = PobierzDecimal(worksheet, wiersz, KOLUMNA_KONIEC),
                    Sluzby = PobierzSluzby(worksheet, wiersz),
                    SumaSluzba = PobierzDecimal(worksheet, wiersz, KOLUMNA_SLUZBA),
                    SumaDyzur = PobierzDecimal(worksheet, wiersz, KOLUMNA_DYZUR),
                    SumaUrlop = PobierzDecimal(worksheet, wiersz, KOLUMNA_URLOP),
                    SumaOddelegowanie = PobierzDecimal(worksheet, wiersz, KOLUMNA_ODDELEGOWANIE),
                    SumaL4 = PobierzDecimal(worksheet, wiersz, KOLUMNA_L4)
                };

                return Result<GrafikMiesieczny>.Success(grafik);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Wczytywanie grafiku dla {Nazwa} zostało anulowane", nazwa);
                return Result<GrafikMiesieczny>.Failure(ResultErrors.OperationCancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd wczytywania grafiku dla {Nazwa} w miesiącu {Miesiac}", nazwa, miesiac);
                return Result<GrafikMiesieczny>.Failure($"Błąd wczytywania grafiku: {ex.Message}");
            }
        }

        private int ZnajdzWierszMarynarza(ExcelWorksheet worksheet, string nazwa)
        {
            int ostatniWiersz = worksheet.Dimension.End.Row;
            int numerKolumny = GetColumnNumber(KOLUMNA_NAZWA);

            for (int wiersz = PIERWSZY_WIERSZ_DANYCH; wiersz <= ostatniWiersz; wiersz++)
            {
                var cellValue = worksheet.Cells[wiersz, numerKolumny].Text?.Trim();
                if (string.Equals(cellValue, nazwa, StringComparison.OrdinalIgnoreCase))
                    return wiersz;
            }

            return -1;
        }

        private Dictionary<int, string> PobierzSluzby(ExcelWorksheet worksheet, int wiersz)
        {
            var sluzby = new Dictionary<int, string>();
            int pierwszaKolumna = worksheet.Dimension.Start.Column + OFFSET_KOLUMNY_SLUZB;
            int ostatniaKolumna = worksheet.Dimension.End.Column - OFFSET_KOLUMN_KONCOWYCH;

            for (int kolumna = pierwszaKolumna; kolumna <= ostatniaKolumna; kolumna++)
            {
                string wartosc = worksheet.Cells[wiersz, kolumna].Text;
                if (!string.IsNullOrWhiteSpace(wartosc))
                {
                    int dzien = kolumna - pierwszaKolumna + 1;
                    sluzby.Add(dzien, wartosc.Trim());
                }
            }

            return sluzby;
        }

        private decimal PobierzDecimal(ExcelWorksheet worksheet, int wiersz, string kolumna)
        {
            try
            {
                int numerKolumny = GetColumnNumber(kolumna);
                var cellValue = worksheet.Cells[wiersz, numerKolumny].Text;

                return decimal.TryParse(cellValue, out decimal wartosc) ? wartosc : 0m;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd podczas parsowania wartości w wierszu {Wiersz}, kolumna {Kolumna}", wiersz, kolumna);
                return 0m;
            }
        }

        private static int GetColumnNumber(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Nazwa kolumny nie może być pusta", nameof(columnName));

            columnName = columnName.ToUpperInvariant();
            int sum = 0;

            for (int i = 0; i < columnName.Length; i++)
            {
                sum *= 26;
                sum += (columnName[i] - 'A' + 1);
            }

            return sum;
        }
    }
}