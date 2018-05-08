using Organizerr.Enums;
using Organizerr.Extensions;
using Organizerr.Properties;
using RadarrSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Organizerr.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="ObservableObject" />
    /// <seealso cref="IPageViewModel" />
    public class RadarrViewModel : ObservableObject, IPageViewModel
    {
        // movie discovery
        private RadarrSharp.Models.RootFolder _selectedRootFolder;
        private RadarrSharp.Enums.MinimumAvailability _selectedMinimumAvailability;
        private bool _movieDiscoveryIsMonitored;
        private bool _movieDiscoverySearchForMovie;
        private string _movieDiscoverySearchTerm;
        private ObservableCollection<RadarrSharp.Models.Movie> _movieDiscoveryMovies;
        private ICollectionView _movieDiscoveryMoviesView;
        private bool _isMovieDiscoveryOverlayVisible;
        private bool _movieDiscoveryIsSearching;

        private string _searchTerm;
        private bool _toggleMonitoredStatusIsBusy;
        private bool _isSettingMonitoredToFalseForCutoffMet;
        private int _unmonitorWhereCutoffMetCountProcessed;
        private int _unmonitorWhereCutoffMetCountTotal;
        private ObservableCollection<RadarrSharp.Models.Movie> _movies;
        private ObservableCollection<RadarrSharp.Models.ExtraFile> _extraFiles;
        private ObservableCollection<RadarrSharp.Models.RootFolder> _rootFolders;
        private RadarrSharp.Models.Movie _selectedMovie;
        private RadarrSharp.Models.Profile _selectedProfile;
        private string _selectedMoviePosterUrl;
        private string _selectedMovieFanartUrl;
        private long _totalDiskUsage;
        private int _missingMovieCount;
        private RadarrSharp.Models.SystemStatus _systemStatus;
        private ICollectionView _moviesView;

        // filter
        private bool _filterOnStatus;
        private RadarrSharp.Enums.Status _filterOnStatusValue;
        private FilterValue _monitoredFilterValue;
        private FilterValue _downloadedFilterValue;

        // commands
        private RelayCommand _getRootFoldersCommand;
        private RelayCommand _getMoviesCommand;
        private RelayCommand _getProfilesCommand;
        private RelayCommand _toggleMonitoredStatusCommand;
        private RelayCommand _unmonitorWhereCutoffMetCommand;
        private RelayCommand _openRadarrPageCommand;
        private RelayCommand _automaticMovieSearchCommand;
        private RelayCommand _searchForMissingMoviesCommand;
        private RelayCommand _refreshMovieCommand;
        private RelayCommand _getExtraFilesCommand;
        private RelayCommand _openFolderPathCommand;
        private RelayCommand _getRadarrSystemInfo;
        private RelayCommand _deleteMovieCommand;
        private RelayCommand _clearSearchTermCommand;
        private RelayCommand _movieDiscoverySearchCommand;
        private RelayCommand _movieDiscoveryAddMovieCommand;
        private RelayCommand _getRadarrHealthCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadarrViewModel"/> class.
        /// </summary>
        public RadarrViewModel()
        {
            RadarrClient = new RadarrClient(Settings.Default.RadarrHost, Settings.Default.RadarrPort, Settings.Default.RadarrApiKey, Settings.Default.RadarrUrlBase)
            {
                WriteDebug = false
            };

            // Set Radarr URL
            var sb = new StringBuilder();
            sb.Append("http");
            if (RadarrClient.UseSsl) sb.Append("s");
            sb.Append($"://{RadarrClient.Host}:{RadarrClient.Port}");
            RadarrUrl = sb.ToString();

            RootFolders = new ObservableCollection<RadarrSharp.Models.RootFolder>();
            Profiles = new List<RadarrSharp.Models.Profile>();
            Movies = new ObservableCollection<RadarrSharp.Models.Movie>();
            MovieDiscoveryMovies = new ObservableCollection<RadarrSharp.Models.Movie>();
            ExtraFiles = new ObservableCollection<RadarrSharp.Models.ExtraFile>();

            // Get root folder path
            GetRootFoldersCommand.Execute(null);

            // Get profiles
            GetProfilesCommand.Execute(null);

            // Get movies
            GetMoviesCommand.Execute(null);

            // Get system info
            GetRadarrSystemInfo.Execute(null);

            // Set views
            MoviesView = CollectionViewSource.GetDefaultView(Movies);
            MoviesView.SortDescriptions.Add(new SortDescription(nameof(RadarrSharp.Models.Movie.SortTitle), ListSortDirection.Ascending));
            MoviesView.Filter = new Predicate<object>(Movies_OnFilter);

            MovieDiscoveryMoviesView = CollectionViewSource.GetDefaultView(MovieDiscoveryMovies);

            // design mode props
            //IsMovieDiscoveryOverlayVisible = true;
            //MovieDiscoveryIsSearching = true;
            //MovieDiscoverySearchTerm = "fifty";
            //MovieDiscoverySearchCommand.Execute(null);
        }

        public string Name => "RADARR";

        public RadarrSharp.Models.RootFolder SelectedRootFolder
        {
            get => _selectedRootFolder;
            set { if (value == _selectedRootFolder) return; _selectedRootFolder = value; OnPropertyChanged(); }
        }

        public RadarrSharp.Enums.MinimumAvailability SelectedMinimumAvailability
        {
            get => _selectedMinimumAvailability;
            set { if (value == _selectedMinimumAvailability) return; _selectedMinimumAvailability = value; OnPropertyChanged(); }
        }

        public bool MovieDiscoveryIsMonitored
        {
            get => _movieDiscoveryIsMonitored;
            set { if (value == _movieDiscoveryIsMonitored) return; _movieDiscoveryIsMonitored = value; OnPropertyChanged(); }
        }

        public bool MovieDiscoverySearchForMovie
        {
            get => _movieDiscoverySearchForMovie;
            set { if (value == _movieDiscoverySearchForMovie) return; _movieDiscoverySearchForMovie = value; OnPropertyChanged(); }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set { if (value == _searchTerm) return; _searchTerm = value; OnPropertyChanged(); MoviesView.Refresh(); }
        }

        public bool IsMovieDiscoveryOverlayVisible
        {
            get => _isMovieDiscoveryOverlayVisible;
            set { if (value == _isMovieDiscoveryOverlayVisible) return; _isMovieDiscoveryOverlayVisible = value; OnPropertyChanged(); }
        }

        public bool MovieDiscoveryIsSearching
        {
            get => _movieDiscoveryIsSearching;
            set { if (value == _movieDiscoveryIsSearching) return; _movieDiscoveryIsSearching = value; OnPropertyChanged(); }
        }

        public string MovieDiscoverySearchTerm
        {
            get => _movieDiscoverySearchTerm;
            set { if (value == _movieDiscoverySearchTerm) return; _movieDiscoverySearchTerm = value; OnPropertyChanged(); if (MovieDiscoverySearchCommand.CanExecute(!string.IsNullOrWhiteSpace(MovieDiscoverySearchTerm))) MovieDiscoverySearchCommand.Execute(null); }
        }

        public bool ToggleMonitoredStatusIsBusy
        {
            get => _toggleMonitoredStatusIsBusy;
            set { if (value == _toggleMonitoredStatusIsBusy) return; _toggleMonitoredStatusIsBusy = value; OnPropertyChanged(); }
        }

        public bool IsSettingMonitoredToFalseForCutoffMet
        {
            get => _isSettingMonitoredToFalseForCutoffMet;
            set { if (value == _isSettingMonitoredToFalseForCutoffMet) return; _isSettingMonitoredToFalseForCutoffMet = value; OnPropertyChanged(); }
        }

        public int UnmonitorWhereCutoffMetCountProcessed
        {
            get => _unmonitorWhereCutoffMetCountProcessed;
            set { if (value == _unmonitorWhereCutoffMetCountProcessed) return; _unmonitorWhereCutoffMetCountProcessed = value; OnPropertyChanged(); }
        }

        public int UnmonitorWhereCutoffMetCountTotal
        {
            get => _unmonitorWhereCutoffMetCountTotal;
            set { if (value == _unmonitorWhereCutoffMetCountTotal) return; _unmonitorWhereCutoffMetCountTotal = value; OnPropertyChanged(); }
        }

        public RadarrClient RadarrClient { get; set; }

        public string RadarrUrl { get; set; }

        public ObservableCollection<RadarrSharp.Models.Movie> Movies
        {
            get => _movies;
            set { if (value == _movies) return; _movies = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RadarrSharp.Models.ExtraFile> ExtraFiles
        {
            get => _extraFiles;
            set { if (value == _extraFiles) return; _extraFiles = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RadarrSharp.Models.Movie> MovieDiscoveryMovies
        {
            get => _movieDiscoveryMovies;
            set { if (value == _movieDiscoveryMovies) return; _movieDiscoveryMovies = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RadarrSharp.Models.RootFolder> RootFolders
        {
            get => _rootFolders;
            set { if (value == _rootFolders) return; _rootFolders = value; OnPropertyChanged(); }
        }

        public RadarrSharp.Models.Movie SelectedMovie
        {
            get => _selectedMovie;
            set { if (value == _selectedMovie) return; _selectedMovie = value; OnPropertyChanged(); SetSelectedMoveImageUrls(); }
        }

        public RadarrSharp.Models.Profile SelectedProfile
        {
            get => _selectedProfile;
            set { if (value == _selectedProfile) return; _selectedProfile = value; OnPropertyChanged(); }
        }

        public string SelectedMoviePosterUrl
        {
            get => _selectedMoviePosterUrl;
            set { if (value == _selectedMoviePosterUrl) return; _selectedMoviePosterUrl = value; OnPropertyChanged(); }
        }

        public string SelectedMovieFanartUrl
        {
            get => _selectedMovieFanartUrl;
            set { if (value == _selectedMovieFanartUrl) return; _selectedMovieFanartUrl = value; OnPropertyChanged(); }
        }

        public long TotalDiskUsage
        {
            get => _totalDiskUsage;
            set { if (value == _totalDiskUsage) return; _totalDiskUsage = value; OnPropertyChanged(); }
        }

        public int MissingMovieCount
        {
            get => _missingMovieCount;
            set { if (value == _missingMovieCount) return; _missingMovieCount = value; OnPropertyChanged(); }
        }

        public RadarrSharp.Models.SystemStatus SystemStatus
        {
            get => _systemStatus;
            set { if (value == _systemStatus) return; _systemStatus = value; OnPropertyChanged(); }
        }

        public List<RadarrSharp.Models.Profile> Profiles { get; set; }

        public ICollectionView MoviesView
        {
            get => _moviesView;
            set { if (value == _moviesView) return; _moviesView = value; OnPropertyChanged(); }
        }

        public ICollectionView MovieDiscoveryMoviesView
        {
            get => _movieDiscoveryMoviesView;
            set { if (value == _movieDiscoveryMoviesView) return; _movieDiscoveryMoviesView = value; OnPropertyChanged(); }
        }

        public bool FilterOnStatus
        {
            get => _filterOnStatus;
            set { if (value == _filterOnStatus) return; _filterOnStatus = value; OnPropertyChanged(); MoviesView.Refresh(); }
        }

        public RadarrSharp.Enums.Status FilterOnStatusValue
        {
            get => _filterOnStatusValue;
            set { if (value == _filterOnStatusValue) return; _filterOnStatusValue = value; OnPropertyChanged(); if (FilterOnStatus) MoviesView.Refresh(); }
        }

        public FilterValue MonitoredFilterValue
        {
            get => _monitoredFilterValue;
            set { if (value == _monitoredFilterValue) return; _monitoredFilterValue = value; OnPropertyChanged(); MoviesView.Refresh(); }
        }

        public FilterValue DownloadedFilterValue
        {
            get => _downloadedFilterValue;
            set { if (value == _downloadedFilterValue) return; _downloadedFilterValue = value; OnPropertyChanged(); MoviesView.Refresh(); }
        }


        /*
         * Commands
         */

        public RelayCommand GetRootFoldersCommand =>
            _getRootFoldersCommand ?? (_getRootFoldersCommand = new RelayCommand(Execute_GetRootFoldersCommand, p => true));

        public RelayCommand GetMoviesCommand =>
            _getMoviesCommand ?? (_getMoviesCommand = new RelayCommand(Execute_GetMoviesCommand, p => true));

        public RelayCommand GetProfilesCommand =>
            _getProfilesCommand ?? (_getProfilesCommand = new RelayCommand(Execute_GetProfilesCommand, p => true));

        public RelayCommand ToggleMonitoredStatusCommand =>
            _toggleMonitoredStatusCommand ?? (_toggleMonitoredStatusCommand = new RelayCommand(Execute_ToggleMonitoredStatusCommand, p => true));

        public RelayCommand UnmonitorWhereCutoffMetCommand =>
            _unmonitorWhereCutoffMetCommand ?? (_unmonitorWhereCutoffMetCommand = new RelayCommand(Execute_UnmonitorWhereCutoffMetCommand, p => true));

        public RelayCommand OpenRadarrPageCommand =>
            _openRadarrPageCommand ?? (_openRadarrPageCommand = new RelayCommand(Execute_OpenRadarrPageCommand, p => true));

        public RelayCommand AutomaticMovieSearchCommand =>
            _automaticMovieSearchCommand ?? (_automaticMovieSearchCommand = new RelayCommand(Execute_AutomaticMovieSearchCommand, p => true));

        public RelayCommand SearchForMissingMoviesCommand =>
            _searchForMissingMoviesCommand ?? (_searchForMissingMoviesCommand = new RelayCommand(Execute_SearchForMissingMoviesCommand, p => true));

        public RelayCommand RefreshMovieCommand =>
            _refreshMovieCommand ?? (_refreshMovieCommand = new RelayCommand(Execute_RefreshMovieCommand, p => true));

        public RelayCommand GetExtraFilesCommand =>
            _getExtraFilesCommand ?? (_getExtraFilesCommand = new RelayCommand(Execute_GetExtraFilesCommand, p => true));

        public RelayCommand OpenFolderPathCommand =>
            _openFolderPathCommand ?? (_openFolderPathCommand = new RelayCommand(Execute_OpenFolderPathCommand, p => true));

        public RelayCommand GetRadarrSystemInfo =>
            _getRadarrSystemInfo ?? (_getRadarrSystemInfo = new RelayCommand(Execute_GetRadarrSystemInfo, p => true));

        public RelayCommand DeleteMovieCommand =>
            _deleteMovieCommand ?? (_deleteMovieCommand = new RelayCommand(Execute_DeleteMovieCommand, p => true));

        public RelayCommand ClearSearchTermCommand =>
            _clearSearchTermCommand ?? (_clearSearchTermCommand = new RelayCommand(Execute_ClearSearchTermCommand, p => true));

        public RelayCommand MovieDiscoverySearchCommand =>
            _movieDiscoverySearchCommand ?? (_movieDiscoverySearchCommand = new RelayCommand(Execute_MovieDiscoverySearchCommand, p => true));

        public RelayCommand MovieDiscoveryAddMovieCommand =>
            _movieDiscoveryAddMovieCommand ?? (_movieDiscoveryAddMovieCommand = new RelayCommand(Execute_MovieDiscoveryAddMovieCommand, p => true));

        public RelayCommand GetRadarrHealthCommand =>
            _getRadarrHealthCommand ?? (_getRadarrHealthCommand = new RelayCommand(Execute_GetRadarrHealthCommand, p => true));


        /// <summary>
        /// Executes the get root folders command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetRootFoldersCommand(object obj)
        {
            IList<RadarrSharp.Models.RootFolder> rootFolders = new List<RadarrSharp.Models.RootFolder>();

            try
            {
                rootFolders = await RadarrClient.RootFolder.GetRootFolders();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_GetRootFolderPathCommand] Operation has failed - {ex.Message}");
            }

            // Add all items to collection
            if (rootFolders.Count > 0)
                foreach (var item in rootFolders)
                    RootFolders.Add(item);

            // Set first item as selected
            if (RootFolders.Count > 0)
            {
                SelectedRootFolder = rootFolders.FirstOrDefault();
                Debug.WriteLine($"[INFO] [Execute_GetRootFolderPathCommand] RootFolderPath set to '{SelectedRootFolder.Path}'");
            }
        }

        /// <summary>
        /// Executes the get movies command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetMoviesCommand(object obj)
        {
            IList<RadarrSharp.Models.Movie> movies = new List<RadarrSharp.Models.Movie>();

            try
            {
                movies = await RadarrClient.Movie.GetMovies();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_GetMoviesCommand] Operation has failed - {ex.Message}");
            }

            if (movies.Count > 0)
                foreach (var item in movies)
                {
                    Movies.Add(item);

                    // Calculate total disk usage
                    if (item.Downloaded && item.MovieFile != null)
                    {
                        long size = item.MovieFile.Size;
                        TotalDiskUsage += size == 0 ? 0 : size;
                    }

                    // calculate missing movie count
                    if (!item.Downloaded)
                        MissingMovieCount++;
                }
        }

        /// <summary>
        /// Executes the get profiles command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetProfilesCommand(object obj)
        {
            IList<RadarrSharp.Models.Profile> profiles = new List<RadarrSharp.Models.Profile>();

            try
            {
                profiles = await RadarrClient.Profile.GetProfiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_GetProfilesCommand] Operation has failed - {ex.Message}");
            }

            // Add all items to collection
            if (profiles.Count > 0)
                foreach (var item in profiles)
                    Profiles.Add(item);

            // Set first item as selected
            if (Profiles.Count > 0)
            {
                SelectedProfile = Profiles.FirstOrDefault();
                Debug.WriteLine($"[INFO] [Execute_GetProfilesCommand] SelectedProfile set to '{SelectedProfile.Name}'");
            }
        }

        /// <summary>
        /// Executes the toggle monitored status command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_ToggleMonitoredStatusCommand(object obj)
        {
            if (obj == null)
                return;

            // Toggle status flag
            ToggleMonitoredStatusIsBusy = true;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            // Remember current value
            bool oldValue = movie.Monitored;

            if (oldValue) // set to false if true
                movie.Monitored = false;
            else if (!oldValue) // set to true if false
                movie.Monitored = true;

            // Get updated movie object from Radarr
            var updatedMovie = await RadarrClient.Movie.UpdateMovie(movie);
            if (updatedMovie != null)
            {
                // Get new monitored property value from updated movie object
                foreach (var item in Movies)
                    if (item.Id == updatedMovie.Id)
                        item.Monitored = updatedMovie.Monitored;

                // Check for success
                if (updatedMovie.Monitored != oldValue)
                    Debug.WriteLine($"[SUCCESS] [{updatedMovie.Title} (ID: {updatedMovie.Id})] 'Monitored' property has changed from '{oldValue}' to '{updatedMovie.Monitored}' successfully");
                else
                    Debug.WriteLine($"[ERROR] [{updatedMovie.Title} (ID: {updatedMovie.Id})] 'Monitored' property has failed to change");
            }
            else
                Debug.WriteLine($"[ERROR] [{updatedMovie.Title} (ID: {updatedMovie.Id})] Got no response from Radarr, operation has failed");

            // Refresh view
            MoviesView.Refresh();

            // Toggle status flag
            ToggleMonitoredStatusIsBusy = false;
        }

        /// <summary>
        /// Executes the unmonitor where cutoff met command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_UnmonitorWhereCutoffMetCommand(object obj)
        {
            if (Movies.Count == 0)
                return;

            // Count total items to change - and return if zero
            UnmonitorWhereCutoffMetCountTotal = Movies.Where(m => m.HasFile && m.Monitored).Count(c => c.MovieFile.Quality.Quality.Id == Profiles.FirstOrDefault(p => p.Id == c.QualityProfileId).Cutoff.Id);
            if (UnmonitorWhereCutoffMetCountTotal == 0)
                return;

            // Show user dialog
            var dialog = new DialogWindow($"Unmonitor {UnmonitorWhereCutoffMetCountTotal} movies?", $"All movies where the cutoff quality is met will be set to unmonitored. Be aware that this process cannot be cancelled untill it is finished. Continue?", DialogWindow.DialogButtons.YesNo);
            if (!(bool)dialog.ShowDialog())
            {
                Debug.WriteLine($"[INFO] Operation to set 'Monitored' property to false for {UnmonitorWhereCutoffMetCountTotal} items where cancelled by user");

                // return
                return;
            }

            // Toggle status flag
            IsSettingMonitoredToFalseForCutoffMet = true;

            Debug.WriteLine($"[INFO] Started operation to set 'Monitored' property to false for {UnmonitorWhereCutoffMetCountTotal} items");

            var moviesToUpdate = new List<RadarrSharp.Models.Movie>();

            // Loop all movies that has HasFile (true) and Monitored (true)
            foreach (var item in Movies.Where(x => x.HasFile && x.Monitored))
            {
                // Get cutoff id
                var cutoffId = Profiles.FirstOrDefault(x => x.Id == item.QualityProfileId).Cutoff.Id;

                // If current movie file quality id equals cutoff id
                if (item.MovieFile.Quality.Quality.Id == cutoffId)
                {
                    // Set monitored to false
                    item.Monitored = false;

                    // Add
                    moviesToUpdate.Add(item);
                }
            }

            IList<RadarrSharp.Models.Movie> updatedMovies = new List<RadarrSharp.Models.Movie>();

            // Issue command and get updated movie object collection from Radarr
            try
            {
                updatedMovies = await RadarrClient.Movie.UpdateMovies(moviesToUpdate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_UnmonitorWhereCutoffMetCommand] Operation has failed. {ex.Message}");
            }

            // Check for success
            if (updatedMovies.Count != 0)
                Debug.WriteLine($"[SUCCESS] [Execute_UnmonitorWhereCutoffMetCommand] Operation has completed successfully");

            // Refresh view
            MoviesView.Refresh();

            // Toggle status flag
            IsSettingMonitoredToFalseForCutoffMet = false;

            Debug.WriteLine($"[INFO] Radarr has set 'Monitored' property to false for {UnmonitorWhereCutoffMetCountProcessed} of {UnmonitorWhereCutoffMetCountTotal} items");
        }

        /// <summary>
        /// Executes the open radarr page command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private void Execute_OpenRadarrPageCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            Process.Start($"{RadarrClient.ApiUrl.Replace("/api", $"/movies/{movie.TitleSlug}")}");

            Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Open Radarr page: {RadarrClient.ApiUrl.Replace("/api", $"/movies/{movie.TitleSlug}")}");
        }

        /// <summary>
        /// Executes the automatic movie search command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_AutomaticMovieSearchCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            var command = await RadarrClient.Command.MoviesSearch(new int[] { movie.Id });
            if (command != null)
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got no response from Radarr, operation has failed");
        }

        /// <summary>
        /// Executes the search for missing movies command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_SearchForMissingMoviesCommand(object obj)
        {
            // Run command
            var command = await RadarrClient.Command.MissingMoviesSearch("status", "released");

            // Check for success
            if (command != null)
                Debug.WriteLine($"[INFO] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] Got no response from Radarr, operation has failed");
        }

        /// <summary>
        /// Executes the refresh movie command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_RefreshMovieCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            // Run command
            var command = await RadarrClient.Command.RefreshMovie(movie.Id);

            // Check for success
            if (command != null)
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got no response from Radarr, operation has failed");
        }

        /// <summary>
        /// Executes the get extra files command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetExtraFilesCommand(object obj)
        {
            if (obj == null)
                return;

            // Clear collection
            ExtraFiles.Clear();

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            // Skip if not downloaded
            if (!movie.Downloaded)
                return;

            // Get extra files object collection
            var extraFiles = await RadarrClient.ExtraFile.GetExtraFiles(movie.Id);

            // Check for success
            if (extraFiles != null)
            {
                // Add to extra files collection
                foreach (var item in extraFiles)
                    ExtraFiles.Add(item);
            }
            else
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got no response from Radarr, operation has failed");
        }

        /// <summary>
        /// Executes the open folder path command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private void Execute_OpenFolderPathCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            // Set path to run in process
            var path = $"\\\\{RadarrClient.Host}{movie.Path}";

            Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Open path: {path}");

            if (Directory.Exists(path))
                Process.Start(path);
        }

        /// <summary>
        /// Executes the get radarr system information.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetRadarrSystemInfo(object obj)
        {
            try
            {
                SystemStatus = await RadarrClient.SystemStatus.GetSystemStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INFO] [Execute_GetRadarrSystemInfo] Operation has failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the delete movie command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_DeleteMovieCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            // Show user dialog
            var dialog = new DialogWindow($"Delete {movie.Title}?", $"This will remove the movie from Radarr. It will not touch any files.", DialogWindow.DialogButtons.OkCancel);
            if (!(bool)dialog.ShowDialog())
            {
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Cancelled by user");

                // return
                return;
            }

            try
            {
                await RadarrClient.Movie.DeleteMovie(movie.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Operation has failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the movie discovery search command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_MovieDiscoverySearchCommand(object obj)
        {
            if (string.IsNullOrWhiteSpace(MovieDiscoverySearchTerm))
            {
                if (MovieDiscoveryMovies.Count > 0)
                    MovieDiscoveryMovies.Clear();

                return;
            }
                
            if (MovieDiscoveryMovies.Count > 0)
                MovieDiscoveryMovies.Clear();

            IList<RadarrSharp.Models.Movie> movies = new List<RadarrSharp.Models.Movie>();

            Debug.WriteLine($"[INFO] [Execute_MovieDiscoverySearchCommand] Started search with term: '{MovieDiscoverySearchTerm}'");

            MovieDiscoveryIsSearching = true;

            try
            {
                movies = await RadarrClient.Movie.SearchForMovie(MovieDiscoverySearchTerm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INFO] [Execute_MovieDiscoverySearchCommand] Operation has failed - {ex.Message}");
            }

            if (movies.Count > 0)
                foreach (var item in movies)
                    MovieDiscoveryMovies.Add(item);
            else
                Debug.WriteLine($"[INFO] [Execute_MovieDiscoverySearchCommand] Got no results from Radarr on search term: '{MovieDiscoverySearchTerm}'");

            MovieDiscoveryIsSearching = false;
        }

        /// <summary>
        /// Executes the movie discovery add movie command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_MovieDiscoveryAddMovieCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var discoveryMovie = (RadarrSharp.Models.Movie)obj;

            var newMovie = new RadarrSharp.Models.Movie();

            try
            {
                newMovie = await RadarrClient.Movie.AddMovie(
                    discoveryMovie.Title,
                    int.Parse(discoveryMovie.Year.ToString()),
                    int.Parse(SelectedProfile.Id.ToString()),
                    discoveryMovie.TitleSlug,
                    new List<RadarrSharp.Models.Image>(discoveryMovie.Images),
                    int.Parse(discoveryMovie.TmdbId.ToString()),
                    SelectedRootFolder.Path,
                    SelectedMinimumAvailability,
                    MovieDiscoveryIsMonitored,
                    new RadarrSharp.Endpoints.Movie.AddOptions { SearchForMovie = MovieDiscoverySearchForMovie });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_AddMovieFromDiscovery] Operation has failed. {ex.Message}");
            }

            if (newMovie != null)
            {
                Movies.Add(newMovie);

                // Refresh views
                MoviesView.Refresh();
                MovieDiscoveryMoviesView.Refresh();
            }
        }

        /// <summary>
        /// Executes the clear search term command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private void Execute_ClearSearchTermCommand(object obj)
        {
            SearchTerm = string.Empty;
        }

        /// <summary>
        /// Executes the get radarr health command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetRadarrHealthCommand(object obj)
        {
            Debug.WriteLine($"Event fired");

            
        }

        /// <summary>
        /// Sets the selected move image urls.
        /// </summary>
        private void SetSelectedMoveImageUrls()
        {
            if (SelectedMovie == null)
                return;

            // reset before selecting new images
            SelectedMoviePosterUrl = null;
            SelectedMovieFanartUrl = null;

            if (Settings.Default.ShowPoster && SelectedMovie.Images.Any(x => x.CoverType == RadarrSharp.Enums.CoverType.Poster))
                SelectedMoviePosterUrl = $"{RadarrUrl}{SelectedMovie.Images.FirstOrDefault(x => x.CoverType == RadarrSharp.Enums.CoverType.Poster).Url}";

            if (Settings.Default.ShowFanart && SelectedMovie.Images.Any(x => x.CoverType == RadarrSharp.Enums.CoverType.FanArt))
                SelectedMovieFanartUrl = $"{RadarrUrl}{SelectedMovie.Images.FirstOrDefault(x => x.CoverType == RadarrSharp.Enums.CoverType.FanArt).Url}";
        }

        private bool Movies_OnFilter(object obj)
        {
            if (Movies.Count == 0) return false;

            var movie = (RadarrSharp.Models.Movie)obj;

            // hide downloaded
            if (DownloadedFilterValue == FilterValue.Hide && movie.Downloaded)
                return false;

            // hide unmonitored
            if (MonitoredFilterValue == FilterValue.Hide && movie.Monitored)
                return false;

            // filter on status
            if (FilterOnStatus)
                if (FilterOnStatusValue != movie.Status)
                    return false;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                // radarr id
                if (SearchTerm.StartsWith("id:"))
                    return movie.Id.ToString() == SearchTerm.Substring("id:".Length);

                // imdb
                if (SearchTerm.StartsWith("imdb:"))
                    return movie.ImdbId.ToString() == SearchTerm.Substring("imdb:".Length);

                // the movie database
                if (SearchTerm.StartsWith("tmdb:"))
                    return movie.TmdbId.ToString() == SearchTerm.Substring("tmdb:".Length);

                // search term
                return movie.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}
