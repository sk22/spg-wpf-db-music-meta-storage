using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Spengergasse.MusicMetaApp.Model {
  class VM_Artist {
    public List<Member> NewMembers { get; set; } = new List<Member>();

    public ObservableCollection<Member> CurrentMembers { get; set; }
    public Artist CurrentArtist { get; set; } = new Artist();
    public Member SelectedMember { get; set; }
    public string TextMember { get; set; } = "";

    public ICommand AddMemberCommand { get; set; }
    public ICommand RemoveMemberCommand { get; set; }

    public VM_Artist(Artist artist) {
      CurrentArtist = artist ?? new Artist();
      AddMemberCommand = new DelegateCommand(AddMemberExecuted, AddMemberCanExecute);
      RemoveMemberCommand = new DelegateCommand(RemoveMemberExecuted);
      CurrentMembers = new ObservableCollection<Member>(
        CurrentArtist.ArtistMembers.Select(m => m.Member)
      );
    }

    private bool AddMemberCanExecute(object obj) =>
      (SelectedMember != null &&
        !CurrentMembers.Select(m => m.Id).Contains(SelectedMember.Id)) ||
      (SelectedMember == null && TextMember.Length > 0
        && !NewMembers.Select(m => m.Name).Contains(TextMember));

    private void AddMemberExecuted(object obj) {
      if (SelectedMember != null) {
        CurrentMembers.Add(SelectedMember);
      } else {
        using (var db = new HIF3bkaiserEntities()) {
          var member = new Member {
            Name = TextMember
          };
          NewMembers.Add(member);
          CurrentMembers.Add(member);
        }
      }
    }

    private void RemoveMemberExecuted(object obj) {
      var box = obj as ListBox;
      var member = box.SelectedItem as Member;
      CurrentMembers.Remove(member);
      if (NewMembers.Contains(member)) {
        NewMembers.Remove(member);
      }
    }

    public List<Member> AllMembers {
      get {
        using (var db = new HIF3bkaiserEntities()) {
          return db.Members.ToList();
        }
      }
    }

    public int? Id {
      get => CurrentArtist.Id == 0 ? null : (int?)CurrentArtist.Id;
      set => CurrentArtist.Id = value ?? 0;
    }

    public string Name {
      get => CurrentArtist.Name;
      set => CurrentArtist.Name = value;
    }
  }
}
