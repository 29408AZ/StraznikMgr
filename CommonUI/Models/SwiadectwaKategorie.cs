using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUI.Models
{
    public class SwiadectwaKategorie
    {
        public ObservableCollection<Swiadectwo> SwiadectwaKatII { get; } = new();
        public ObservableCollection<Swiadectwo> SwiadectwaKatIII { get; } = new();
        public ObservableCollection<Swiadectwo> SwiadectwaKatIV { get; } = new();
    }
}
