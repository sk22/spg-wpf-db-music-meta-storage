using System.Collections.Generic;

namespace Spengergasse.MusicMetaApp.Model {
  public class VM_Song {
    public Song CurrentSong { get; set; }
    public IEnumerable<Artist> AllArtists { get; set; }
    public IEnumerable<Album> AllAlbums { get; set; }
    public IEnumerable<string> AllGenres { get; set; }

    public VM_Song(
      Song song,
      IEnumerable<Artist> allArtists,
      IEnumerable<Album> allAlbums,
      IEnumerable<string> allGenres
    ) {
      CurrentSong = song ?? new Song();
      AllArtists = allArtists;
      AllAlbums = allAlbums;
      AllGenres = allGenres;
    }
  }
}