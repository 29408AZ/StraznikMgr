using CommonUI.Common;
using CommonUI.Events;
using CommonUI.Models;
using CommonUI.ModelServices;
using CommonUI.Utilities;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Prism.Events;
using System.Collections.Immutable;

namespace Services
{
    public class ExcelMarynarzService : BaseExcelService, IMarynarzService
    {
        private const int WIERSZ_NAGLOWKA = 1;
        private const int PIERWSZY_WIERSZ_DANYCH = 2;
        private const int KOLUMNA_NAZWA = 2;
        private const string ARKUSZ_ZASOBY = "Zasoby";
        private const string ARKUSZ_SWIADECTWA = "Swiadectwa";

        // Thread-safe immutable collections
        private ImmutableList<Marynarz> _marynarzeCache = ImmutableList<Marynarz>.Empty;
        private ImmutableList<Swiadectwo> _swiadectwaCache = ImmutableList<Swiadectwo>.Empty;

        private readonly Lazy<IGrafikService> _grafikService;

        public ExcelMarynarzService(
            IExcelFileService excelFileService,
            IEventAggregator eventAggregator,
            ILogger<ExcelMarynarzService> logger,
            Lazy<IGrafikService> grafikService)
            : base(excelFileService, eventAggregator, logger)
        {
            _grafikService = grafikService ?? throw new ArgumentNullException(nameof(grafikService));
        }

        protected override async Task LoadDataInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                var marynarze = await LoadMarynarzeAsync(cancellationToken);
                var swiadectwa = await LoadSwiadectwaAsync(cancellationToken);

                // Atomowa zamiana - thread-safe
                _marynarzeCache = marynarze.ToImmutableList();
                _swiadectwaCache = swiadectwa.ToImmutableList();

