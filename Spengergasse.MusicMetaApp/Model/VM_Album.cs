using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Spengergasse.MusicMetaApp.Model {
  class VM_Album {
    public VM_Album(Album album, IEnumerable<Artist> allArtists) {
      CurrentAlbum = album ?? new Album();
      AllArtists = allArtists;
      CurrentRatings = new ObservableCollection<AlbumRating>(
        CurrentAlbum.AlbumRatings
      );
    }

    public Album CurrentAlbum { get; set; }
    public ObservableCollection<AlbumRating> CurrentRatings { get; set; }
    public IEnumerable<Artist> AllArtists { get; set; }
  }
}
