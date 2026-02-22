using CommonUI.Events;
using CommonUI.Models;
using CommonUI.ModelServices;
using ModuleEdycja.Views;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleEdycja.ViewModels
{
    public class SzczegolyViewModel : BindableBase, IDisposable
    {
        private readonly IRegionManager _regionManager;
        private readonly IMarynarzService _marynarzService;
        private readonly IZalogaService _zalogaService;
        private readonly ISwiadectwaService _swiadectwaService;
        private readonly IEventAggregator _eventAggregator;
        private readonly SubscriptionToken _marynarzSelectedToken;
        private readonly SubscriptionToken _jednostkaSelectedToken;

        private Marynarz? _wybranyMarynarz;
        private JednostkaPlywajaca? _wybranaJednostka;
        private ObservableCollection<Zaloga> _zalogi;
        private SwiadectwaKategorie _swiadectwa = new();
        private bool _isSelectionLocked;
        private bool _disposed;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private const string REGION_WIDOKU = "SelectedViewRegion";
        private const string WIDOK_MARYNARZ = "SzczegolyMarynarzaView";
        private const string WIDOK_JEDNOSTKA = "SzczegolyJednostkiView";

        public SzczegolyViewModel(
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IMarynarzService marynarzService,
            IZalogaService zalogaService,
            ISwiadectwaService swiadectwaService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _marynarzService = marynarzService ?? throw new ArgumentNullException(nameof(marynarzService));
            _zalogaService = zalogaService ?? throw new ArgumentNullException(nameof(zalogaService));
            _swiadectwaService = swiadectwaService ?? throw new ArgumentNullException(nameof(swiadectwaService));
            _zalogi = new ObservableCollection<Zaloga>();

            _marynarzSelectedToken = _eventAggregator.GetEvent<SelectedMarynarzEvent>().Subscribe(OnMarynarzSelected);
            _jednostkaSelectedToken = _eventAggregator.GetEvent<SelectedJednPlywEvent>().Subscribe(OnJednostkaSelected);
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

        public ObservableCollection<Swiadectwo> SwiadectwaKatII => _swiadectwa.SwiadectwaKatII;
        public ObservableCollection<Swiadectwo> SwiadectwaKatIII => _swiadectwa.SwiadectwaKatIII;
        public ObservableCollection<Swiadectwo> SwiadectwaKatIV => _swiadectwa.SwiadectwaKatIV;

        public Marynarz? WybranyMarynarz
        {
            get => _wybranyMarynarz;
            set => SetProperty(ref _wybranyMarynarz, value);
        }

        public JednostkaPlywajaca? WybranaJednostka
        {
            get => _wybranaJednostka;
            set => SetProperty(ref _wybranaJednostka, value);
        }

        public ObservableCollection<Zaloga> Zalogi
        {
            get => _zalogi;
            set => SetProperty(ref _zalogi, value);
        }

        private void OnMarynarzSelected(Marynarz marynarz)
        {
            _ = PrzelaczWidokMarynarzAsync(marynarz);
        }

        private void OnJednostkaSelected(JednostkaPlywajaca jednostka)
        {
            _ = PrzelaczWidokJednostkaAsync(jednostka);
        }

        private async Task PrzelaczWidokMarynarzAsync(Marynarz marynarz)
        {
            if (marynarz == null || _isSelectionLocked) return;

            try
            {
                _isSelectionLocked = true;
                IsBusy = true;
                StatusMessage = "Ładowanie danych funkcjonariusza...";

                WybranyMarynarz = marynarz;
                WybranaJednostka = null;
                _zalogi.Clear();

                await AktualizujSwiadectwaAsync(marynarz.Id);
                AktywujWidok(WIDOK_MARYNARZ);

                StatusMessage = $"Załadowano dane: {marynarz.Nazwa}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas przełączania widoku marynarza: {ex.Message}");
                StatusMessage = $"Błąd: {ex.Message}";
            }
            finally
            {
                _isSelectionLocked = false;
                IsBusy = false;
            }
        }

        private async Task AktualizujSwiadectwaAsync(int marynarzId)
        {
            SwiadectwaKatII.Clear();
            SwiadectwaKatIII.Clear();
            SwiadectwaKatIV.Clear();

            var noweSwiadectwa = await _swiadectwaService.GetSwiadectwaForMarynarzAsync(marynarzId);

            foreach (var swiadectwo in noweSwiadectwa.SwiadectwaKatII)
                SwiadectwaKatII.Add(swiadectwo);
            foreach (var swiadectwo in noweSwiadectwa.SwiadectwaKatIII)
                SwiadectwaKatIII.Add(swiadectwo);
            foreach (var swiadectwo in noweSwiadectwa.SwiadectwaKatIV)
                SwiadectwaKatIV.Add(swiadectwo);
        }

        private async Task PrzelaczWidokJednostkaAsync(JednostkaPlywajaca jednPlyw)
        {
            if (jednPlyw == null || _isSelectionLocked) return;

            try
            {
                _isSelectionLocked = true;
                IsBusy = true;
                StatusMessage = "Ładowanie danych jednostki...";

                WybranaJednostka = jednPlyw;
                WybranyMarynarz = null;
                await ZaladujZalogiDlaJednostkiAsync(jednPlyw);

                AktywujWidok(WIDOK_JEDNOSTKA);

                StatusMessage = $"Załadowano dane: {jednPlyw.NumerBurtowy} ({jednPlyw.Kategoria})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas przełączania widoku jednostki: {ex.Message}");
                StatusMessage = $"Błąd: {ex.Message}";
            }
            finally
            {
                _isSelectionLocked = false;
                IsBusy = false;
            }
        }

        private async Task ZaladujZalogiDlaJednostkiAsync(JednostkaPlywajaca jednostka)
        {
            Zalogi.Clear();
            var result = await _zalogaService.GetByKategoriaAsync(jednostka.Kategoria);
            
            if (!result.IsSuccess)
            {
                StatusMessage = $"Błąd ładowania załóg: {result.Error}";
                return;
            }
            
            if (result.Value == null) return;

            foreach (var zaloga in result.Value)
            {
                Zalogi.Add(zaloga);
            }
        }

        private void AktywujWidok(string nazwaWidoku)
        {
            _regionManager.RequestNavigate(REGION_WIDOKU, nazwaWidoku);
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
                _marynarzSelectedToken?.Dispose();
                _jednostkaSelectedToken?.Dispose();
            }

            _disposed = true;
        }
    }
}
