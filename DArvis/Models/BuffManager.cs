using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DArvis.Models;

public class BuffManager(Player player) : INotifyPropertyChanged 
{
    private List<string> _buffNames =
    [
        "armachd",
        "beag slan",
        "slan",
        "mor slan",
        "beannaich",
        "mor beannaich",
        "fas deireas",
        "creag neart",
        "beag naomh aite",
        "naomh aite",
        "mor naomh aite",
        "ard naomh aite",
        "mor dion"
    ];

    private ObservableCollection<Spell> _buffs = [];
    private ObservableCollection<BuffCheckboxViewModel> _buffCheckboxes = [];
    public ObservableCollection<BuffCheckboxViewModel> BuffCheckboxes => _buffCheckboxes;

    public event PropertyChangedEventHandler PropertyChanged;

    private bool _hasBuffs = false;
    public void UpdateBuffs()
    {
        if (_hasBuffs) return;
        
        foreach(var buff in player.Spellbook.AllSpells)
        {
            if (_buffNames.Contains(buff.Name))
            {
                _buffs.Add(buff);
                _buffCheckboxes.Add(new BuffCheckboxViewModel
                {
                    Spell = buff,
                    IsChecked = false,
                    UpdateAction = UpdateBuffCheckboxes
                });
            }
        }
        
        if (_buffs.Count > 0) _hasBuffs = true;
    }
    private void UpdateBuffCheckboxes()
    {
        // Marshal to UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var sortedBuffs = _buffs
                .OrderByDescending(a => _buffCheckboxes.FirstOrDefault(x => x.Spell.Name == a.Name)?.IsChecked ?? false)
                .ThenBy(a => _buffCheckboxes.FirstOrDefault(x => x.Spell.Name == a.Name)?.IsChecked == true ? a.Name : null)
                .Select(a => a.Name)
                .ToList();
            
            var reordered = _buffCheckboxes
                .OrderBy(x => sortedBuffs.IndexOf(x.Spell.Name))
                .ToList();

            _buffCheckboxes.Clear();
            foreach (var item in reordered)
            {
                _buffCheckboxes.Add(item);
            }
        });
    }

}

public class BuffCheckboxViewModel : INotifyPropertyChanged
{
    private bool _isChecked;

    public Spell Spell { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            UpdateAction?.Invoke();
        }
    }
    
    public Action UpdateAction { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}