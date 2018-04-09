using Spengergasse.MusicMetaApp.Model;
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
  /// Interaction logic for View_Artist.xaml
  /// </summary>
  public partial class View_Artist : Window {
    public Member SelectedMember { get; set; }

    public View_Artist() {
      InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e) {
      using (var db = new HIF3bkaiserEntities()) {
        /* if (db.Genres.Any(g => g.Name == NameField.Text)) {
          MessageBox.Show("Genre already exists", "Genre could not be added");
          return;
        } */
        DialogResult = true;
      }
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      var vm = MembersList.DataContext as VM_Artist;
      vm.RemoveMemberCommand.Execute(sender);
    }
  }
}
