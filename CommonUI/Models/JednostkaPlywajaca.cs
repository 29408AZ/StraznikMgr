namespace CommonUI.Models
{
    public class JednostkaPlywajaca : IEquatable<JednostkaPlywajaca>
    {
        public string Kategoria { get; }
        public string NumerBurtowy { get; }

        public JednostkaPlywajaca(string kategoria, string numerBurtowy)
        {
            Walidacja(kategoria, numerBurtowy);
            Kategoria = kategoria.Trim();
            NumerBurtowy = numerBurtowy.Trim();
        }

        public string Jednostki => $"{NumerBurtowy} ({Kategoria})";

        private static void Walidacja(string kategoria, string numerBurtowy)
        {
            if (string.IsNullOrWhiteSpace(kategoria))
                throw new ArgumentException("Kategoria jednostki nie może być pusta.", nameof(kategoria));
            if (string.IsNullOrWhiteSpace(numerBurtowy))
                throw new ArgumentException("Numer burtowy nie może być pusty.", nameof(numerBurtowy));
        }

        public override string ToString() => Jednostki;

        public bool Equals(JednostkaPlywajaca? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Kategoria == other.Kategoria && NumerBurtowy == other.NumerBurtowy;
        }

        public override bool Equals(object? obj) => Equals(obj as JednostkaPlywajaca);

        public override int GetHashCode() => HashCode.Combine(Kategoria, NumerBurtowy);

        public static bool operator ==(JednostkaPlywajaca? left, JednostkaPlywajaca? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(JednostkaPlywajaca? left, JednostkaPlywajaca? right) => !(left == right);
    }
}