using CommonUI.Events;
using CommonUI.Models;
using CommonUI.ModelServices;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleListy.ViewModels
{
    public class ListViewModel : BindableBase, IDisposable
    {
        private readonly IMarynarzService _marynarzService;
        private readonly IJednostkaPlywajacaService _jednostkaService;
        private readonly IEventAggregator _eventAggregator;
        private readonly SubscriptionToken _marynarzUpdatedToken;
        private readonly SubscriptionToken _jednostkiUpdatedToken;
        private readonly SubscriptionToken _fileOpenedToken;

        private ObservableCollection<Marynarz> _marynarze = new();
        private ObservableCollection<JednostkaPlywajaca> _jednostki = new();
        private Marynarz? _selectedMarynarz;
        private JednostkaPlywajaca? _selectedJednPlyw;
        private bool _isMarynarzSelectionChanging;
        private bool _isJednostkaSelectionChanging;
        private bool _disposed;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public ListViewModel(
            IMarynarzService marynarzService,
            IJednostkaPlywajacaService jednostkaService,
            IEventAggregator eventAggregator)
        {
            _marynarzService = marynarzService ?? throw new ArgumentNullException(nameof(marynarzService));
            _jednostkaService = jednostkaService ?? throw new ArgumentNullException(nameof(jednostkaService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            _marynarzUpdatedToken = _eventAggregator.GetEvent<MarynarzUpdatedEvent>().Subscribe(() => _ = ZaladujDaneAsync());
            _jednostkiUpdatedToken = _eventAggregator.GetEvent<JednostkiUpdatedEvent>().Subscribe(() => _ = ZaladujDaneAsync());
            _fileOpenedToken = _eventAggregator.GetEvent<ExcelFileOpenedEvent>().Subscribe(() => _ = ZaladujDaneAsync());
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<Marynarz> Marynarze
        {
            get => _marynarze;
            set => SetProperty(ref _marynarze, value);
        }

        public ObservableCollection<JednostkaPlywajaca> Jednostki
        {
            get => _jednostki;
            set => SetProperty(ref _jednostki, value);
        }

        public Marynarz? SelectedMarynarz
        {
            get => _selectedMarynarz;
            set
            {
                if (_isMarynarzSelectionChanging) return;

                try
                {
                    _isMarynarzSelectionChanging = true;
                    _isJednostkaSelectionChanging = true;
                    if (SetProperty(ref _selectedMarynarz, value))
                    {
                        System.Diagnostics.Debug.WriteLine($"Wybrano marynarza: {value?.Nazwa}");
                        if (value != null)
                        {
                            _eventAggregator.GetEvent<SelectedMarynarzEvent>().Publish(value);
                        }
                        _selectedJednPlyw = null;
                        RaisePropertyChanged(nameof(SelectedJednPlyw));
                    }
                }
                finally
                {
                    _isMarynarzSelectionChanging = false;
                    _isJednostkaSelectionChanging = false;
                }
            }
        }

        public JednostkaPlywajaca? SelectedJednPlyw
        {
            get => _selectedJednPlyw;
            set
            {
                if (_isJednostkaSelectionChanging) return;

                try
                {
                    _isJednostkaSelectionChanging = true;
                    _isMarynarzSelectionChanging = true;
                    if (SetProperty(ref _selectedJednPlyw, value))
                    {
                        System.Diagnostics.Debug.WriteLine($"Wybrano jednostkę: {value?.NumerBurtowy} ({value?.Kategoria})");
                        if (value != null)
                        {
                            _eventAggregator.GetEvent<SelectedJednPlywEvent>().Publish(value);
                        }
                        _selectedMarynarz = null;
                        RaisePropertyChanged(nameof(SelectedMarynarz));
                    }
                }
                finally
                {
                    _isJednostkaSelectionChanging = false;
                    _isMarynarzSelectionChanging = false;
                }
            }
        }

        private async Task ZaladujDaneAsync()
        {
            if (IsBusy) return; // Zapobiegaj wielokrotnemu ładowaniu

            try
            {
                IsBusy = true;
                StatusMessage = "Ładowanie danych...";

                var marynarzResult = await _marynarzService.GetAllAsync();
                var jednostkaResult = await _jednostkaService.GetAllAsync();

                // Zachowaj aktualnie wybranego marynarza i jednostkę
                var aktualnieWybranyMarynarz = _selectedMarynarz;
                var aktualnieWybranaJednostka = _selectedJednPlyw;

                // Tymczasowo wyłącz publikowanie zdarzeń podczas ładowania
                _isMarynarzSelectionChanging = true;
                _isJednostkaSelectionChanging = true;

                if (marynarzResult.IsSuccess && marynarzResult.Value != null)
                {
                    Marynarze = new ObservableCollection<Marynarz>(marynarzResult.Value);
                    StatusMessage = $"Załadowano {Marynarze.Count} marynarzy";
                }
                else if (!marynarzResult.IsSuccess)
                {
                    StatusMessage = $"Błąd ładowania marynarzy: {marynarzResult.Error}";
                }

                if (jednostkaResult.IsSuccess && jednostkaResult.Value != null)
                {
                    Jednostki = new ObservableCollection<JednostkaPlywajaca>(jednostkaResult.Value);
                    StatusMessage = $"Załadowano {Marynarze.Count} marynarzy i {Jednostki.Count} jednostek";
                }
                else if (!jednostkaResult.IsSuccess)
                {
                    StatusMessage = $"Błąd ładowania jednostek: {jednostkaResult.Error}";
                }

                // Przywróć wybór, jeśli element nadal istnieje w kolekcji
                if (aktualnieWybranyMarynarz != null)
                {
                    _selectedMarynarz = Marynarze.FirstOrDefault(m => m.Id == aktualnieWybranyMarynarz.Id);
                    RaisePropertyChanged(nameof(SelectedMarynarz));
                }

                if (aktualnieWybranaJednostka != null)
                {
                    _selectedJednPlyw = Jednostki.FirstOrDefault(j =>
                        j.NumerBurtowy == aktualnieWybranaJednostka.NumerBurtowy &&
                        j.Kategoria == aktualnieWybranaJednostka.Kategoria);
                    RaisePropertyChanged(nameof(SelectedJednPlyw));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas ładowania danych: {ex.Message}");
                StatusMessage = $"Błąd: {ex.Message}";
            }
            finally
            {
                // Przywróć obsługę zdarzeń
                _isMarynarzSelectionChanging = false;
                _isJednostkaSelectionChanging = false;
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _marynarzUpdatedToken?.Dispose();
                _jednostkiUpdatedToken?.Dispose();
                _fileOpenedToken?.Dispose();
            }

            _disposed = true;
        }
    }
}