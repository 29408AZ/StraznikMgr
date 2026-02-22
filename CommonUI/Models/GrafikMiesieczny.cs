namespace CommonUI.Models
{
    public record GrafikMiesieczny
    {
        public decimal GodzinyPoczatek { get; init; }
        public decimal GodzinyKoniec { get; init; }
        public IReadOnlyDictionary<int, string> Sluzby { get; init; } = new Dictionary<int, string>();

        public decimal SumaSluzba { get; init; }
        public decimal SumaDyzur { get; init; }
        public decimal SumaUrlop { get; init; }
        public decimal SumaOddelegowanie { get; init; }
        public decimal SumaL4 { get; init; }
    }
}
