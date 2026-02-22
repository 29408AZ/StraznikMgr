using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Prism.Events;

namespace Services
{
    public abstract class BaseExcelService : IDisposable
    {
        protected readonly IExcelFileService _excelFileService;
        protected readonly IEventAggregator _eventAggregator;
        protected readonly ILogger _logger;
        protected readonly SemaphoreSlim _dataLock = new(1, 1);
        protected bool _disposed;
        protected bool _isLoaded;

        protected BaseExcelService(IExcelFileService excelFileService, IEventAggregator eventAggregator, ILogger logger)
        {
            _excelFileService = excelFileService ?? throw new ArgumentNullException(nameof(excelFileService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract Task LoadDataInternalAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Ładuje dane z zabezpieczeniem przed wielokrotnym ładowaniem (cache stampede prevention)
        /// </summary>
        public async Task LoadDataAsync(CancellationToken cancellationToken = default)
        {
            await _dataLock.WaitAsync(cancellationToken);
            try
            {
                await LoadDataInternalAsync(cancellationToken);
                _isLoaded = true;
            }
            finally
            {
                _dataLock.Release();
            }
        }

        /// <summary>
        /// Upewnia się, że dane są załadowane (lazy loading)
        /// </summary>
        protected async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
        {
            if (_isLoaded) return;

            await _dataLock.WaitAsync(cancellationToken);
            try
            {
                if (_isLoaded) return; // Double-check
                await LoadDataInternalAsync(cancellationToken);
                _isLoaded = true;
            }
            finally
            {
                _dataLock.Release();
            }
        }

        /// <summary>
        /// Synchroniczne pobranie arkusza - EPPlus nie jest thread-safe,
        /// więc nie używamy Task.Run
        /// </summary>
        protected ExcelWorksheet GetWorksheet(string worksheetName)
        {
            var package = _excelFileService.GetPackage();
            return package.Workbook.Worksheets[worksheetName]
                ?? throw new InvalidOperationException($"Arkusz '{worksheetName}' nie został znaleziony.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dataLock.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
