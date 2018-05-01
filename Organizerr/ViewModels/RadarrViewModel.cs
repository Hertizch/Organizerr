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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Organizerr.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Organizerr.Extensions.ObservableObject" />
    /// <seealso cref="Organizerr.ViewModels.IPageViewModel" />
    public class RadarrViewModel : ObservableObject, IPageViewModel
    {
        private string _searchTerm;
        private bool _isAddMovieOverlayVisible;
        private string _addMovieSearchTerm;
        private bool _toggleMonitoredStatusIsBusy;
        private bool _isSettingMonitoredToFalseForCutoffMet;
        private int _unmonitorWhereCutoffMetCountProcessed;
        private int _unmonitorWhereCutoffMetCountTotal;
        private ObservableCollection<RadarrSharp.Models.Movie> _movies;
        private ObservableCollection<RadarrSharp.Models.ExtraFile> _extraFiles;
        private ObservableCollection<RadarrSharp.Models.Movie> _addMovies;
        private RadarrSharp.Models.Movie _selectedMovie;
        private string _selectedMoviePosterUrl;
        private string _selectedMovieFanartUrl;
        private long _totalDiskUsage;
        private int _missingMovieCount;
        private RadarrSharp.Models.SystemStatus _systemStatus;
        private ICollectionView _moviesView;

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
        private RelayCommand _addMovieSearchCommand;

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

            // Get profiles
            GetProfilesCommand.Execute(null);

            // Get movies
            GetMoviesCommand.Execute(null);

            // Get system info
            GetRadarrSystemInfo.Execute(null);

            // Set view
            MoviesView = CollectionViewSource.GetDefaultView(Movies);
            MoviesView.SortDescriptions.Add(new SortDescription(nameof(RadarrSharp.Models.Movie.SortTitle), ListSortDirection.Ascending));
            MoviesView.Filter = new Predicate<object>(Movies_OnFilter);

            /*IsAddMovieOverlayVisible = true;
            AddMovieSearchTerm = "matrix";
            AddMovieSearchCommand.Execute(null);*/
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => "RADARR";

        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        /// <value>
        /// The search term.
        /// </value>
        public string SearchTerm
        {
            get => _searchTerm;
            set { if (value == _searchTerm) return; _searchTerm = value; OnPropertyChanged(); MoviesView.Refresh(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is add movie overlay visible.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is add movie overlay visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsAddMovieOverlayVisible
        {
            get => _isAddMovieOverlayVisible;
            set { if (value == _isAddMovieOverlayVisible) return; _isAddMovieOverlayVisible = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the add movie search term.
        /// </summary>
        /// <value>
        /// The add movie search term.
        /// </value>
        public string AddMovieSearchTerm
        {
            get => _addMovieSearchTerm;
            set { if (value == _addMovieSearchTerm) return; _addMovieSearchTerm = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [toggle monitored status is busy].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [toggle monitored status is busy]; otherwise, <c>false</c>.
        /// </value>
        public bool ToggleMonitoredStatusIsBusy
        {
            get => _toggleMonitoredStatusIsBusy;
            set { if (value == _toggleMonitoredStatusIsBusy) return; _toggleMonitoredStatusIsBusy = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is setting monitored to false for cutoff met.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is setting monitored to false for cutoff met; otherwise, <c>false</c>.
        /// </value>
        public bool IsSettingMonitoredToFalseForCutoffMet
        {
            get => _isSettingMonitoredToFalseForCutoffMet;
            set { if (value == _isSettingMonitoredToFalseForCutoffMet) return; _isSettingMonitoredToFalseForCutoffMet = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the unmonitor where cutoff met count processed.
        /// </summary>
        /// <value>
        /// The unmonitor where cutoff met count processed.
        /// </value>
        public int UnmonitorWhereCutoffMetCountProcessed
        {
            get => _unmonitorWhereCutoffMetCountProcessed;
            set { if (value == _unmonitorWhereCutoffMetCountProcessed) return; _unmonitorWhereCutoffMetCountProcessed = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the unmonitor where cutoff met count total.
        /// </summary>
        /// <value>
        /// The unmonitor where cutoff met count total.
        /// </value>
        public int UnmonitorWhereCutoffMetCountTotal
        {
            get => _unmonitorWhereCutoffMetCountTotal;
            set { if (value == _unmonitorWhereCutoffMetCountTotal) return; _unmonitorWhereCutoffMetCountTotal = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the radarr client.
        /// </summary>
        /// <value>
        /// The radarr client.
        /// </value>
        public RadarrClient RadarrClient { get; set; }

        /// <summary>
        /// Gets or sets the radarr URL.
        /// </summary>
        /// <value>
        /// The radarr URL.
        /// </value>
        public string RadarrUrl { get; set; }

        /// <summary>
        /// Gets or sets the movies.
        /// </summary>
        /// <value>
        /// The movies.
        /// </value>
        public ObservableCollection<RadarrSharp.Models.Movie> Movies
        {
            get => _movies;
            set { if (value == _movies) return; _movies = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the extra files.
        /// </summary>
        /// <value>
        /// The extra files.
        /// </value>
        public ObservableCollection<RadarrSharp.Models.ExtraFile> ExtraFiles
        {
            get => _extraFiles;
            set { if (value == _extraFiles) return; _extraFiles = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the add movies.
        /// </summary>
        /// <value>
        /// The add movies.
        /// </value>
        public ObservableCollection<RadarrSharp.Models.Movie> AddMovies
        {
            get => _addMovies;
            set { if (value == _addMovies) return; _addMovies = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the selected movie.
        /// </summary>
        /// <value>
        /// The selected movie.
        /// </value>
        public RadarrSharp.Models.Movie SelectedMovie
        {
            get => _selectedMovie;
            set { if (value == _selectedMovie) return; _selectedMovie = value; OnPropertyChanged(); SetSelectedMoveImageUrls(); }
        }

        /// <summary>
        /// Gets or sets the selected movie poster URL.
        /// </summary>
        /// <value>
        /// The selected movie poster URL.
        /// </value>
        public string SelectedMoviePosterUrl
        {
            get => _selectedMoviePosterUrl;
            set { if (value == _selectedMoviePosterUrl) return; _selectedMoviePosterUrl = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the selected movie fanart URL.
        /// </summary>
        /// <value>
        /// The selected movie fanart URL.
        /// </value>
        public string SelectedMovieFanartUrl
        {
            get => _selectedMovieFanartUrl;
            set { if (value == _selectedMovieFanartUrl) return; _selectedMovieFanartUrl = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the total disk usage.
        /// </summary>
        /// <value>
        /// The total disk usage.
        /// </value>
        public long TotalDiskUsage
        {
            get => _totalDiskUsage;
            set { if (value == _totalDiskUsage) return; _totalDiskUsage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the missing movie count.
        /// </summary>
        /// <value>
        /// The missing movie count.
        /// </value>
        public int MissingMovieCount
        {
            get => _missingMovieCount;
            set { if (value == _missingMovieCount) return; _missingMovieCount = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the system status.
        /// </summary>
        /// <value>
        /// The system status.
        /// </value>
        public RadarrSharp.Models.SystemStatus SystemStatus
        {
            get => _systemStatus;
            set { if (value == _systemStatus) return; _systemStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the profiles.
        /// </summary>
        /// <value>
        /// The profiles.
        /// </value>
        public List<RadarrSharp.Models.Profile> Profiles { get; set; }

        /// <summary>
        /// Gets or sets the movies view.
        /// </summary>
        /// <value>
        /// The movies view.
        /// </value>
        public ICollectionView MoviesView
        {
            get => _moviesView;
            set { if (value == _moviesView) return; _moviesView = value; OnPropertyChanged(); }
        }


        /*
         * Commands
         */

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

        public RelayCommand AddMovieSearchCommand =>
            _addMovieSearchCommand ?? (_addMovieSearchCommand = new RelayCommand(Execute_AddMovieSearchCommand, p => true));

        /// <summary>
        /// Executes the get movies command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_GetMoviesCommand(object obj)
        {
            // Initialize collection if null
            if (Movies == null)
                Movies = new ObservableCollection<RadarrSharp.Models.Movie>();

            IList<RadarrSharp.Models.Movie> movies = new List<RadarrSharp.Models.Movie>();

            try
            {
                movies = await RadarrClient.Movie.GetMovies();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_GetMoviesCommand] Operation has failed - {ex.Message}");
            }

            if (movies != null)
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
            // Initialize collection if null
            if (Profiles == null)
                Profiles = new List<RadarrSharp.Models.Profile>();

            IList<RadarrSharp.Models.Profile> profiles = new List<RadarrSharp.Models.Profile>();

            try
            {
                profiles = await RadarrClient.Profile.GetProfiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] [Execute_GetProfilesCommand] Operation has failed - {ex.Message}");
            }

            if (profiles != null)
                foreach (var item in profiles)
                    Profiles.Add(item);
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

            // Initialize collection if null
            if (ExtraFiles == null)
                ExtraFiles = new ObservableCollection<RadarrSharp.Models.ExtraFile>();

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

                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got {extraFiles.Count} extra files");
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
            SystemStatus = await RadarrClient.SystemStatus.GetSystemStatus();
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
        /// Executes the add movie search command.
        /// </summary>
        /// <param name="obj">The object.</param>
        private async void Execute_AddMovieSearchCommand(object obj)
        {
            if (string.IsNullOrWhiteSpace(AddMovieSearchTerm))
                return;

            if (AddMovies == null)
                AddMovies = new ObservableCollection<RadarrSharp.Models.Movie>();

            if (AddMovies.Count > 0)
                AddMovies.Clear();

            var movies = await RadarrClient.Movie.SearchForMovie(AddMovieSearchTerm);

            if (movies != null)
                foreach (var item in movies)
                    AddMovies.Add(item);
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
        /// Sets the selected move image urls.
        /// </summary>
        private void SetSelectedMoveImageUrls()
        {
            if (SelectedMovie == null)
                return;

            // does not work as images are never null
            if (!SelectedMovie.Images.Any(x => x.CoverType == RadarrSharp.Enums.CoverType.Poster))
                SelectedMoviePosterUrl = $"{RadarrUrl}/Content/Images/poster-dark.png";

            SelectedMoviePosterUrl = $"{RadarrUrl}{SelectedMovie.Images.FirstOrDefault(x => x.CoverType == RadarrSharp.Enums.CoverType.Poster).Url}";
            SelectedMovieFanartUrl = $"{RadarrUrl}{SelectedMovie.Images.FirstOrDefault(x => x.CoverType == RadarrSharp.Enums.CoverType.FanArt).Url}";
        }

        private bool Movies_OnFilter(object obj)
        {
            var movie = (RadarrSharp.Models.Movie)obj;

            // if no text in search term
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return true;

            // radarr id
            if (SearchTerm.StartsWith("id:"))
                return movie != null && movie.Id.ToString() == SearchTerm.Substring("id:".Length);

            // imdb
            if (SearchTerm.StartsWith("imdb:"))
                return movie != null && movie.ImdbId.ToString() == SearchTerm.Substring("imdb:".Length);

            // the movie database
            if (SearchTerm.StartsWith("tmdb:"))
                return movie != null && movie.TmdbId.ToString() == SearchTerm.Substring("tmdb:".Length);

            return movie != null && movie.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
        }
    }
}
