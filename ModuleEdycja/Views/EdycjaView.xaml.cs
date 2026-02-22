using ModuleEdycja.ViewModels;
using System.Windows.Controls;

namespace ModuleEdycja.Views
{
    public partial class EdycjaView : UserControl
    {
        public EdycjaView(SzczegolyViewModel edycjaVM)
        {
            InitializeComponent();
            DataContext = edycjaVM;
        }
    }
}
