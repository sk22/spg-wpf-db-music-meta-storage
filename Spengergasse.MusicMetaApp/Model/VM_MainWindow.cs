using Spengergasse.MusicMetaApp.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Spengergasse.MusicMetaApp.Model {
  class VM_MainWindow : INotifyPropertyChanged {
    private static readonly string ALL_GENRES = "<All Genres>";
    private static readonly Artist ALL_ARTISTS = new Artist { Name = "<All Artists>", Id = 0 };
    private static readonly Album ALL_ALBUMS = new Album { Name = "<All Album>", Id = 0 };
    private string _selectedGenre = ALL_GENRES;
    private Artist _selectedArtist = ALL_ARTISTS;
    private Album _selectedAlbum = ALL_ALBUMS;
    private Song _selectedSong;

    public event PropertyChangedEventHandler PropertyChanged;

    public VM_MainWindow() {
      EditGenreCommand = new DelegateCommand(EditGenreExecuted, GenreOperationCanExecute);
      RemoveGenreCommand = new DelegateCommand(RemoveGenreExecuted, GenreOperationCanExecute);
      AddArtistCommand = new DelegateCommand(AddArtistExecuted);
      EditArtistCommand = new DelegateCommand(EditArtistExecuted, ArtistOperationCanExecute);
      RemoveArtistCommand = new DelegateCommand(RemoveArtistExecuted, ArtistOperationCanExecute);
      AddAlbumCommand = new DelegateCommand(AddAlbumExecuted);
      EditAlbumCommand = new DelegateCommand(EditAlbumExecuted, AlbumOperationCanExecute);
      RemoveAlbumCommand = new DelegateCommand(RemoveAlbumExecuted, AlbumOperationCanExecute);
      AddSongCommand = new DelegateCommand(AddSongExecuted);
      EditSongCommand = new DelegateCommand(EditSongExecuted);
    }

    #region Genres

    public IEnumerable<string> AllGenres {
      get {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          return new List<string> { ALL_GENRES }
            .Concat(
              db.Songs.Select(s => s.Genre).Distinct().ToList()
            );
        }
      }
    }

    public string SelectedGenre {
      get => _selectedGenre;
      set {
        _selectedGenre = value;
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedGenre"));
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
      }
    }

    public ICommand EditGenreCommand { get; }
    public ICommand RemoveGenreCommand { get; }

    public bool GenreOperationCanExecute(object param)
      => SelectedGenre != null && SelectedGenre != ALL_GENRES;

    public void RemoveGenreExecuted(object param) {
      using (var db = new HIF3bkaiserEntities()) {
        var songs = db.Songs.Where(s => s.Genre == SelectedGenre);
        var result = MessageBox.Show(
          string.Format(
            "Do you want to permanently delete all songs " +
            "of the genre {0}? {1} occurencies by {2} artists",
            SelectedGenre,
            songs.Count(),
            songs.Select(s => s.ArtistId).Distinct().Count()
          ),
          "Delete genrea",
          MessageBoxButton.YesNo
        );
        if (result == MessageBoxResult.Yes) {
          db.Songs.RemoveRange(songs);
          db.SaveChanges();
        }
        PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
      }
    }

    public void EditGenreExecuted(object param) {
      var vm = new VM_Genre { CurrentGenre = SelectedGenre };
      var view = new View_Genre { DataContext = vm };
      view.ShowDialog();
      if (view.DialogResult.HasValue && view.DialogResult.Value) {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          foreach (var song in db.Songs.Where(s => s.Genre == SelectedGenre)) {
            song.Genre = vm.CurrentGenre;
          }
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
        }
      }
    }

    #endregion

    #region Artists

    public IEnumerable<Artist> AllArtists {
      get {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          return new List<Artist> { ALL_ARTISTS }
            .Concat(db.Artists.OrderBy(g => g.Name).ToList());
        }
      }
    }

    public Artist SelectedArtist {
      get => _selectedArtist;
      set {
        _selectedArtist = value;
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedArtist"));
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
      }
    }

    public ICommand EditArtistCommand { get; }
    public ICommand AddArtistCommand { get; }
    public ICommand RemoveArtistCommand { get; }

    public void EditArtistExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Artists.Attach(SelectedArtist);
        var vm = new VM_Artist(SelectedArtist);
        var view = new View_Artist { DataContext = vm };
        var oldArtistMembers = vm.CurrentArtist.ArtistMembers.ToList();
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          // where new list doesn't contain deleted member
          var deletedArtistMembers = oldArtistMembers
            .Where(am => !vm.CurrentMembers.Select(m => m.Id).Contains(am.MemberId));
          var removedMembers = deletedArtistMembers.Select(am => am.Member).ToList();

          var unusedMembers = removedMembers.Where(m => m.ArtistMembers.Count == 1);
          var deleteUnused = PromptDeleteMembers(unusedMembers);

          if (deleteUnused) db.Members.RemoveRange(unusedMembers);

          foreach (var artistMember in deletedArtistMembers) {
            if (!unusedMembers.Contains(artistMember.Member) || deleteUnused) {
              db.Entry(artistMember).State = System.Data.Entity.EntityState.Deleted;
            }
          }

          // add members to database if they're new
          foreach (var member in vm.NewMembers) db.Members.Add(member);

          // where old relationships don't contain new member
          var addedArtistMembers = vm.CurrentMembers
            .Where(m => !oldArtistMembers.Select(am => am.MemberId).Contains(m.Id));
          foreach (var member in addedArtistMembers) {
            db.ArtistMembers.Add(new ArtistMember {
              ArtistId = vm.CurrentArtist.Id,
              Member = member
            });
          }

          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllArtists"));
          PropertyChanged(this, new PropertyChangedEventArgs("AllMembers"));
        }
      }
    }

    public static bool PromptDeleteMembers(IEnumerable<Member> members) {
      if (members.Count() == 0) return true;
      var result = MessageBox.Show(
        string.Format(
          "Do you want to permanently delete member{0}? {1}",
          members.Count() == 1 ? "" : "s",
          string.Join(", ", members.Select(m => m.Name))
        ),
        "Delete members",
        MessageBoxButton.YesNo
      );
      return result == MessageBoxResult.Yes;
    }

    public bool ArtistOperationCanExecute(object param)
      => SelectedArtist != null && SelectedArtist.Id != 0;

    public void RemoveArtistExecuted(object param) {
      using (var db = new HIF3bkaiserEntities()) {
        db.Artists.Attach(SelectedArtist);
        var unusedMembers = SelectedArtist.ArtistMembers
          .Select(am => am.Member)
          .Where(m => m.ArtistMembers.Count() == 1);
        if (!PromptDeleteMembers(unusedMembers)) return;
        db.Members.RemoveRange(unusedMembers);
        db.ArtistMembers.RemoveRange(SelectedArtist.ArtistMembers);
        db.Songs.RemoveRange(db.Songs.Where(s => s.ArtistId == SelectedArtist.Id));
        db.Albums.RemoveRange(db.Albums.Where(a => a.ArtistId == SelectedArtist.Id));
        db.Artists.Remove(SelectedArtist);
        db.SaveChanges();
        PropertyChanged(this, new PropertyChangedEventArgs(null));
      }
    }

    public void AddArtistExecuted(object param) {
      var vm = new VM_Artist(null);
      var view = new View_Artist { DataContext = vm };
      view.ShowDialog();
      if (view.DialogResult.HasValue && view.DialogResult.Value) {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          db.Artists.Add(vm.CurrentArtist);
          foreach (var member in vm.NewMembers) {
            db.Members.Add(member);
          }
          foreach (var member in vm.CurrentMembers) {
            db.ArtistMembers.Add(new ArtistMember {
              ArtistId = vm.CurrentArtist.Id,
              Member = member
            });
          }
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllArtists"));
        }
      }
    }

    #endregion

    #region Albums

    public IEnumerable<Album> AllAlbums {
      get {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          return new List<Album> { ALL_ALBUMS }
            .Concat(db.Albums.OrderBy(g => g.Name).ToList());
        }
      }
    }

    public Album SelectedAlbum {
      get => _selectedAlbum;
      set {
        _selectedAlbum = value;
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedAlbum"));
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
      }
    }

    public ICommand EditAlbumCommand { get; }
    public ICommand AddAlbumCommand { get; }
    public ICommand RemoveAlbumCommand { get; }

    public void EditAlbumExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Albums.Attach(SelectedAlbum);
        var vm = new VM_Album(SelectedAlbum, db.Artists.ToList());
        var view = new View_Album { DataContext = vm };
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          db.Entry(vm.CurrentAlbum).State = System.Data.Entity.EntityState.Modified;
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
        }
      }
    }

    public bool AlbumOperationCanExecute(object param)
      => SelectedAlbum != null && SelectedAlbum.Id != 0;

    public void RemoveAlbumExecuted(object param) { }

    public void AddAlbumExecuted(object param) {
      var vm = new VM_Album(null, AllArtists);
      var view = new View_Album { DataContext = vm };
      view.ShowDialog();

      if (view.DialogResult.HasValue && view.DialogResult.Value) {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          db.Albums.Add(vm.CurrentAlbum);
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
        }
      }
    }

    #endregion

    #region Songs
    public Song SelectedSong {
      get => _selectedSong;
      set {
        _selectedSong = value;
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedSong"));
      }
    }

    public IEnumerable<Song> SelectedSongs {
      get {
        using (var db = new HIF3bkaiserEntities()) {
          return db.Songs
            .Include("Artist")
            .Include("Album")
            .Where(s => SelectedArtist.Id == 0 || SelectedArtist.Id == s.ArtistId)
            .Where(s => SelectedAlbum.Id == 0 || SelectedAlbum.Id == s.AlbumId)
            .Where(s => SelectedGenre == ALL_GENRES || SelectedGenre == s.Genre)
            .OrderBy(s => s.Id).ToList();
        }
      }
    }

    public ICommand AddSongCommand { get; }
    public ICommand EditSongCommand { get; }

    private void AddSongExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        var vm = new VM_Song(
          null,
          db.Artists.ToList(),
          db.Albums.ToList(),
          db.Songs.Select(s => s.Genre).Distinct().ToList()
        );
        var view = new View_Song { DataContext = vm };
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          db.Artists.Attach(vm.CurrentSong.Artist);
          db.Albums.Attach(vm.CurrentSong.Album);
          db.Songs.Add(vm.CurrentSong);
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
          PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
        }
      }
    }

    private void EditSongExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Songs.Attach(SelectedSong);
        db.Entry(SelectedSong).Reference(s => s.Artist).Load();
        db.Entry(SelectedSong).Reference(s => s.Album).Load();

        var vm = new VM_Song(
          SelectedSong,
          db.Artists.ToList(),
          db.Albums.ToList(),
          db.Songs.Select(s => s.Genre).Distinct().ToList()
        );
        var view = new View_Song { DataContext = vm };
        var oldComments = SelectedSong.SongComments.ToList();
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          db.SongComments.RemoveRange(
            oldComments.Where(c => !vm.CurrentComments.Contains(c))
          );

          SelectedSong.SongComments = vm.CurrentComments;
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
          PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
        }
      }
    }

    #endregion
  }
}
