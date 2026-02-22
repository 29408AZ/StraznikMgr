using CommonUI.Common;
using CommonUI.Models;

namespace CommonUI.ModelServices
{
    public class MarynarzService : IMarynarzService
    {
        private readonly List<Marynarz> _marynarze;
        private readonly List<Swiadectwo> _swiadectwa;

        public MarynarzService()
        {
            _marynarze = new List<Marynarz>();
            _swiadectwa = new List<Swiadectwo>();
        }

        public Task<Result<IEnumerable<Marynarz>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<IEnumerable<Marynarz>>.Success(_marynarze.OrderBy(m => m.Id).AsEnumerable()));
        }

        public Task<Result> UpdateAsync(Marynarz updatedMarynarz)
        {
            return Task.FromResult(Result.Failure("Modyfikacja danych nie jest obsługiwana."));
        }

        public Task<Result<IEnumerable<Swiadectwo>>> GetSwiadectwaAsync(int marynarzId, CancellationToken cancellationToken = default)
        {
            var result = _swiadectwa
                .Where(s => s.MarynarzId == marynarzId)
                .OrderBy(s => s.Stanowisko);

            return Task.FromResult(Result<IEnumerable<Swiadectwo>>.Success(result.AsEnumerable()));
        }

        public Task<Result<IEnumerable<Swiadectwo>>> GetAllSwiadectwaAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<IEnumerable<Swiadectwo>>.Success(_swiadectwa.OrderBy(s => s.MarynarzId).AsEnumerable()));
        }

        public Task<Result> RefreshAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
}