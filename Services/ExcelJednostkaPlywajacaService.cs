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
    public class ExcelJednostkaPlywajacaService : BaseExcelService, IJednostkaPlywajacaService
    {
        private const int WIERSZ_NAGLOWKA = 1;
        private const int PIERWSZY_WIERSZ_DANYCH = 2;
        private const int PIERWSZA_KOLUMNA = 1;
        private const string ARKUSZ_JEDNOSTKI = "Jednostki";

        private ImmutableList<JednostkaPlywajaca> _jednostkiCache = ImmutableList<JednostkaPlywajaca>.Empty;

        public ExcelJednostkaPlywajacaService(
            IExcelFileService excelFileService,
            IEventAggregator eventAggregator,
            ILogger<ExcelJednostkaPlywajacaService> logger)
            : base(excelFileService, eventAggregator, logger)
        {
        }

        protected override Task LoadDataInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var worksheet = GetWorksheet(ARKUSZ_JEDNOSTKI);
                var result = new List<JednostkaPlywajaca>();

                int col = PIERWSZA_KOLUMNA;

                while (!string.IsNullOrWhiteSpace(worksheet.Cells[WIERSZ_NAGLOWKA, col].Text))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var kategoria = worksheet.Cells[WIERSZ_NAGLOWKA, col].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(kategoria))
                    {
                        col++;
                        continue;
                    }

                    int row = PIERWSZY_WIERSZ_DANYCH;

                    while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                    {
                        var numerBurtowy = worksheet.Cells[row, col].Text?.Trim();

                        if (!string.IsNullOrWhiteSpace(numerBurtowy))
                        {
                            try
                            {
                                result.Add(new JednostkaPlywajaca(kategoria, numerBurtowy));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Błąd podczas dodawania jednostki {Kategoria} {NumerBurtowy}", kategoria, numerBurtowy);
                            }
                        }

                        row++;
                    }

                    col++;
                }

                // Atomowa zamiana - thread-safe
                _jednostkiCache = result.ToImmutableList();

                _logger.LogInformation("Wczytano {Count} jednostek pływających", _jednostkiCache.Count);
                _eventAggregator.GetEvent<JednostkiUpdatedEvent>().Publish();

                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ładowanie jednostek zostało anulowane");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wczytywania danych jednostek");
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
                _logger.LogError(ex, "Błąd podczas odświeżania jednostek");
                return Result.Failure($"Błąd podczas odświeżania: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<JednostkaPlywajaca>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                var result = _jednostkiCache
                    .OrderBy(j => j.Kategoria)
                    .ThenBy(j => j.NumerBurtowy);

                return Result<IEnumerable<JednostkaPlywajaca>>.Success(result.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania jednostek");
                return Result<IEnumerable<JednostkaPlywajaca>>.Failure($"Błąd: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<JednostkaPlywajaca>>> GetByKategoriaAsync(string kategoria, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kategoria))
            {
                _logger.LogWarning("Próba pobrania jednostek z pustą kategorią");
                return Result<IEnumerable<JednostkaPlywajaca>>.Failure("Kategoria nie może być pusta");
            }

            await EnsureLoadedAsync(cancellationToken);

            var result = _jednostkiCache
                .Where(j => j.Kategoria.Equals(kategoria, StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => j.NumerBurtowy);

            return Result<IEnumerable<JednostkaPlywajaca>>.Success(result.AsEnumerable());
        }
    }
}
