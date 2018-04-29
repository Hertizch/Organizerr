using Organizerr.Extensions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Organizerr.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Organizerr.Extensions.ObservableObject" />
    public class BaseViewModel : ObservableObject
    {
        private IPageViewModel _selectedPageViewModel;
        private ObservableCollection<IPageViewModel> _pageViewModels;
        private RelayCommand _changePageViewModelCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        public BaseViewModel()
        {
            PageViewModels = new ObservableCollection<IPageViewModel>
            {
                new RadarrViewModel(),
                new SonarrViewModel(),
                new SettingsViewModel()
            };

            SelectedPageViewModel = PageViewModels[0];
        }

        /// <summary>
        /// Gets the radarr view model.
        /// </summary>
        /// <value>
        /// The radarr view model.
        /// </value>
        public RadarrViewModel RadarrViewModel { get; }

        /// <summary>
        /// Gets the sonarr view model.
        /// </summary>
        /// <value>
        /// The sonarr view model.
        /// </value>
        public SonarrViewModel SonarrViewModel { get; }

        /// <summary>
        /// Gets the settings view model.
        /// </summary>
        /// <value>
        /// The settings view model.
        /// </value>
        public SettingsViewModel SettingsViewModel { get; }

        /// <summary>
        /// Gets or sets the selected page view model.
        /// </summary>
        /// <value>
        /// The selected page view model.
        /// </value>
        public IPageViewModel SelectedPageViewModel
        {
            get => _selectedPageViewModel; set { if (value == _selectedPageViewModel) return; _selectedPageViewModel = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the page view models.
        /// </summary>
        /// <value>
        /// The page view models.
        /// </value>
        public ObservableCollection<IPageViewModel> PageViewModels
        {
            get => _pageViewModels; set { if (value == _pageViewModels) return; _pageViewModels = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets the change page view model command.
        /// </summary>
        /// <value>
        /// The change page view model command.
        /// </value>
        public RelayCommand ChangePageViewModelCommand =>
            _changePageViewModelCommand ?? (_changePageViewModelCommand = new RelayCommand(p => Execute_ChangePageViewModelCommand((IPageViewModel)p), p => p is IPageViewModel));

        /// <summary>
        /// Executes the change page view model command.
        /// </summary>
        /// <param name="pageViewModel">The page view model.</param>
        private void Execute_ChangePageViewModelCommand(IPageViewModel pageViewModel)
        {
           if (SelectedPageViewModel != null && SelectedPageViewModel.Name == pageViewModel.Name)
            {
                Debug.WriteLine($"Execute_ChangePageViewModelCommand: {pageViewModel.Name}, IGNORED");
                return;
            }

            Debug.WriteLine($"Execute_ChangePageViewModelCommand: {pageViewModel.Name}");

            SelectedPageViewModel = PageViewModels.FirstOrDefault(vm => vm == pageViewModel);
        }
    }
}
