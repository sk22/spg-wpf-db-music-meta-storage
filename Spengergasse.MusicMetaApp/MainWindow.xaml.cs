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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Spengergasse.MusicMetaApp {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs args) {
      DataContext = new VM_MainWindow();
    }

    private void Exit(object sender, RoutedEventArgs e) {
      Application.Current.Shutdown();
    }

    private void GenresList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      var vm = GenresList.DataContext as VM_MainWindow;
      if (vm.EditGenreCommand.CanExecute(sender)) {
        vm.EditGenreCommand.Execute(sender);
      }
    }

    private void ArtistsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      var vm = ArtistsList.DataContext as VM_MainWindow;
      if (vm.EditArtistCommand.CanExecute(sender)) {
        vm.EditArtistCommand.Execute(sender);
      }
    }

    private void AlbumsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      var vm = AlbumsList.DataContext as VM_MainWindow;
      if (vm.EditAlbumCommand.CanExecute(sender)) {
        vm.EditAlbumCommand.Execute(sender);
      }
    }

    private void SongsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
      var vm = SongsList.DataContext as VM_MainWindow;
      if (vm.EditSongCommand.CanExecute(sender)) {
        vm.EditSongCommand.Execute(sender);
      }
    }
  }
}
