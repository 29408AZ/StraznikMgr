using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ModulePatrol.Views
{
    public partial class ZalogaPodgladWindow : Window
    {
        public DateTime DataOd { get; set; } = DateTime.Now;
        public DateTime DataDo { get; set; } = DateTime.Now;
        public string GodzinaOd { get; set; } = "00:00";
        public string GodzinaDo { get; set; } = "00:00";
        public string Kategoria { get; set; } = string.Empty;
        public string Jednostka { get; set; } = string.Empty;
        public ObservableCollection<ZalogaPozycja> Zaloga { get; } = new();

        public ZalogaPodgladWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Load(ZalogaPodgladRequest request)
        {
            DataOd = request.DataOd;
            DataDo = request.DataDo;
            Kategoria = request.Kategoria;
            Jednostka = request.Jednostka;
            Zaloga.Clear();
            foreach (var z in request.Zaloga ?? Enumerable.Empty<ZalogaPozycja>())
                Zaloga.Add(z);
        }

        private void Zapisz_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // logika zapisu zostanie dodana później
        }

        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
