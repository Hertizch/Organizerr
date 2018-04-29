using Organizerr.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organizerr.ViewModels
{
    public class SettingsViewModel : ObservableObject, IPageViewModel
    {
        public SettingsViewModel()
        {

        }

        public string Name => "SETTINGS";
    }
}
