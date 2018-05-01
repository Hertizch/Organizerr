using Organizerr.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organizerr.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Organizerr.Extensions.ObservableObject" />
    /// <seealso cref="Organizerr.ViewModels.IPageViewModel" />
    public class SettingsViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        public SettingsViewModel()
        {

        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => "SETTINGS";
    }
}
