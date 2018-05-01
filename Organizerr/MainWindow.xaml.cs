using System.Windows;
using System.Windows.Input;

namespace Organizerr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Height = SystemParameters.WorkArea.Height * 0.8;

            if (SystemParameters.WorkArea.Width > 2560)
                Width = SystemParameters.WorkArea.Width * 0.6;
            else
                Width = SystemParameters.WorkArea.Width * 0.8;
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void CanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
