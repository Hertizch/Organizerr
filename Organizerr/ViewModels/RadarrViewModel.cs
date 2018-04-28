using Organizerr.Extensions;
using RadarrSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.DataGrid;

namespace Organizerr.ViewModels
{
    public class RadarrViewModel : ObservableObject, IPageViewModel
    {
        private string _searchTerm;
        private bool _toggleMonitoredStatusIsBusy;
        private int _unmonitorWhereCutoffMetCountProcessed;
        private int _unmonitorWhereCutoffMetCountTotal;
        private ObservableCollection<RadarrSharp.Models.Movie> _movies;
        private ICollectionView _moviesView;
        private RelayCommand _getMoviesCommand;
        private RelayCommand _getProfilesCommand;
        private RelayCommand _toggleMonitoredStatusCommand;
        private RelayCommand _unmonitorWhereCutoffMetCommand;
        private RelayCommand _openRadarrPageCommand;
        private RelayCommand _automaticMovieSearchCommand;
        private RelayCommand _searchForMissingMoviesCommand;
        private RelayCommand _refreshMovieCommand;

        public RadarrViewModel()
        {
            RadarrClient = new RadarrClient("192.168.1.250", 7878, "43bdebe9f37b4d20bd5c37834ddb8ac2", "radarr")
            {
                WriteDebug = true
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

            // Set view
            MoviesView = CollectionViewSource.GetDefaultView(Movies);
            MoviesView.SortDescriptions.Add(new SortDescription(nameof(RadarrSharp.Models.Movie.SortTitle), ListSortDirection.Ascending));
            MoviesView.Filter = new Predicate<object>(Movies_OnFilter);
        }

        public string Name => "RADARR";

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (value == _searchTerm) return; _searchTerm = value; OnPropertyChanged(); MoviesView.Refresh();
            }
        }

        public bool ToggleMonitoredStatusIsBusy
        {
            get => _toggleMonitoredStatusIsBusy;
            set
            {
                if (value == _toggleMonitoredStatusIsBusy) return; _toggleMonitoredStatusIsBusy = value; OnPropertyChanged();
            }
        }

        public int UnmonitorWhereCutoffMetCountProcessed
        {
            get => _unmonitorWhereCutoffMetCountProcessed;
            set
            {
                if (value == _unmonitorWhereCutoffMetCountProcessed) return; _unmonitorWhereCutoffMetCountProcessed = value; OnPropertyChanged();
            }
        }

        public int UnmonitorWhereCutoffMetCountTotal
        {
            get => _unmonitorWhereCutoffMetCountTotal;
            set
            {
                if (value == _unmonitorWhereCutoffMetCountTotal) return; _unmonitorWhereCutoffMetCountTotal = value; OnPropertyChanged();
            }
        }

        public RadarrClient RadarrClient { get; set; }

        public string RadarrUrl { get; set; }

        public ObservableCollection<RadarrSharp.Models.Movie> Movies
        {
            get => _movies;
            set
            {
                if (value == _movies) return; _movies = value; OnPropertyChanged();
            }
        }

        public List<RadarrSharp.Models.Profile> Profiles { get; set; }

