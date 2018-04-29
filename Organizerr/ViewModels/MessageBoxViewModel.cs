using Organizerr.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organizerr.ViewModels
{
    public class MessageBoxViewModel : ObservableObject
    {
        private bool _isDialogVisible;

        public MessageBoxViewModel()
        {

        }

        public bool IsDialogVisible
        {
            get => _isDialogVisible;
            set
            {
                if (value == _isDialogVisible) return; _isDialogVisible = value; OnPropertyChanged();
            }
        }

        public void ShowDialog(string title, string message)
        {
            IsDialogVisible = true;

            Debug.WriteLine($"MessageBoxViewModel.Show: {title} - {message}");

            IsDialogVisible = false;
        }
    }
}
