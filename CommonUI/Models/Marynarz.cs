namespace CommonUI.Models
{
    public class Marynarz : IEquatable<Marynarz>
    {
        public int Id { get; }
        private string _nazwa = string.Empty;
        public string Nazwa
        {
            get => _nazwa;
            set => _nazwa = WalidacjaNazwy(value);
        }

        private readonly Dictionary<DateTime, string> _sluzby = new();
        public IReadOnlyDictionary<DateTime, string> Sluzby => _sluzby;

        public record BilansGodzin
        {
            public decimal GodzinyPoczatek { get; init; }
            public decimal GodzinyKoniec { get; init; }
            public decimal Sluzba { get; init; }
            public decimal Dyzur { get; init; }
            public decimal Urlop { get; init; }
            public decimal Oddelegowanie { get; init; }
            public decimal L4 { get; init; }

            public static BilansGodzin Create(
                decimal godzinyPoczatek,
                decimal godzinyKoniec,
                decimal sluzba = 0,
                decimal dyzur = 0,
                decimal urlop = 0,
                decimal oddelegowanie = 0,
                decimal l4 = 0)
            {
                return new BilansGodzin
                {
                    GodzinyPoczatek = godzinyPoczatek,
                    GodzinyKoniec = godzinyKoniec,
                    Sluzba = sluzba,
                    Dyzur = dyzur,
                    Urlop = urlop,
                    Oddelegowanie = oddelegowanie,
                    L4 = l4
                };
            }
        }

        private readonly Dictionary<string, BilansGodzin> _bilanseMiesiecy = new();
        public IReadOnlyDictionary<string, BilansGodzin> BilanseMiesiecy => _bilanseMiesiecy;

        public decimal StanNaKoniecOkresu { get; private set; }
        public string Funkcjonariusze => Nazwa;

        public Marynarz(int id, string nazwa)
        {
            WalidacjaId(id);
            Id = id;
            Nazwa = nazwa;
        }

        public void UstawStanNaKoniecOkresu(decimal stan)
        {
            StanNaKoniecOkresu = stan;
        }

        public void DodajSluzbe(DateTime data, string opis)
        {
            if (string.IsNullOrWhiteSpace(opis))
                throw new ArgumentException("Opis służby nie może być pusty.", nameof(opis));
            _sluzby[data] = opis;
        }

        public void DodajBilansMiesieczny(
            string miesiac,
            decimal godzinyPoczatek,
            decimal godzinyKoniec,
            decimal sluzba = 0,
            decimal dyzur = 0,
            decimal urlop = 0,
            decimal oddelegowanie = 0,
            decimal l4 = 0)
        {
            if (string.IsNullOrWhiteSpace(miesiac))
                throw new ArgumentException("Nazwa miesiąca nie może być pusta.", nameof(miesiac));

            _bilanseMiesiecy[miesiac.ToUpper()] = BilansGodzin.Create(
                godzinyPoczatek, godzinyKoniec, sluzba, dyzur, urlop, oddelegowanie, l4);
        }

        public BilansGodzin? GetBilansMiesieczny(string miesiac)
        {
            return _bilanseMiesiecy.TryGetValue(miesiac.ToUpper(), out var bilans) ? bilans : null;
        }

        private static string WalidacjaNazwy(string nazwa)
        {
            if (string.IsNullOrWhiteSpace(nazwa))
                throw new ArgumentException("Nazwa marynarza nie może być pusta.");
            return nazwa.Trim();
        }

        private static void WalidacjaId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID musi być większe od 0.");
        }

        public bool Equals(Marynarz? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Marynarz);
        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(Marynarz? left, Marynarz? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Marynarz? left, Marynarz? right) => !(left == right);
    }
}