                _eventAggregator.GetEvent<MarynarzeUpdatedEvent>().Publish();
                _logger.LogInformation("Załadowano {MarynarzeCount} marynarzy i {SwiadectwaCount} świadectw",
                    _marynarzeCache.Count, _swiadectwaCache.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ładowanie danych marynarzy zostało anulowane");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się wczytać danych marynarzy");
                throw;
            }
        }

        public async Task<Result> RefreshAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _isLoaded = false;
                await LoadDataAsync(cancellationToken);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                return Result.Failure(ResultErrors.OperationCancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas odświeżania danych marynarzy");
                return Result.Failure($"Błąd podczas odświeżania: {ex.Message}");
            }
        }

        private async Task<List<Marynarz>> LoadMarynarzeAsync(CancellationToken cancellationToken)
        {
            var result = new List<Marynarz>();

            var worksheet = GetWorksheet(ARKUSZ_ZASOBY);
            var dostepneMiesiace = (await _grafikService.Value.GetDostepneMiesiaceAsync(cancellationToken)).ToList();

            if (dostepneMiesiace.Count == 0)
            {
                _logger.LogWarning("Brak dostępnych miesięcy w grafiku");
                return result;
            }

            var lastRow = worksheet.Dimension.End.Row;

            for (int row = PIERWSZY_WIERSZ_DANYCH; row <= lastRow; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nazwa = worksheet.Cells[row, KOLUMNA_NAZWA].Text?.Trim();
                if (string.IsNullOrWhiteSpace(nazwa))
                    continue;

                var marynarz = ParseMarynarzRow(worksheet, row);
                if (marynarz != null)
                {
                    foreach (var miesiac in dostepneMiesiace)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var grafikResult = await _grafikService.Value.WczytajGrafikAsync(miesiac, marynarz.Nazwa, cancellationToken);
                        if (grafikResult.IsSuccess && grafikResult.Value != null)
                        {
                            var grafikMiesieczny = grafikResult.Value;
                            marynarz.DodajBilansMiesieczny(
                                miesiac,
                                grafikMiesieczny.GodzinyPoczatek,
                                grafikMiesieczny.GodzinyKoniec,
                                grafikMiesieczny.SumaSluzba,
                                grafikMiesieczny.SumaDyzur,
                                grafikMiesieczny.SumaUrlop,
                                grafikMiesieczny.SumaOddelegowanie,
                                grafikMiesieczny.SumaL4);

                            foreach (var sluzba in grafikMiesieczny.Sluzby)
                            {
                                if (PolishDateHelper.TryParseMiesiacToDate(miesiac, sluzba.Key, out var data))
                                {
                                    marynarz.DodajSluzbe(data, sluzba.Value);
                                }
                            }
                        }
                    }

                    result.Add(marynarz);
                    _logger.LogDebug("Dodano marynarza: {Nazwa}", marynarz.Nazwa);
                }
            }

            return result;
        }

        private Task<List<Swiadectwo>> LoadSwiadectwaAsync(CancellationToken cancellationToken)
        {
            var result = new List<Swiadectwo>();
            var worksheet = GetWorksheet(ARKUSZ_SWIADECTWA);

            int row = PIERWSZY_WIERSZ_DANYCH;

            while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var swiadectwo = ParseSwiadectwoRow(worksheet, row);
                if (swiadectwo != null)
                {
                    result.Add(swiadectwo);
                }
                row++;
            }

            _logger.LogInformation("Wczytano {Count} świadectw", result.Count);
            return Task.FromResult(result);
        }

        private Marynarz? ParseMarynarzRow(ExcelWorksheet worksheet, int row)
        {
            try
            {
                var id = row - WIERSZ_NAGLOWKA;
                var nazwa = worksheet.Cells[row, KOLUMNA_NAZWA].Text?.Trim();

                if (string.IsNullOrWhiteSpace(nazwa))
                {
                    _logger.LogWarning("Pusty wiersz marynarza w rzędzie {Row}", row);
                    return null;
                }

                return new Marynarz(id, nazwa);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd podczas parsowania wiersza {Row}", row);
                return null;
            }
        }

        private Swiadectwo? ParseSwiadectwoRow(ExcelWorksheet worksheet, int row)
        {
            try
            {
                var idText = worksheet.Cells[row, 1].Text?.Trim();
                var stanowisko = worksheet.Cells[row, 3].Text?.Trim();
                var jednostka = worksheet.Cells[row, 4].Text?.Trim();

                if (string.IsNullOrWhiteSpace(idText) ||
                    string.IsNullOrWhiteSpace(stanowisko) ||
                    string.IsNullOrWhiteSpace(jednostka))
                {
                    _logger.LogWarning("Niepełne dane świadectwa w wierszu {Row}", row);
                    return null;
                }

                if (!int.TryParse(idText, out var id))
                {
                    _logger.LogWarning("Nieprawidłowy ID w wierszu {Row}: {IdText}", row, idText);
                    return null;
                }

                return new Swiadectwo(id, stanowisko, jednostka);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd w wierszu {Row}", row);
                return null;
            }
        }

        public async Task<Result<IEnumerable<Marynarz>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                return Result<IEnumerable<Marynarz>>.Success(_marynarzeCache.OrderBy(m => m.Id).AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania marynarzy");
                return Result<IEnumerable<Marynarz>>.Failure($"Błąd: {ex.Message}");
            }
        }

        public Task<Result> UpdateAsync(Marynarz updatedMarynarz)
        {
            return Task.FromResult(Result.Failure("Modyfikacja danych nie jest obsługiwana."));
        }

        public async Task<Result<IEnumerable<Swiadectwo>>> GetSwiadectwaAsync(int marynarzId, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            var result = _swiadectwaCache
                .Where(s => s.MarynarzId == marynarzId)
                .OrderBy(s => s.Stanowisko);

            return Result<IEnumerable<Swiadectwo>>.Success(result.AsEnumerable());
        }

        public async Task<Result<IEnumerable<Swiadectwo>>> GetAllSwiadectwaAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return Result<IEnumerable<Swiadectwo>>.Success(_swiadectwaCache.OrderBy(s => s.MarynarzId).AsEnumerable());
        }
    }
}
