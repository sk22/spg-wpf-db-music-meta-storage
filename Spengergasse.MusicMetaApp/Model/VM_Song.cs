using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Spengergasse.MusicMetaApp.Model {
  public class VM_Song {
    public Song CurrentSong { get; set; }
    public IEnumerable<Artist> AllArtists { get; set; }
    public IEnumerable<Album> AllAlbums { get; set; }
    public IEnumerable<string> AllGenres { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand AddCommentCommand { get; set; }

    public ObservableCollection<SongComment> CurrentComments { get; set; }
    public string ArtistText { get; set; }
    public string AlbumText { get; set; }

    public VM_Song(
      Song song,
      IEnumerable<Artist> allArtists,
      IEnumerable<Album> allAlbums,
      IEnumerable<string> allGenres
    ) {
      CurrentSong = song ?? new Song();
      ArtistText = CurrentSong.Artist?.Name;
      AlbumText = CurrentSong.Album?.Name;
      AllArtists = allArtists;
      AllAlbums = allAlbums;
      AllGenres = allGenres;
      SaveCommand = new DelegateCommand(SaveExecuted, SaveCanExecute);
      AddCommentCommand = new DelegateCommand(AddCommentExecuted);
      CurrentComments = new ObservableCollection<SongComment>(
        CurrentSong.SongComments
      );
    }

    private void AddCommentExecuted(object obj) {
      CurrentComments.Add(
        new SongComment {
          SongId = CurrentSong.Id,
          Position = 0,
          UserName = "Anonymous"
        }
      );
    }

    private bool SaveCanExecute(object obj) {
      // var window = obj as Window;
      // window.FindName("");
      return CurrentSong.Title != null && CurrentSong.Title.Length > 0;
    }

    private void SaveExecuted(object obj) {
      var window = obj as Window;
      window.DialogResult = true;
    }
  }
}