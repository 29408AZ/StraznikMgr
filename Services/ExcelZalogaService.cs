using CommonUI.Common;
using CommonUI.Events;
using CommonUI.Models;
using CommonUI.ModelServices;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Prism.Events;
using System.Collections.Immutable;

namespace Services
{
    public class ExcelZalogaService : BaseExcelService, IZalogaService
    {
        private const int WIERSZ_NAGLOWKA = 1;
        private const int PIERWSZY_WIERSZ_DANYCH = 2;
        private const int PIERWSZA_KOLUMNA = 1;
        private const string ARKUSZ_ZALOGI = "Zalogi";

        private ImmutableList<Zaloga> _zalogiCache = ImmutableList<Zaloga>.Empty;

        public ExcelZalogaService(
            IExcelFileService excelFileService,
            IEventAggregator eventAggregator,
            ILogger<ExcelZalogaService> logger)
            : base(excelFileService, eventAggregator, logger)
        {
        }

        protected override Task LoadDataInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var worksheet = GetWorksheet(ARKUSZ_ZALOGI);
                var result = new HashSet<Zaloga>(); // HashSet automatycznie usuwa duplikaty

                var lastRow = worksheet.Dimension.End.Row;
                int column = PIERWSZA_KOLUMNA;

                _logger.LogDebug("Ładowanie załóg z arkusza '{Arkusz}', lastRow={LastRow}", ARKUSZ_ZALOGI, lastRow);

                while (!string.IsNullOrWhiteSpace(worksheet.Cells[WIERSZ_NAGLOWKA, column].Text))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var kategoria = worksheet.Cells[WIERSZ_NAGLOWKA, column].Text?.Trim();

                    // Pomijaj kolumny które nie są kategoriami (muszą zaczynać się od "KAT")
                    if (string.IsNullOrWhiteSpace(kategoria) || !kategoria.StartsWith("KAT", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Pomijam kolumnę {Col}: '{Kategoria}' (nie jest kategorią KAT)", column, kategoria);
                        column++;
                        continue;
                    }

                    _logger.LogDebug("Kolumna {Col}: kategoria='{Kategoria}'", column, kategoria);

                    for (int row = PIERWSZY_WIERSZ_DANYCH; row <= lastRow; row++)
                    {
                        var stanowisko = worksheet.Cells[row, column].Text?.Trim();

                        if (!string.IsNullOrWhiteSpace(stanowisko))
                        {
                            try
                            {
                                var zaloga = new Zaloga(kategoria, stanowisko);
                                if (result.Add(zaloga))
                                {
                                    _logger.LogDebug("Dodano: {Kategoria} - {Stanowisko}", kategoria, stanowisko);
                                }
                                else
                                {
                                    _logger.LogDebug("Pominięto duplikat: {Kategoria} - {Stanowisko}", kategoria, stanowisko);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Błąd podczas dodawania załogi {Kategoria} {Stanowisko}", kategoria, stanowisko);
                            }
                        }
                    }

                    column++;
                }

                // Atomowa zamiana - thread-safe
                _zalogiCache = result.ToImmutableList();

                _logger.LogInformation("Wczytano {Count} stanowisk załogi", _zalogiCache.Count);
                _eventAggregator.GetEvent<ZalogiUpdatedEvent>().Publish();

                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ładowanie załóg zostało anulowane");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wczytywania danych załóg");
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
                _logger.LogError(ex, "Błąd podczas odświeżania załóg");
                return Result.Failure($"Błąd podczas odświeżania: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<Zaloga>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                return Result<IEnumerable<Zaloga>>.Success(_zalogiCache.OrderBy(z => z.Kategoria).AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania załóg");
                return Result<IEnumerable<Zaloga>>.Failure($"Błąd: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<Zaloga>>> GetByKategoriaAsync(string kategoria, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kategoria))
            {
                _logger.LogWarning("Próba pobrania załóg z pustą kategorią");
                return Result<IEnumerable<Zaloga>>.Failure("Kategoria nie może być pusta");
            }

            await EnsureLoadedAsync(cancellationToken);
            
            _logger.LogDebug("Szukam załóg dla kategorii: '{Kategoria}'. Dostępne kategorie: {Kategorie}", 
                kategoria, 
                string.Join(", ", _zalogiCache.Select(z => $"'{z.Kategoria}'").Distinct()));
            
            var result = _zalogiCache
                .Where(z => z.Kategoria.Equals(kategoria, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            _logger.LogDebug("Znaleziono {Count} załóg dla kategorii '{Kategoria}'", result.Count, kategoria);

            return Result<IEnumerable<Zaloga>>.Success(result.AsEnumerable());
        }
    }
}
