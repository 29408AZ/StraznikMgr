using CommonUI.Events;
using CommonUI.ModelServices;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Services;
using Straznik.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Straznik
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Rejestracja loggingu
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Debug);
            });

            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));

            // Centralna rejestracja serwisów - singleton dla całej aplikacji
            containerRegistry.RegisterSingleton<IExcelFileService, ExcelFileService>();
            containerRegistry.RegisterSingleton<IGrafikService, ExcelGrafikService>();
            containerRegistry.RegisterSingleton<IMarynarzService, ExcelMarynarzService>();
            containerRegistry.RegisterSingleton<IJednostkaPlywajacaService, ExcelJednostkaPlywajacaService>();
            containerRegistry.RegisterSingleton<IZalogaService, ExcelZalogaService>();
            containerRegistry.RegisterSingleton<ISwiadectwaService, SwiadectwaService>();

            // Rejestracja Lazy<IGrafikService> dla rozwiązania circular dependency
            containerRegistry.Register<Lazy<IGrafikService>>(c => 
                new Lazy<IGrafikService>(() => c.Resolve<IGrafikService>()));
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleListy.ModuleListy>();
            moduleCatalog.AddModule<ModuleEdycja.ModuleEdycja>();
            moduleCatalog.AddModule<ModulePatrol.ModulePatrol>();
        }

        protected override async void OnInitialized()
        {
            base.OnInitialized();
            await InitializeExcelFileAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Container.Resolve<IExcelFileService>() is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas zwalniania pliku Excel: {ex.Message}");
            }

            base.OnExit(e);
        }

        private async Task InitializeExcelFileAsync()
        {
            try
            {
                var excelFileService = Container.Resolve<IExcelFileService>();
                var eventAggregator = Container.Resolve<IEventAggregator>();
                const string defaultPath = "pdsg.xlsx";

                string filePath;
                if (File.Exists(defaultPath))
                {
                    filePath = defaultPath;
                }
                else
                {
                    filePath = Application.Current.Dispatcher.Invoke(() =>
                        excelFileService.PromptForFilePath(defaultPath));
                }

                await excelFileService.OpenFileAsync(filePath);
                
                // Publikuj event po pomyślnym otwarciu pliku - ViewModele przeładują dane
                eventAggregator.GetEvent<ExcelFileOpenedEvent>().Publish();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas otwierania pliku Excel: {ex.Message}");
                MessageBox.Show($"Nie udało się otworzyć pliku Excel: {ex.Message}", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
