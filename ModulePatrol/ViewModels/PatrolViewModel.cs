using CommonUI.Events;
using CommonUI.Models;
using CommonUI.ModelServices;
using CommonUI.Utilities;
using Microsoft.Extensions.Logging;
using ModulePatrol.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModulePatrol.ViewModels
{
    public class PatrolViewModel : BindableBase, IDisposable
    {
        private readonly IMarynarzService _marynarzService;
        private readonly IJednostkaPlywajacaService _jednostkaService;
        private readonly IZalogaService _zalogaService;
        private readonly ISwiadectwaService _swiadectwaService;
        private readonly IPatrolService _patrolService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<PatrolViewModel> _logger;
        
        private CancellationTokenSource? _cts;
        private bool _disposed;

        private DateTime _dataOd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _dataDo = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(2).AddDays(-1);
        private string? _wybranaKategoria;
        private JednostkaPlywajaca? _wybranaJednostka;
        private Marynarz? _wybranyMarynarz;
        private ZalogaSlot? _wybranySlot;

        private ObservableCollection<string> _kategorieJednostek;
        private ObservableCollection<JednostkaPlywajaca> _dostepneJednostki;
        private ObservableCollection<Marynarz> _dostepniMarynarze;
        private ObservableCollection<ZalogaSlot> _slotyZalogi;

        private DelegateCommand? _dodajDoZalogiCmd;
        private DelegateCommand? _usunZZalogiCmd;
        private DelegateCommand? _zapiszPatrolCmd;
        private DelegateCommand? _anulujCommand;
        private readonly Func<ZalogaPodgladRequest, bool?> _podgladFactory;

        public PatrolViewModel(IMarynarzService marynarzService, IJednostkaPlywajacaService jednostkaService,
            IZalogaService zalogaService, ISwiadectwaService swiadectwaService, IPatrolService patrolService,
            IEventAggregator eventAggregator, ILogger<PatrolViewModel> logger, 
            Func<ZalogaPodgladRequest, bool?> podgladFactory)
        {
            _marynarzService = marynarzService;
            _jednostkaService = jednostkaService;
            _zalogaService = zalogaService;
            _swiadectwaService = swiadectwaService;
            _patrolService = patrolService;
            _eventAggregator = eventAggregator;
            _logger = logger;
            _podgladFactory = podgladFactory ?? (_ => false);

            _kategorieJednostek = new ObservableCollection<string>();
            _dostepneJednostki = new ObservableCollection<JednostkaPlywajaca>();
            _dostepniMarynarze = new ObservableCollection<Marynarz>();
            _slotyZalogi = new ObservableCollection<ZalogaSlot>();

            ZaladujKategorieJednostek();
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
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
            
            _disposed = true;
        }
        #region Properties

        public DateTime DataOd
        {
            get => _dataOd;
            set
            {
                if (SetProperty(ref _dataOd, value))
                {
                    _ = OdswiezDostepnychMarynarzyAsync();
                    _zapiszPatrolCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime DataDo
        {
            get => _dataDo;
            set
            {
                if (SetProperty(ref _dataDo, value))
                {
                    _ = OdswiezDostepnychMarynarzyAsync();
                    _zapiszPatrolCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public string? WybranaKategoria
        {
            get => _wybranaKategoria;
            set
            {
                if (SetProperty(ref _wybranaKategoria, value))
                {
                    _logger.LogDebug("Wybrana kategoria: {Kategoria}", value);
                    _ = ZaladujJednostkiDlaKategoriiAsync();
                    _ = ZaladujStanowiskaDlaKategoriiAsync();
                    WybranySlot = null;
                    _zapiszPatrolCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public JednostkaPlywajaca? WybranaJednostka
        {
            get => _wybranaJednostka;
            set
            {
                if (SetProperty(ref _wybranaJednostka, value))
                {
                    _logger.LogDebug("Wybrana jednostka: {Jednostka}", value?.Jednostki);
                    _zapiszPatrolCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public Marynarz? WybranyMarynarz
        {
            get => _wybranyMarynarz;
            set
            {
                if (SetProperty(ref _wybranyMarynarz, value))
                {
                    _dodajDoZalogiCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public ZalogaSlot? WybranySlot
        {
            get => _wybranySlot;
            set
            {
                if (SetProperty(ref _wybranySlot, value))
                {
                    _ = OdswiezDostepnychMarynarzyAsync();
                    _dodajDoZalogiCmd?.RaiseCanExecuteChanged();
                    _usunZZalogiCmd?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> KategorieJednostek => _kategorieJednostek;
        public ObservableCollection<JednostkaPlywajaca> DostepneJednostki => _dostepneJednostki;
        public ObservableCollection<Marynarz> DostepniMarynarze => _dostepniMarynarze;
        public ObservableCollection<ZalogaSlot> SlotyZalogi => _slotyZalogi;

        #endregion

        #region Commands

        public DelegateCommand DodajDoZalogiCmd => _dodajDoZalogiCmd ??= new DelegateCommand(DodajDoZalogi, CanDodajDoZalogi);
        private void DodajDoZalogi()
        {
            if (WybranyMarynarz == null || WybranySlot == null) return;

            WybranySlot.PrzydzielonyMarynarz = WybranyMarynarz;
            WybranyMarynarz = null;
            _ = OdswiezDostepnychMarynarzyAsync();
            _zapiszPatrolCmd?.RaiseCanExecuteChanged();
        }
        private bool CanDodajDoZalogi() =>  WybranyMarynarz != null && WybranySlot != null && !WybranySlot.JestObsadzony;
       
        
        
        
        public DelegateCommand UsunZZalogiCmd => _usunZZalogiCmd ??= new DelegateCommand(UsunZZalogi, CanUsunZZalogi);
        public DelegateCommand ZapiszPatrolCmd => _zapiszPatrolCmd ??= new DelegateCommand(ZapiszPatrol, CanZapiszPatrol);
        public DelegateCommand AnulujCommand => _anulujCommand ??= new DelegateCommand(Anuluj);


        private bool CanUsunZZalogi() => WybranySlot?.JestObsadzony == true;
        private void UsunZZalogi()
        {
            if (WybranySlot?.JestObsadzony != true) return;

            WybranySlot.PrzydzielonyMarynarz = null;
            _ = OdswiezDostepnychMarynarzyAsync();
            _zapiszPatrolCmd?.RaiseCanExecuteChanged();
        }

        private bool CanZapiszPatrol()
        {
            return DataOd <= DataDo &&
                   WybranaKategoria != null &&
                   WybranaJednostka != null &&
                   SlotyZalogi.Any() &&
                   SlotyZalogi.All(s => s.JestObsadzony);
        }

        private async void ZapiszPatrol()
        {
            if (WybranaJednostka == null || WybranaKategoria == null) return;

            try
            {
                var patrol = new Patrol(DataOd, DataDo, WybranaKategoria, WybranaJednostka);

                foreach (var slot in SlotyZalogi.Where(s => s.JestObsadzony && s.PrzydzielonyMarynarz != null))
                {
                    patrol.DodajZaloge(new PatrolZaloga(slot.Stanowisko, slot.PrzydzielonyMarynarz!));
                }

                var request = new ZalogaPodgladRequest
                {
                    DataOd = patrol.DataOd,
                    DataDo = patrol.DataDo,
                    Kategoria = patrol.Kategoria,
                    Jednostka = patrol.Jednostka.Jednostki,
                    Zaloga = patrol.Zaloga
                        .Select(z => new ZalogaPozycja { Stanowisko = z.Stanowisko, Marynarz = z.Marynarz.Nazwa })
                        .ToList()
                };

                var wynik = _podgladFactory(request);
                if (wynik == true)
                {
                    _logger.LogInformation("Użytkownik zatwierdził patrol: {DataOd:d} - {DataDo:d}, Jednostka: {Jednostka}", 
                        patrol.DataOd, patrol.DataDo, patrol.Jednostka.Jednostki);

                    // Zapisz patrol do grafiku Excel
                    var result = await _patrolService.ZapiszPatrolDoGrafikuAsync(patrol);
                    
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("Patrol zapisany pomyślnie do grafiku");
                        
                        foreach (var zalogant in patrol.Zaloga)
                        {
                            _logger.LogDebug("- {Stanowisko}: {Funkcjonariusz}", 
                                zalogant.Stanowisko, zalogant.Marynarz.Funkcjonariusze);
                        }

                        // Publikuj event - inne ViewModele mogą przeładować dane
                        _eventAggregator.GetEvent<GrafikUpdatedEvent>().Publish();
                        
                        // Odśwież listę marynarzy, ponieważ grafik się zmienił
                        await _marynarzService.RefreshAsync();

                        System.Windows.MessageBox.Show(
                            "Patrol został zapisany i grafik zaktualizowany pomyślnie!", 
                            "Sukces", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Information);

                        Anuluj();
                    }
                    else
                    {
                        _logger.LogError("Błąd podczas zapisu patrolu: {Error}", result.Error);
                        System.Windows.MessageBox.Show(
                            $"Nie udało się zapisać patrolu:\n{result.Error}", 
                            "Błąd", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas zapisywania patrolu");
                System.Windows.MessageBox.Show(
                    $"Wystąpił nieoczekiwany błąd:\n{ex.Message}", 
                    "Błąd", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void Anuluj()
        {
            DataOd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DataDo = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(2).AddDays(-1);
            WybranaKategoria = null;
            WybranaJednostka = null;
            WybranyMarynarz = null;
            WybranySlot = null;
            _slotyZalogi.Clear();
            _dostepniMarynarze.Clear();
        }

        #endregion

        #region Helper Methods

        //private void OdswiezDostepnychMarynarzy()
        //{
        //    try
        //    {
        //        _dostepniMarynarze.Clear();
        //        WybranyMarynarz = null;

        //        if (string.IsNullOrEmpty(WybranaKategoria) || WybranySlot == null)
        //            return;

        //        var wszyscyMarynarze = _marynarzService.GetAll();
        //        var przydzieleniMarynarze = SlotyZalogi
        //            .Where(s => s.JestObsadzony)
        //            .Select(s => s.PrzydzielonyMarynarz.Id)
        //            .ToList();

        //        int odrzuceniPrzydzielenie = 0;
        //        int odrzuceniGrafik = 0;
        //        int odrzuceniUprawnienia = 0;

        //        foreach (var marynarz in wszyscyMarynarze)
        //        {
        //            if (przydzieleniMarynarze.Contains(marynarz.Id))
        //            {
        //                odrzuceniPrzydzielenie++;
        //                continue;
        //            }
        //            var dostepnosc = new int[31];
        //            foreach (var sluzba in marynarz.Sluzby)
        //            {
        //                var dataSluzby = sluzba.Key;
        //                if (dataSluzby.Year == DataOd.Year && dataSluzby.Month == DataOd.Month)
        //                {
        //                    dostepnosc[dataSluzby.Day - 1] = 1;
        //                }
        //            }
        //            bool jestZajety = false;
        //            for (int i = DataOd.Day - 1; i < DataDo.Day; i++)
        //            {
        //                if (dostepnosc[i] == 1)
        //                {
        //                    jestZajety = true;
        //                    break;
        //                }
        //            }
        //            if (jestZajety)
        //            {
        //                odrzuceniGrafik++;
        //                continue;
        //            }

        //            var swiadectwa = _swiadectwaService.GetSwiadectwaForMarynarz(marynarz.Id);
        //            var odpowiednieSwiadectwa = WybranaKategoria switch
        //            {
        //                "KAT II" => swiadectwa.SwiadectwaKatII,
        //                "KAT III" => swiadectwa.SwiadectwaKatIII,
        //                "KAT IV" => swiadectwa.SwiadectwaKatIV,
        //                _ => null
        //            };

        //            if (odpowiednieSwiadectwa != null &&
        //                odpowiednieSwiadectwa.Any(s => s.Stanowisko == WybranySlot.Stanowisko))
        //            {
        //                _dostepniMarynarze.Add(marynarz);
        //            }
        //            else
        //            {
        //                odrzuceniUprawnienia++;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}
        private Task OdswiezDostepnychMarynarzyAsync() => RunWithCtsAsync(OdswiezDostepnychMarynarzyCoreAsync);

        private async Task OdswiezDostepnychMarynarzyCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _dostepniMarynarze.Clear();
                if (string.IsNullOrEmpty(WybranaKategoria) || WybranySlot == null)
                    return;

                var marynarzResult = await _marynarzService.GetAllAsync(cancellationToken);
                if (!marynarzResult.IsSuccess || marynarzResult.Value == null)
                    return;

                var wszyscyMarynarze = marynarzResult.Value;
                var przydzieleniMarynarze = SlotyZalogi
                    .Where(s => s.JestObsadzony && s.PrzydzielonyMarynarz != null)
                    .Select(s => s.PrzydzielonyMarynarz!.Id)
                    .ToList();

                _logger.LogDebug("Szukam marynarzy dla stanowiska: '{Stanowisko}', kategoria: '{Kategoria}'", 
                    WybranySlot.Stanowisko, WybranaKategoria);

                foreach (var marynarz in wszyscyMarynarze)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (przydzieleniMarynarze.Contains(marynarz.Id))
                        continue;

                    bool jestZajety = marynarz.Sluzby
                        .Any(sluzba => sluzba.Key >= DataOd && sluzba.Key <= DataDo);

                    if (jestZajety)
                        continue;

                    var swiadectwa = await _swiadectwaService.GetSwiadectwaForMarynarzAsync(marynarz.Id, cancellationToken);
                    var odpowiednieSwiadectwa = WybranaKategoria switch
                    {
                        "KAT II" => swiadectwa.SwiadectwaKatII,
                        "KAT III" => swiadectwa.SwiadectwaKatIII,
                        "KAT IV" => swiadectwa.SwiadectwaKatIV,
                        _ => null
                    };

                    if (odpowiednieSwiadectwa != null)
                    {
                        // Używa StanowiskoMapper do porównania różnych nazw stanowisk
                        bool maUprawnienia = odpowiednieSwiadectwa.Any(s => 
                            StanowiskoMapper.AreEquivalent(s.Stanowisko, WybranySlot.Stanowisko));
                        
                        if (maUprawnienia)
                        {
                            _dostepniMarynarze.Add(marynarz);
                            _logger.LogDebug("Dodano marynarza: {Marynarz} (ma uprawnienia na {Stanowisko})", 
                                marynarz.Nazwa, WybranySlot.Stanowisko);
                        }
                    }
                }

                _logger.LogDebug("Znaleziono {Count} dostępnych marynarzy", _dostepniMarynarze.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Anulowano odświeżanie dostępnych marynarzy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas analizy dostępności marynarzy");
            }
        }

        private async Task RunWithCtsAsync(Func<CancellationToken, Task> operation)
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                await operation(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignorujemy anulowanie
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas odświeżania marynarzy");
            }
        }

        private void ZaladujKategorieJednostek()
        {
            try
            {
                _kategorieJednostek.Clear();
                var kategorie = new[] { "KAT II", "KAT III", "KAT IV" };
                foreach (var kategoria in kategorie)
                {
                    _kategorieJednostek.Add(kategoria);
                }
                _logger.LogDebug("Załadowano {Count} kategorii", kategorie.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania kategorii");
            }
        }
        private async Task ZaladujJednostkiDlaKategoriiCoreAsync()
        {
            try
            {
                _dostepneJednostki.Clear();
                WybranaJednostka = null;

                if (string.IsNullOrEmpty(WybranaKategoria)) return;

                var result = await _jednostkaService.GetAllAsync();
                if (!result.IsSuccess || result.Value == null) return;

                var jednostki = result.Value
                    .Where(j => j.Kategoria == WybranaKategoria)
                    .ToList();

                foreach (var jednostka in jednostki)
                {
                    _dostepneJednostki.Add(jednostka);
                }

                _logger.LogDebug("Załadowano {Count} jednostek dla kategorii {Kategoria}", 
                    jednostki.Count, WybranaKategoria);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Anulowano ładowanie jednostek");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania jednostek");
            }
        }

        private async Task ZaladujJednostkiDlaKategoriiAsync()
        {
            try
            {
                await ZaladujJednostkiDlaKategoriiCoreAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania jednostek");
            }
        }

        private async Task ZaladujStanowiskaDlaKategoriiCoreAsync()
        {
            try
            {
                _slotyZalogi.Clear();

                if (string.IsNullOrEmpty(WybranaKategoria)) return;

                var result = await _zalogaService.GetByKategoriaAsync(WybranaKategoria);
                if (!result.IsSuccess || result.Value == null) return;

                foreach (var stanowisko in result.Value)
                {
                    _slotyZalogi.Add(new ZalogaSlot(stanowisko.Stanowisko));
                }

                _logger.LogDebug("Załadowano {Count} stanowisk dla kategorii {Kategoria}", 
                    _slotyZalogi.Count, WybranaKategoria);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Anulowano ładowanie stanowisk");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania stanowisk");
            }
        }

        private async Task ZaladujStanowiskaDlaKategoriiAsync()
        {
            try
            {
                await ZaladujStanowiskaDlaKategoriiCoreAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania stanowisk");
            }
        }

        #endregion
    }

    public class ZalogaSlot : BindableBase
    {
        private string _stanowisko = string.Empty;
        private Marynarz? _przydzielonyMarynarz;
        private bool _jestObsadzony;

        public string Stanowisko
        {
            get => _stanowisko;
            set => SetProperty(ref _stanowisko, value);
        }

        public Marynarz? PrzydzielonyMarynarz
        {
            get => _przydzielonyMarynarz;
            set
            {
                SetProperty(ref _przydzielonyMarynarz, value);
                JestObsadzony = value != null;
            }
        }

        public bool JestObsadzony
        {
            get => _jestObsadzony;
            private set => SetProperty(ref _jestObsadzony, value);
        }

        public ZalogaSlot(string stanowisko)
        {
            Stanowisko = stanowisko;
        }
    }
}
