namespace CommonUI.Models
{
    public class Patrol
    {
        private DateTime _dataOd;
        private DateTime _dataDo;

        public DateTime DataOd
        {
            get => _dataOd;
            set
            {
                if (value > _dataDo && _dataDo != default)
                    throw new ArgumentException("Data rozpoczęcia nie może być późniejsza niż data zakończenia.");
                _dataOd = value;
            }
        }

        public DateTime DataDo
        {
            get => _dataDo;
            set
            {
                if (value < _dataOd)
                    throw new ArgumentException("Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
                _dataDo = value;
            }
        }

        public string Kategoria { get; }
        public JednostkaPlywajaca Jednostka { get; }

        private readonly List<PatrolZaloga> _zaloga = new();
        public IReadOnlyList<PatrolZaloga> Zaloga => _zaloga;

        public Patrol(DateTime dataOd, DateTime dataDo, string kategoria, JednostkaPlywajaca jednostka)
        {
            if (dataOd > dataDo)
                throw new ArgumentException("Data rozpoczęcia nie może być późniejsza niż data zakończenia.");

            _dataOd = dataOd;
            _dataDo = dataDo;
            Kategoria = kategoria ?? throw new ArgumentNullException(nameof(kategoria));
            Jednostka = jednostka ?? throw new ArgumentNullException(nameof(jednostka));
        }

        public void DodajZaloge(PatrolZaloga zaloga)
        {
            if (zaloga == null)
                throw new ArgumentNullException(nameof(zaloga));
            _zaloga.Add(zaloga);
        }

        public void UsunZaloge(PatrolZaloga zaloga)
        {
            _zaloga.Remove(zaloga);
        }

        public void WyczyscZaloge()
        {
            _zaloga.Clear();
        }
    }
}