        public ICollectionView MoviesView
        {
            get => _moviesView;
            set
            {
                if (value == _moviesView) return; _moviesView = value; OnPropertyChanged();
            }
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

        private async void Execute_GetMoviesCommand(object obj)
        {
            if (Movies == null)
                Movies = new ObservableCollection<RadarrSharp.Models.Movie>();

            var movies = await RadarrClient.Movie.GetMovies();
            foreach (var item in movies)
                Movies.Add(item);
        }

        private async void Execute_GetProfilesCommand(object obj)
        {
            if (Profiles == null)
                Profiles = new List<RadarrSharp.Models.Profile>();

            var profiles = await RadarrClient.Profile.GetProfiles();
            foreach (var item in profiles)
                Profiles.Add(item);
        }

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

        private async void Execute_UnmonitorWhereCutoffMetCommand(object obj)
        {
            if (Movies.Count == 0)
                return;

            // Toggle status flag
            ToggleMonitoredStatusIsBusy = true;

            // Count total items to change
            UnmonitorWhereCutoffMetCountTotal = Movies.Where(m => m.HasFile && m.Monitored).Count(c => c.MovieFile.Quality.Quality.Id == Profiles.FirstOrDefault(p => p.Id == c.QualityProfileId).Cutoff.Id);

            Debug.WriteLine($"[INFO] Started operation to set 'Monitored' property to false for {UnmonitorWhereCutoffMetCountTotal} items");

            // Loop all movies that has HasFile (true) and Monitored (true)
            foreach (var item in Movies.Where(x => x.HasFile && x.Monitored))
            {
                // Get cutoff id
                var cutoffId = Profiles.FirstOrDefault(x => x.Id == item.QualityProfileId).Cutoff.Id;

                // If current movie file quality id equals cutoff id
                if (item.MovieFile.Quality.Quality.Id == cutoffId)
                {
                    // Remember current value
                    bool oldValue = item.Monitored;

                    // Set monitored to false
                    item.Monitored = false;

                    // Issue command and get updated movie object from Radarr
                    var updatedMovie = await RadarrClient.Movie.UpdateMovie(item);

                    // Check for successful return of new movie object
                    if (updatedMovie != null)
                    {
                        // Get new monitored property value from updated movie object
                        item.Monitored = updatedMovie.Monitored;

                        // Check for success
                        if (updatedMovie.Monitored != oldValue)
                        {
                            // Count as processed
                            UnmonitorWhereCutoffMetCountProcessed++;

                            Debug.WriteLine($"[SUCCESS] [{updatedMovie.Title} (ID: {updatedMovie.Id})] 'Monitored' property has changed from '{oldValue}' to '{updatedMovie.Monitored}' successfully");
                        }
                        else
                            Debug.WriteLine($"[ERROR] [{updatedMovie.Title} (ID: {updatedMovie.Id})] 'Monitored' property has failed to change");
                    }
                    else
                        Debug.WriteLine($"[ERROR] [{updatedMovie.Title} (ID: {updatedMovie.Id})] Got no response from Radarr, operation has failed");
                }
            }

            // Refresh view after all items are processed
            MoviesView.Refresh();

            // Toggle status flag
            ToggleMonitoredStatusIsBusy = false;

            Debug.WriteLine($"[INFO] Radarr has set 'Monitored' property to false for {UnmonitorWhereCutoffMetCountProcessed} of {UnmonitorWhereCutoffMetCountTotal} items");
        }

        private void Execute_OpenRadarrPageCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            Process.Start($"{RadarrClient.ApiUrl.Replace("/api", $"/movies/{movie.TitleSlug}")}");

            Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Open Radarr page: {RadarrClient.ApiUrl.Replace("/api", $"/movies/{movie.TitleSlug}")}");
        }

        private async void Execute_AutomaticMovieSearchCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            var command = await RadarrClient.Command.MoviesSearch(new int[] { int.Parse(movie.Id.ToString()) });
            if (command != null)
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got no response from Radarr, operation has failed");
        }

        private async void Execute_SearchForMissingMoviesCommand(object obj)
        {
            var command = await RadarrClient.Command.MissingMoviesSearch("status", "released");
            if (command != null)
                Debug.WriteLine($"[INFO] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] Got no response from Radarr, operation has failed");
        }

        private async void Execute_RefreshMovieCommand(object obj)
        {
            if (obj == null)
                return;

            // Get movie object
            var movie = (RadarrSharp.Models.Movie)obj;

            var command = await RadarrClient.Command.RefreshMovie(int.Parse(movie.Id.ToString()));
            if (command != null)
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Command '{command.Name}' (ID: {command.Id}) started successfully");
            else
                Debug.WriteLine($"[INFO] [{movie.Title} (ID: {movie.Id})] Got no response from Radarr, operation has failed");
        }

        private bool Movies_OnFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchTerm)) return true;

            var movie = (RadarrSharp.Models.Movie)obj;

            if (SearchTerm.StartsWith("id:"))
                return movie != null && movie.Id.ToString() == SearchTerm.Substring(3);

            return movie != null && movie.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
        }
    }
}
