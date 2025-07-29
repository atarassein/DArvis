﻿using System;
using System.Windows.Media;

using DArvis.Common;
using DArvis.Services.SideQuest;

namespace DArvis.Models
{
    public sealed class SpellQueueItem : ObservableObject, ICopyable<SpellQueueItem>
    {
        private int id;
        private ImageSource icon;
        private string name;
        private SpellTarget target = new();
        private DateTime lastUsedTimestamp;
        private int startingLevel;
        private int currentLevel;
        private int maximumLevel;
        private int? targetLevel;
        private bool isUndefined;
        private bool isActive;
        private bool isOnCooldown;
        private bool isWaitingOnHealth;

        private bool _isDoneNotified = false;
        public Player Player;
        
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public SpellTarget Target
        {
            get => target;
            set => SetProperty(ref target, value);
        }

        public DateTime LastUsedTimestamp
        {
            get => lastUsedTimestamp;
            set => SetProperty(ref lastUsedTimestamp, value);
        }

        public int StartingLevel
        {
            get => startingLevel;
            set => SetProperty(ref startingLevel, value, onChanged: (s) => { RaisePropertyChanged(nameof(PercentCompleted)); });
        }

        public int CurrentLevel
        {
            get => currentLevel;
            set => SetProperty(ref currentLevel, value, onChanged: (s) => { RaisePropertyChanged(nameof(IsDone)); RaisePropertyChanged(nameof(PercentCompleted)); });
        }

        public int MaximumLevel
        {
            get => maximumLevel;
            set => SetProperty(ref maximumLevel, value, onChanged: (s) => { RaisePropertyChanged(nameof(IsDone)); RaisePropertyChanged(nameof(PercentCompleted)); });
        }

        public int? TargetLevel
        {
            get => targetLevel;
            set => SetProperty(ref targetLevel, value, onChanged: (s) => { RaisePropertyChanged(nameof(IsDone)); RaisePropertyChanged(nameof(HasTargetLevel)); RaisePropertyChanged(nameof(PercentCompleted)); });
        }

        public double PercentCompleted
        {
            get
            {
                if (!HasTargetLevel || CurrentLevel >= TargetLevel.Value)
                {
                    return 100;
                }

                return currentLevel * 100.0 / targetLevel.Value;
            }
        }

        public bool HasTargetLevel => targetLevel.HasValue;

        public bool IsDone
        {
            get
            {
                if (!targetLevel.HasValue)
                    return false;

                var isDone = CurrentLevel >= TargetLevel.Value;
                if (isDone && !_isDoneNotified)
                {
                    _isDoneNotified = true;
                    var sideQuest = App.Current.Services.GetService<ISideQuest>();
                    var toast = new ToastMessage
                    {
                        Type = "mention",
                        Title = $"{Player.Name}",
                        Content = $"{Name} has reached level {TargetLevel.Value}!",
                    };
                    sideQuest.ShowBackgroundToast(Player, toast);
                }
                return isDone;
            }
        }

        public bool IsUndefined
        {
            get => isUndefined;
            set => SetProperty(ref isUndefined, value);
        }

        public bool IsActive
        {
            get => isActive;
            set => SetProperty(ref isActive, value);
        }

        public bool IsOnCooldown
        {
            get => isOnCooldown;
            set => SetProperty(ref isOnCooldown, value);
        }

        public bool IsWaitingOnHealth
        {
            get => isWaitingOnHealth;
            set => SetProperty(ref isWaitingOnHealth, value);
        }

        public bool CastIfManaBelowEnabled { get; set; } = false;
        public int CastIfManaBelow { get; set; } = 0;
        
        public void CopyTo(SpellQueueItem other) => CopyTo(other, true, false);

        public void CopyTo(SpellQueueItem other, bool copyId) => CopyTo(other, copyId, false);

        public void CopyTo(SpellQueueItem other, bool copyId = true, bool copyTimestamp = false)
        {
            if (copyId)
                other.Id = Id;

            other.Icon = Icon;
            other.Name = Name;
            other.Target = Target;
            other.StartingLevel = StartingLevel;
            other.CurrentLevel = CurrentLevel;
            other.MaximumLevel = MaximumLevel;
            other.TargetLevel = TargetLevel;
            other.IsUndefined = IsUndefined;
            other.IsActive = IsActive;
            other.isOnCooldown = IsOnCooldown;
            other.IsWaitingOnHealth = IsWaitingOnHealth;
        }

        public override string ToString() => string.Format("{0} on {1}", name, target.ToString());
    }
}
