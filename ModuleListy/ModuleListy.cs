using ModuleListy.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace ModuleListy
{
    public class ModuleListy : IModule
    {
        private readonly IRegionManager _regionManager;

        public ModuleListy(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Serwisy są rejestrowane centralnie w App.xaml.cs
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RegisterViewWithRegion("ShellWindowRegionModuleListy", typeof(ListView));
        }
    }
}