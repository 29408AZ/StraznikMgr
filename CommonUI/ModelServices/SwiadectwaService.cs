using CommonUI.Common;
using CommonUI.Models;
using CommonUI.ModelServices;
using Microsoft.Extensions.Logging;

namespace CommonUI.ModelServices
{
    public class SwiadectwaService : ISwiadectwaService
    {
        private readonly IMarynarzService _marynarzService;
        private readonly ILogger<SwiadectwaService> _logger;

        public SwiadectwaService(IMarynarzService marynarzService, ILogger<SwiadectwaService> logger)
        {
            _marynarzService = marynarzService ?? throw new ArgumentNullException(nameof(marynarzService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SwiadectwaKategorie> GetSwiadectwaForMarynarzAsync(int marynarzId, CancellationToken cancellationToken = default)
        {
            var swiadectwaKategorie = new SwiadectwaKategorie();
            var result = await _marynarzService.GetSwiadectwaAsync(marynarzId, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Nie udało się pobrać świadectw dla marynarza ID={MarynarzId}: {Error}", 
                    marynarzId, result.Error);
                return swiadectwaKategorie;
            }

            _logger.LogDebug("Przetwarzanie świadectw dla marynarza ID={MarynarzId}", marynarzId);

            foreach (var swiadectwo in result.Value)
            {
                _logger.LogDebug("Świadectwo - Stanowisko: {Stanowisko}, Kategoria: {Kategoria}",
                    swiadectwo.Stanowisko, swiadectwo.Jednostka);

                var kategoria = swiadectwo.Jednostka.Trim().ToUpper();

                // Kolejność jest ważna! III musi być przed II, bo "III" zawiera "II"
                if (kategoria.Contains("IV"))
                {
                    swiadectwaKategorie.SwiadectwaKatIV.Add(swiadectwo);
                    _logger.LogDebug("Dodano do KAT IV");
                }
                else if (kategoria.Contains("III"))
                {
                    swiadectwaKategorie.SwiadectwaKatIII.Add(swiadectwo);
                    _logger.LogDebug("Dodano do KAT III");
                }
                else if (kategoria.Contains("II"))
                {
                    swiadectwaKategorie.SwiadectwaKatII.Add(swiadectwo);
                    _logger.LogDebug("Dodano do KAT II");
                }
                else
                {
                    _logger.LogWarning("Nieznana kategoria: {Kategoria}", kategoria);
                }
            }

            _logger.LogInformation("Przypisano świadectwa: KAT II={KatII}, KAT III={KatIII}, KAT IV={KatIV}",
                swiadectwaKategorie.SwiadectwaKatII.Count,
                swiadectwaKategorie.SwiadectwaKatIII.Count,
                swiadectwaKategorie.SwiadectwaKatIV.Count);

            return swiadectwaKategorie;
        }
    }
}