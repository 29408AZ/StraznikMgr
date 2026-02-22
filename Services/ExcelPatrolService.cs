using CommonUI.Common;
using CommonUI.Models;
using CommonUI.ModelServices;
using CommonUI.Utilities;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class ExcelPatrolService : IPatrolService
    {
        private readonly IExcelFileService _excelFileService;
        private readonly ILogger<ExcelPatrolService> _logger;
        private readonly SemaphoreSlim _dataLock = new(1, 1);

        public ExcelPatrolService(IExcelFileService excelFileService, ILogger<ExcelPatrolService> logger)
        {
            _excelFileService = excelFileService;
            _logger = logger;
        }

        public async Task<Result<bool>> ZapiszPatrolDoGrafikuAsync(Patrol patrol, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dataLock.WaitAsync(cancellationToken);

                _logger.LogInformation("Rozpoczęto zapis patrolu do grafiku: {DataOd} - {DataDo}, Jednostka: {Jednostka}",
                    patrol.DataOd, patrol.DataDo, patrol.Jednostka.Jednostki);

                var package = _excelFileService.GetPackage();

                // Iterujemy przez wszystkie dni patrolu
                for (var date = patrol.DataOd.Date; date <= patrol.DataDo.Date; date = date.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var monthName = GetMonthSheetName(date.Month);
                    var worksheet = GetWorksheet(monthName);

                    if (worksheet == null)
                    {
                        return Result<bool>.Failure($"Nie znaleziono arkusza grafiku dla miesiąca: {monthName}");
                    }

                    _logger.LogDebug("Przetwarzanie dnia: {Date} w arkuszu {MonthName}", date, monthName);

                    // Dla każdego marynarza w załodze
                    foreach (var zalogant in patrol.Zaloga)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var marynarz = zalogant.Marynarz;
                        _logger.LogDebug("Zapisuję wpis dla marynarza: {Marynarz}", marynarz.Nazwa);

                        // Znajdź wiersz marynarza
                        var rowIndex = FindMarynarzRow(worksheet, marynarz.Nazwa);
                        if (rowIndex == -1)
                        {
                            return Result<bool>.Failure(
                                $"Marynarz '{marynarz.Nazwa}' nie został znaleziony w grafiku {monthName}");
                        }

                        // Oblicz kolumnę dla tego dnia (E=5 dla dnia 1, F=6 dla dnia 2, itd.)
                        int columnIndex = 4 + date.Day; // D=4 (godziny początkowe), E=5 (dzień 1)

                        // Walidacja: sprawdź czy komórka jest pusta
                        var existingValue = worksheet.Cells[rowIndex, columnIndex].Value?.ToString();
                        if (!string.IsNullOrWhiteSpace(existingValue))
                        {
                            return Result<bool>.Failure(
                                $"Konflikt: Marynarz '{marynarz.Nazwa}' ma już wpis w grafiku w dniu {date:dd.MM.yyyy}: '{existingValue}'");
                        }

                        // Wpisz "P" (patrol)
                        worksheet.Cells[rowIndex, columnIndex].Value = "P";
                        _logger.LogDebug("Wpisano 'P' dla {Marynarz} w dniu {Date} (wiersz {Row}, kolumna {Col})",
                            marynarz.Nazwa, date, rowIndex, columnIndex);
                    }
                }

                // Zapisz plik
                await _excelFileService.SaveAsync(cancellationToken);

                _logger.LogInformation("Patrol zapisany pomyślnie do grafiku");
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Anulowano zapis patrolu do grafiku");
                return Result<bool>.Failure("Operacja została anulowana");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas zapisu patrolu do grafiku");
                return Result<bool>.Failure($"Błąd podczas zapisu: {ex.Message}");
            }
            finally
            {
                _dataLock.Release();
            }
        }

        private string GetMonthSheetName(int month)
        {
            return MiesiacHelper.GetNazwa(month);
        }

        private ExcelWorksheet? GetWorksheet(string sheetName)
        {
            var package = _excelFileService.GetPackage();
            return package.Workbook.Worksheets.FirstOrDefault(ws =>
                ws.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
        }

        private int FindMarynarzRow(ExcelWorksheet worksheet, string marynarzNazwa)
        {
            // Kolumna C zawiera nazwiska marynarzy, zaczynamy od wiersza 2 (wiersz 1 to nagłówki)
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var cellValue = worksheet.Cells[row, 3].Value?.ToString();
                if (cellValue != null && cellValue.Equals(marynarzNazwa, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return -1; // Nie znaleziono
        }
    }
}
