using ModuleEdycja.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace ModuleEdycja
{
    public class ModuleEdycja : IModule
    {
        private readonly IRegionManager _regionManager;

        public ModuleEdycja(IRegionManager regionManager)
        {
            _regionManager = regionManager;  
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Rejestracja głównego widoku z regionem w ShellWindow
            _regionManager.RegisterViewWithRegion("ShellWindowRegionModuleEdycja", typeof(EdycjaView));

            // Rejestracja widoków szczegółowych z regionami w EdycjaView
            _regionManager.RegisterViewWithRegion("SelectedViewRegion", typeof(SzczegolyMarynarzaView));
            _regionManager.RegisterViewWithRegion("SelectedViewRegion", typeof(SzczegolyJednostkiView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Serwisy są rejestrowane centralnie w App.xaml.cs
        }
    }
}
