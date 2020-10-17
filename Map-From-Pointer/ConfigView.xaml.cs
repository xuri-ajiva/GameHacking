using System.Windows;
using System.Windows.Controls;

namespace Map {
    /// <summary>
    /// Interaktionslogik für ConfigView.xaml
    /// </summary>
    public partial class ConfigView : Window {
        public ConfigView() { InitializeComponent(); }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var value = ( (Slider) sender ).Value;
            Rotater.Radius = (int) value;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            var isChecked                           = ( (CheckBox) sender ).IsChecked;
            if ( isChecked != null ) Rotater.Rotate = isChecked.Value;
        }

        private void Button_Click(object sender, RoutedEventArgs e) { Rotater.SerCenter(); }
    }
}
