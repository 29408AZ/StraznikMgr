using Microsoft.Win32;
using OfficeOpenXml;
using System.IO;

namespace Services
{
    public sealed class ExcelFileService : IExcelFileService, IDisposable
    {
        private string? _filePath;
        private ExcelPackage? _package;
        private bool _disposed;

        public ExcelFileService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public Task<ExcelPackage> OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Plik '{filePath}' nie istnieje.", filePath);

            _filePath = filePath;
            _package?.Dispose();

            // EPPlus ładuje plik synchronicznie do pamięci - nie używamy Task.Run
            // bo to operacja I/O-bound, a EPPlus i tak nie wspiera prawdziwego async
            _package = new ExcelPackage(new FileInfo(_filePath));

            if (_package.Workbook.Worksheets.Count == 0)
                throw new InvalidOperationException("Plik Excel nie zawiera żadnych arkuszy.");

            return Task.FromResult(_package);
        }

        public string PromptForFilePath(string defaultPath)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz plik Excel",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                CheckFileExists = true,
                InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(defaultPath))
            };

            return openFileDialog.ShowDialog() == true
                ? openFileDialog.FileName
                : throw new FileNotFoundException("Nie wybrano pliku Excel.");
        }

        public string GetFilePath(string defaultPath = "pdsg.xlsx")
        {
            if (string.IsNullOrEmpty(_filePath))
                throw new InvalidOperationException("Plik nie został jeszcze otwarty. Wywołaj OpenFileAsync() najpierw.");

            return _filePath;
        }

        public ExcelPackage GetPackage()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ExcelFileService));

            if (_package == null)
                throw new InvalidOperationException("Package Excel nie jest dostępny. Wywołaj OpenFileAsync() najpierw.");

            return _package;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _package?.Dispose();
                _disposed = true;
            }
        }
    }
}