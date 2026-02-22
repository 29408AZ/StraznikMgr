using System;
using System.Collections.Generic;

namespace ModulePatrol.Views
{
    public class ZalogaPozycja
    {
        public string Stanowisko { get; set; } = string.Empty;
        public string Marynarz { get; set; } = string.Empty;
    }

    public class ZalogaPodgladRequest
    {
        public DateTime DataOd { get; set; }
        public DateTime DataDo { get; set; }
        public string Kategoria { get; set; } = string.Empty;
        public string Jednostka { get; set; } = string.Empty;
        public List<ZalogaPozycja> Zaloga { get; set; } = new();
    }
}
