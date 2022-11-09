using System.Windows;
using MahApps.Metro.Controls;
using Prism.Services.Dialogs;
 
namespace CifarPrincipalComponentAnalysis
{
    public partial class CustomPrismDialogWindow : MetroWindow, IDialogWindow
    {
        public IDialogResult Result { get; set; }

        private void CustomPrismDialogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is IDialogAware)
                this.Title = (this.DataContext as IDialogAware).Title;

            this.Loaded -= this.CustomPrismDialogWindow_Loaded;
        }

        public CustomPrismDialogWindow()
        {
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.Loaded += this.CustomPrismDialogWindow_Loaded;
        }
    }
}
