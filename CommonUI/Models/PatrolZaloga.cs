using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUI.Models
{
    public class PatrolZaloga
    {
        public string Stanowisko { get; }
        public Marynarz Marynarz { get; }
        public PatrolZaloga(string stanowisko, Marynarz marynarz)
        {
            Stanowisko = stanowisko ?? throw new ArgumentNullException(nameof(stanowisko));
            Marynarz = marynarz ?? throw new ArgumentNullException(nameof (marynarz));
        }
    }
}
