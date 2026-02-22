namespace CommonUI.Models
{
    public class Swiadectwo : IEquatable<Swiadectwo>
    {
        public int MarynarzId { get; }
        public string Stanowisko { get; }
        public string Jednostka { get; }

        public Swiadectwo(int marynarzId, string stanowisko, string jednostka)
        {
            if (marynarzId <= 0)
                throw new ArgumentException("ID marynarza musi być większe od zera.");
            if (string.IsNullOrWhiteSpace(stanowisko))
                throw new ArgumentException("Stanowisko nie może być puste.");
            if (string.IsNullOrWhiteSpace(jednostka))
                throw new ArgumentException("Jednostka nie może być pusta.");

            MarynarzId = marynarzId;
            Stanowisko = stanowisko.Trim();
            Jednostka = jednostka.Trim();
        }

        public bool Equals(Swiadectwo? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return MarynarzId == other.MarynarzId &&
                   Stanowisko == other.Stanowisko &&
                   Jednostka == other.Jednostka;
        }

        public override bool Equals(object? obj) => Equals(obj as Swiadectwo);

        public override int GetHashCode() => HashCode.Combine(MarynarzId, Stanowisko, Jednostka);

        public static bool operator ==(Swiadectwo? left, Swiadectwo? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Swiadectwo? left, Swiadectwo? right) => !(left == right);
    }
}