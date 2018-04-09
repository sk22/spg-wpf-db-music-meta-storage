using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Spengergasse.MusicMetaApp.View {
  /// <summary>
  /// Interaction logic for View_Song.xaml
  /// </summary>
  public partial class View_Song : Window {
    public View_Song() {
      InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
      e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }
  }
}
