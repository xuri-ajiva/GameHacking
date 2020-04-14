using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sven_Coop_Map {
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
