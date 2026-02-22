using ModulePatrol.Views;
using Prism.Ioc;
using System;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace ModulePatrol
{
    public class ModulePatrol : IModule
    {
        private readonly IRegionManager _regionManager;

        public ModulePatrol(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RegisterViewWithRegion("ShellWindowRegionModulePatrol", typeof(PatrolView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Serwisy są rejestrowane centralnie w App.xaml.cs
            Func<ZalogaPodgladRequest, bool?> factory = request =>
            {
                var window = new Views.ZalogaPodgladWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                window.Load(request);
                return window.ShowDialog();
            };
            containerRegistry.RegisterInstance(factory);
        }
    }
}
