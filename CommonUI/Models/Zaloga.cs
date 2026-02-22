namespace CommonUI.Models
{
    public class Zaloga : IEquatable<Zaloga>
    {
        public string Kategoria { get; }
        public string Stanowisko { get; }
        public string Zalogi => ToString();

        public Zaloga(string kategoria, string stanowisko)
        {
            WalidacjaZalogi(kategoria, stanowisko);
            Kategoria = kategoria.Trim();
            Stanowisko = stanowisko.Trim();
        }

        private static void WalidacjaZalogi(string kategoria, string stanowisko)
        {
            if (string.IsNullOrWhiteSpace(kategoria))
                throw new ArgumentException("Kategoria jednostki nie może być pusta.", nameof(kategoria));
            if (string.IsNullOrWhiteSpace(stanowisko))
                throw new ArgumentException("Stanowisko nie może być puste.", nameof(stanowisko));
        }

        public override string ToString() => Stanowisko;

        public bool Equals(Zaloga? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Kategoria, other.Kategoria, StringComparison.OrdinalIgnoreCase) 
                && string.Equals(Stanowisko, other.Stanowisko, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as Zaloga);

        public override int GetHashCode() => HashCode.Combine(
            Kategoria.ToUpperInvariant(), 
            Stanowisko.ToUpperInvariant());

        public static bool operator ==(Zaloga? left, Zaloga? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Zaloga? left, Zaloga? right) => !(left == right);
    }
}