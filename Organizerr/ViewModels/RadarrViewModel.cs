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
        private ObservableCollection<RadarrSharp.Models.Movie> _movies;
        private ICollectionView _moviesView;
        private RelayCommand _getMoviesCommand;
        private RelayCommand _getProfilesCommand;
        private RelayCommand _toggleMonitoredStatusCommand;

        public RadarrViewModel()
        {
            RadarrClient = new RadarrClient("192.168.1.250", 7878, "43bdebe9f37b4d20bd5c37834ddb8ac2", "radarr")
            {
                WriteDebug = true
            };

            // Get profiles
            GetProfilesCommand.Execute(null);

            // Get movies
            GetMoviesCommand.Execute(null);

            // Set view
            MoviesView = CollectionViewSource.GetDefaultView(Movies);
            MoviesView.SortDescriptions.Add(new SortDescription(nameof(RadarrSharp.Models.Movie.SortTitle), ListSortDirection.Ascending));
            MoviesView.Filter = new Predicate<object>(Movies_OnFilter);
        }

        public string Name => "RadarrViewModel";

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

        public RadarrClient RadarrClient { get; set; }

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
            if (obj == null) return;

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
                foreach (var item in Movies)
                {
                    if (item.Id == updatedMovie.Id)
                    {
                        /*await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.Monitored = updatedMovie.Monitored;
                        });*/

                        item.Monitored = updatedMovie.Monitored;
                    }
                }

                if (updatedMovie.Monitored != oldValue)
                    Debug.WriteLine($"Got response from Radarr, Monitored property has changed from {oldValue} to {updatedMovie.Monitored} successfully");
                else
                    Debug.WriteLine($"Got response from Radarr, but Monitored property has not changed");
            }
            else
                Debug.WriteLine($"Got no response from Radarr, operation has failed");

            // Refresh view
            MoviesView.Refresh();

            ToggleMonitoredStatusIsBusy = false;
        }

        private bool Movies_OnFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchTerm)) return true;
            return obj is RadarrSharp.Models.Movie movie && movie.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
        }
    }
}
