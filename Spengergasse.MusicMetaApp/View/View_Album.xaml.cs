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

namespace Spengergasse.MusicMetaApp.View {
  /// <summary>
  /// Interaction logic for ViewGenre.xaml
  /// </summary>
  public partial class View_Album : Window {
    public View_Album() {
      InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
