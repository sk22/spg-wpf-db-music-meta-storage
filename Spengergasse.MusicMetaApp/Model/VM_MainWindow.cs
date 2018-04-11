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
    private static readonly Album ALL_ALBUMS = new Album { Name = "<All Albums>", Id = 0 };
    private string _selectedGenre = ALL_GENRES;
    private Artist _selectedArtist = ALL_ARTISTS;
    private Album _selectedAlbum = ALL_ALBUMS;
    private Song _selectedSong;

    public event PropertyChangedEventHandler PropertyChanged;

    public VM_MainWindow() {
      RefreshCommand = new DelegateCommand(
        o => PropertyChanged(this, new PropertyChangedEventArgs(null)
      ));
      EditGenreCommand = new DelegateCommand(EditGenreExecuted, GenreOperationCanExecute);
      RemoveGenreCommand = new DelegateCommand(RemoveGenreExecuted, GenreOperationCanExecute);
      AddArtistCommand = new DelegateCommand(AddArtistExecuted);
      EditArtistCommand = new DelegateCommand(EditArtistExecuted, ArtistOperationCanExecute);
      RemoveArtistCommand = new DelegateCommand(RemoveArtistExecuted, ArtistOperationCanExecute);
      AddAlbumCommand = new DelegateCommand(AddAlbumExecuted);
      EditAlbumCommand = new DelegateCommand(EditAlbumExecuted, AlbumOperationCanExecute);
      RemoveAlbumCommand = new DelegateCommand(RemoveAlbumExecuted, AlbumOperationCanExecute);
      AddSongCommand = new DelegateCommand(AddSongExecuted);
      EditSongCommand = new DelegateCommand(EditSongExecuted, EditSongCanExecute);
      RemoveSongCommand = new DelegateCommand(RemoveSongExecuted, RemoveSongCanExecute);
    }

    public ICommand RefreshCommand { get; set; }

    #region Genres

    public IEnumerable<string> AllGenres {
      get {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          return new List<string> { ALL_GENRES }.Concat(
            db.Songs
              .Where(s => s.Genre != null && s.Genre.Length > 0)
              .Select(s => s.Genre)
              .Distinct().ToList()
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
            .Concat(db.Artists.OrderBy(g => g.Name)
              .OrderBy(a => a.Name)
              .ToList()
            );
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
            .Concat(db.Albums
              .OrderBy(a => a.Name)
              .ToList()
            );
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
        var oldRatings = SelectedAlbum.AlbumRatings.ToList();
        var vm = new VM_Album(SelectedAlbum, db.Artists.ToList());
        var view = new View_Album { DataContext = vm };
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          db.AlbumRatings.RemoveRange(
            oldRatings.Where(r => !vm.CurrentRatings.Contains(r))
          );
          SelectedAlbum.AlbumRatings = vm.CurrentRatings;
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
        }
      }
    }

    public bool AlbumOperationCanExecute(object param)
      => SelectedAlbum != null && SelectedAlbum.Id != 0;

    public void RemoveAlbumExecuted(object param) {
      using (var db = new HIF3bkaiserEntities()) {
        db.Albums.Attach(SelectedAlbum);
        db.Albums.Remove(SelectedAlbum);
        db.SaveChanges();
        PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
      }
    }

    public void AddAlbumExecuted(object param) {
      var vm = new VM_Album(null, AllArtists);
      var view = new View_Album { DataContext = vm };
      view.ShowDialog();

      if (view.DialogResult.HasValue && view.DialogResult.Value) {
        using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
          db.Artists.Attach(vm.CurrentAlbum.Artist);
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
            .Where(s => SelectedArtist.Id == ALL_ARTISTS.Id
              || SelectedArtist.Id == s.ArtistId)
            .Where(s => SelectedAlbum.Id == ALL_ALBUMS.Id
              || SelectedAlbum.Id == s.AlbumId)
            .Where(s => SelectedGenre == ALL_GENRES || SelectedGenre == s.Genre)
            .OrderBy(s => s.Album.Name).ThenBy(s => s.DiscNo).ThenBy(s => s.TrackNo)
            .ToList();
        }
      }
    }

    public ICommand AddSongCommand { get; }
    public ICommand RemoveSongCommand { get; }
    public ICommand EditSongCommand { get; }

    private bool RemoveSongCanExecute(object param) =>
      SelectedSong != null;

    private void RemoveSongExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Songs.Attach(SelectedSong);
        db.Songs.Remove(SelectedSong);

        db.SaveChanges();
        PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
      }
    }

    private void AddSongExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Albums.Attach(SelectedAlbum);
        db.Artists.Attach(SelectedArtist);

        var vm = new VM_Song(
          new Song {
            Artist = SelectedArtist == ALL_ARTISTS ? null : SelectedArtist,
            Album = SelectedAlbum == ALL_ALBUMS ? null : SelectedAlbum,
            Genre = SelectedGenre == ALL_GENRES ? null : SelectedGenre
          },
          db.Artists.ToList(),
          db.Albums.ToList(),
          db.Songs.Select(s => s.Genre).Distinct().ToList()
        );


        var view = new View_Song { DataContext = vm };
        view.ShowDialog();


        if (view.DialogResult.HasValue && view.DialogResult.Value) {

          if (vm.CurrentSong.Artist == null) {
            if (vm.ArtistText.Length > 0) {
              var artist = db.Artists.Add(new Artist {
                Name = vm.ArtistText
              });
              db.SaveChanges();
              vm.CurrentSong.Artist = artist;
              PropertyChanged(this, new PropertyChangedEventArgs("AllArtists"));
            }

            if (vm.AlbumText.Length > 0) {
              var album = db.Albums.Add(new Album {
                Name = vm.AlbumText,
                Artist = vm.CurrentSong.Artist
              });
              db.SaveChanges();
              vm.CurrentSong.Album = album;
              PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
            }
          }

          if (!AllGenres.Contains(vm.CurrentSong.Genre)) {
            PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
          }

          db.Songs.Add(vm.CurrentSong);

          vm.CurrentSong.SongComments = vm.CurrentComments;
          db.SaveChanges();
          PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
        }
      }
    }

    private bool EditSongCanExecute(object param) => SelectedSong != null;

    private void EditSongExecuted(object param) {
      using (HIF3bkaiserEntities db = new HIF3bkaiserEntities()) {
        db.Songs.Attach(SelectedSong);
        db.Entry(SelectedSong).Reference("Artist").Load();
        db.Entry(SelectedSong).Reference("Album").Load();
        
        var vm = new VM_Song(
          SelectedSong,
          db.Artists.ToList(),
          db.Albums.ToList(),
          db.Songs.Select(s => s.Genre).Distinct().ToList()
        );

        var oldGenre = vm.CurrentSong.Genre;

        var view = new View_Song { DataContext = vm };
        var oldComments = SelectedSong.SongComments.ToList();
        view.ShowDialog();

        if (view.DialogResult.HasValue && view.DialogResult.Value) {
          db.SongComments.RemoveRange(
            oldComments.Where(c => !vm.CurrentComments.Contains(c))
          );

          if (vm.CurrentSong.Artist == null) {
            if (vm.ArtistText?.Length > 0) {
              var artist = db.Artists.Add(new Artist { Name = vm.ArtistText });
              db.SaveChanges();
              vm.CurrentSong.Artist = artist;
              PropertyChanged(this, new PropertyChangedEventArgs("AllArtists"));
            }
          }

          if (vm.CurrentSong.Album == null) {
            if (vm.AlbumText?.Length > 0) {
              var album = db.Albums.Add(
                new Album {
                  Name = vm.AlbumText,
                  Artist = vm.CurrentSong.Artist
                }
              );
              db.SaveChanges();
              vm.CurrentSong.Album = album;
              PropertyChanged(this, new PropertyChangedEventArgs("AllAlbums"));
            }
          }

          vm.CurrentSong.SongComments = vm.CurrentComments;

          db.SaveChanges();

          if (!AllGenres.Contains(vm.CurrentSong.Genre)
            || oldGenre != vm.CurrentSong.Genre) {
            PropertyChanged(this, new PropertyChangedEventArgs("AllGenres"));
          }
          
          PropertyChanged(this, new PropertyChangedEventArgs("SelectedSongs"));
        }
      }
    }

    #endregion
  }
}
