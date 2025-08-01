﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using DArvis.Common;
using DArvis.IO;
using DArvis.Metadata;
using DArvis.Models;
using DArvis.Settings;

namespace DArvis.Macro
{
    public sealed class PlayerMacroState : MacroState
    {
        private static readonly TimeSpan PanelTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan SwitchDelay = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan DialogDelay = TimeSpan.FromSeconds(2);

        private readonly DeferredDispatcher deferredDispatcher = new();
        private readonly ReaderWriterLockSlim spellQueueLock = new();
        private readonly ReaderWriterLockSlim flowerQueueLock = new();

        private List<SpellQueueItem> spellQueue = new();
        private List<FlowerQueueItem> flowerQueue = new();
        private PlayerMacroStatus playerStatus;

        private FollowTarget _followTarget;
        private BuffTargets _buffTargets;
        private FollowTarget FollowTarget
        {
            get
            {
                if (_followTarget == null)
                {
                    _followTarget = new FollowTarget(this);
                }

                return _followTarget;
            }
        }
        
        private int spellQueueIndex;
        private int flowerQueueIndex;

        private SpellRotationMode spellQueueRotation;
        private bool isWaitingOnMana;
        private bool useLyliacVineyard;
        private bool flowerAlternateCharacters;
        private bool skipSpellsOnCooldown;

        private DateTime waterAndBedsTimestamp;
        private DateTime spellCastTimestamp;
        private TimeSpan spellCastDuration;

        private SpellQueueItem lastUsedSpellItem;
        private SpellQueueItem fasSpioradQueueItem;
        private SpellQueueItem lyliacPlantQueueItem;
        private SpellQueueItem lyliacVineyardQueueItem;

        public event SpellQueueItemEventHandler SpellAdded;
        public event SpellQueueItemEventHandler SpellUpdated;
        public event SpellQueueItemEventHandler SpellRemoved;

        public event FlowerQueueItemEventHandler FlowerTargetAdded;
        public event FlowerQueueItemEventHandler FlowerTargetUpdated;
        public event FlowerQueueItemEventHandler FlowerTargetRemoved;

        public PlayerMacroStatus PlayerStatus
        {
            get => playerStatus;
            set => SetProperty(ref playerStatus, value, nameof(PlayerStatus));
        }

        public IReadOnlyList<SpellQueueItem> QueuedSpells => spellQueue;

        public IReadOnlyList<FlowerQueueItem> FlowerTargets => flowerQueue;

        public int ActiveSpellsCount => spellQueue.Count((spell) => { return !spell.IsDone; });

        public int CompletedSpellsCount => spellQueue.Count((spell) => { return spell.IsDone; });

        public int TotalSpellsCount => spellQueue.Count;

        public int FlowerQueueCount => flowerQueue.Count;

        public SpellRotationMode SpellQueueRotation
        {
            get => spellQueueRotation;
            set => SetProperty(ref spellQueueRotation, value);
        }

        public bool IsWaitingOnMana
        {
            get => isWaitingOnMana;
            set => SetProperty(ref isWaitingOnMana, value);
        }

        public bool UseLyliacVineyard
        {
            get => useLyliacVineyard;
            set => SetProperty(ref useLyliacVineyard, value);
        }

        public bool FlowerAlternateCharacters
        {
            get => flowerAlternateCharacters;
            set => SetProperty(ref flowerAlternateCharacters, value);
        }

        public bool SkipSpellsOnCooldown
        {
            get => skipSpellsOnCooldown;
            set => SetProperty(ref skipSpellsOnCooldown, value);
        }

        public DateTime SpellCastTimestamp
        {
            get => spellCastTimestamp;
            private set => spellCastTimestamp = value;
        }

        public TimeSpan SpellCastDuration
        {
            get => spellCastDuration;
            private set => spellCastDuration = value;
        }

        public bool IsSpellCasting
        {
            get
            {
                if (spellCastDuration == TimeSpan.Zero)
                    return false;

                var elapsed = DateTime.Now - spellCastTimestamp;
                return elapsed < spellCastDuration;
            }
        }

        public PlayerMacroState(Player client)
            : base(client)
        {
            _followTarget = new FollowTarget(this);
            _buffTargets = new BuffTargets(this);
        }

        public void AddToSpellQueue(SpellQueueItem spell, int index = -1)
        {
            spell.IsUndefined = !SpellMetadataManager.Instance.ContainsSpell(spell.Name);

            spellQueueLock.EnterWriteLock();
            try
            {
                if (index < 0)
                    spellQueue.Add(spell);
                else
                    spellQueue.Insert(index, spell);
            }
            finally { spellQueueLock.ExitWriteLock(); }

            OnSpellAdded(spell);
        }

        public void AddToFlowerQueue(FlowerQueueItem flower, int index = -1)
        {
            flowerQueueLock.EnterWriteLock();
            try
            {
                if (index < 0)
                    flowerQueue.Add(flower);
                else
                    flowerQueue.Insert(index, flower);
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            OnFlowerTargetAdded(flower);
        }

        public bool IsSpellInQueue(string spellName)
        {
            spellName = spellName.Trim();

            spellQueueLock.EnterReadLock();
            try
            {
                foreach (var spell in spellQueue)
                {
                    if (string.Equals(spell.Name, spellName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            finally
            {
                spellQueueLock.ExitReadLock();
            }
        }

        public bool RemoveFromSpellQueue(SpellQueueItem spell)
        {
            var wasRemoved = false;

            spellQueueLock.EnterWriteLock();
            try
            {
                wasRemoved = spellQueue.Remove(spell);
            }
            finally { spellQueueLock.ExitWriteLock(); }

            OnSpellRemoved(spell);
            return wasRemoved;
        }

        public void RemoveFromSpellQueueAtIndex(int index)
        {
            SpellQueueItem spell = null;

            spellQueueLock.EnterWriteLock();
            try
            {
                spell = spellQueue?[index];
                spellQueue.RemoveAt(index);
            }
            finally { spellQueueLock.ExitWriteLock(); }

            if (spell != null)
                OnSpellRemoved(spell);
        }

        public bool RemoveFromFlowerQueue(FlowerQueueItem flower)
        {
            var wasRemoved = false;

            flowerQueueLock.EnterWriteLock();
            try
            {
                wasRemoved = flowerQueue.Remove(flower);
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            if (wasRemoved)
                OnFlowerTargetRemoved(flower);

            return wasRemoved;
        }

        public void RemoveFromFlowerQueueAtIndex(int index)
        {
            FlowerQueueItem flower = null;

            flowerQueueLock.EnterWriteLock();
            try
            {
                flower = flowerQueue?[index];
                flowerQueue.RemoveAt(index);
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            if (flower != null)
                OnFlowerTargetRemoved(flower);
        }

        public void ClearSpellQueue()
        {
            var oldSpells = Enumerable.Empty<SpellQueueItem>();

            spellQueueLock.EnterWriteLock();
            try
            {
                oldSpells = spellQueue;
                spellQueue = new List<SpellQueueItem>();
            }
            finally { spellQueueLock.ExitWriteLock(); }

            foreach (var spell in oldSpells)
                OnSpellRemoved(spell);
        }

        public void ClearFlowerQueue()
        {
            var oldFlowers = Enumerable.Empty<FlowerQueueItem>();

            flowerQueueLock.EnterWriteLock();
            try
            {
                oldFlowers = flowerQueue;
                flowerQueue = new List<FlowerQueueItem>();
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            foreach (var flower in oldFlowers)
                OnFlowerTargetRemoved(flower);
        }

        public bool SpellQueueMoveItemUp(SpellQueueItem item)
        {
            var hasChanges = false;

            spellQueueLock.EnterWriteLock();
            try
            {
                hasChanges = MoveListItemUp(spellQueue, item);
            }
            finally { spellQueueLock.ExitWriteLock(); }

            if (hasChanges)
                RaisePropertyChanged(nameof(QueuedSpells));

            return hasChanges;
        }

        public bool SpellQueueMoveItemDown(SpellQueueItem item)
        {
            var hasChanges = false;

            spellQueueLock.EnterWriteLock();
            try
            {
                hasChanges = MoveListItemDown(spellQueue, item);
            }
            finally { spellQueueLock.ExitWriteLock(); }

            if (hasChanges)
                RaisePropertyChanged(nameof(QueuedSpells));

            return hasChanges;
        }

        public bool FlowerQueueMoveItemUp(FlowerQueueItem item)
        {
            var hasChanges = false;

            flowerQueueLock.EnterWriteLock();
            try
            {
                hasChanges = MoveListItemUp(flowerQueue, item);
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            if (hasChanges)
                RaisePropertyChanged(nameof(FlowerTargets));

            return hasChanges;
        }

        public bool FlowerQueueMoveItemDown(FlowerQueueItem item)
        {
            var hasChanges = false;

            flowerQueueLock.EnterWriteLock();
            try
            {
                hasChanges = MoveListItemDown(flowerQueue, item);
            }
            finally { flowerQueueLock.ExitWriteLock(); }

            if (hasChanges)
                RaisePropertyChanged(nameof(FlowerTargets));

            return hasChanges;
        }

        private static bool MoveListItemUp<T>(IList<T> list, T item)
        {
            var currentIndex = list.IndexOf(item);
            var newIndex = currentIndex - 1;

            if (currentIndex < 0 || newIndex < 0)
                return false;

            var displacedItem = list[newIndex];
            var currentItem = list[currentIndex];

            list[newIndex] = currentItem;
            list[currentIndex] = displacedItem;
            return true;
        }

        private static bool MoveListItemDown<T>(IList<T> list, T item)
        {
            var currentIndex = list.IndexOf(item);
            var newIndex = currentIndex + 1;

            if (currentIndex < 0 || currentIndex > list.Count - 1 || newIndex > list.Count - 1)
                return false;

            var displacedItem = list[newIndex];
            var currentItem = list[currentIndex];

            list[newIndex] = currentItem;
            list[currentIndex] = displacedItem;
            return true;
        }

        public void TickFlowerTimers(TimeSpan deltaTime)
        {
            flowerQueueLock.EnterUpgradeableReadLock();
            try
            {
                foreach (var flower in flowerQueue)
                    flower.Tick(deltaTime);
            }
            finally { flowerQueueLock.ExitUpgradeableReadLock(); }
        }

        public void CancelCasting()
        {
            spellCastDuration = TimeSpan.Zero;
        }

        protected override void MacroLoop(object argument)
        {
            // Tick the dispatcher so any scheduled events go off
            deferredDispatcher.Tick();

            if (client.GameClient.IsUserChatting)
            {
                SetPlayerStatus(PlayerMacroStatus.ChatIsUp);
                return;
            }

            var preserveUserPanel = UserSettingsManager.Instance.Settings.PreserveUserPanel;
            InterfacePanel currentPanel = InterfacePanel.Stats;

            if (preserveUserPanel)
                currentPanel = client.GameClient.ActivePanel;

            DoFollowMacro();
            DoBuffMacro();
            
            if (UserSettingsManager.Instance.Settings.FlowerBeforeSpellMacros)
            {
                DoFlowerMacro();
                DoSpellMacro();
            }
            else
            {
                DoSpellMacro();
                DoFlowerMacro();
            }

            var didUseSkill = DoSkillMacro(out var didAssail);

            DoClickWaterAndBedsIfNeeded();

            if (!IsSpellCasting)
                client.Spellbook.ActiveSpell = null;

            if (preserveUserPanel)
                client.SwitchToPanel(currentPanel, out _);

            if (Status == MacroStatus.Running)
            {
                if (IsSpellCasting)
                {
                    var spellName = client.Spellbook.ActiveSpell;

                    if (string.Equals(Spell.FasSpioradKey, spellName, StringComparison.OrdinalIgnoreCase))
                        SetPlayerStatus(PlayerMacroStatus.FasSpiorad);
                    else if (string.Equals(Spell.LyliacPlantKey, spellName, StringComparison.OrdinalIgnoreCase))
                        SetPlayerStatus(PlayerMacroStatus.Flowering);
                    else if (string.Equals(Spell.LyliacVineyardKey, spellName, StringComparison.OrdinalIgnoreCase))
                        SetPlayerStatus(PlayerMacroStatus.Vineyarding);
                    else
                        SetPlayerStatus(PlayerMacroStatus.Casting);
                }
                else if (IsWaitingOnMana)
                    SetPlayerStatus(PlayerMacroStatus.WaitingForMana);
                else if (FlowerAlternateCharacters)
                    SetPlayerStatus(PlayerMacroStatus.ReadyToFlower);
                else if (UseLyliacVineyard)
                    SetPlayerStatus(PlayerMacroStatus.WaitingOnVineyard);
                else if (didUseSkill)
                    SetPlayerStatus(PlayerMacroStatus.UsingSkills);
                else if (didAssail)
                    SetPlayerStatus(PlayerMacroStatus.Assailing);
                else
                {
                    if (flowerQueue.Count > 0)
                        SetPlayerStatus(PlayerMacroStatus.ReadyToFlower);
                    else if (client.Skillbook.ActiveSkills.Any())
                        SetPlayerStatus(PlayerMacroStatus.Waiting);
                    else
                        SetPlayerStatus(PlayerMacroStatus.Idle);
                }
            }
        }

        private async void DoFollowMacro()
        {
            if (await _followTarget.ShouldWalk())
            {
                await _followTarget.Walk();
            }
        }

        private async void DoBuffMacro()
        {
            if (await _buffTargets.ShouldBuff())
            {
                await _buffTargets.Buff();
            }
        }
        // Zolian feature!
        private bool DoClickWaterAndBedsIfNeeded()
        {
            var timeSinceLastAttempt = DateTime.Now - waterAndBedsTimestamp;
            if (timeSinceLastAttempt.TotalSeconds < 0.5)
                return false;

            if (!LocalStorage.GetBoolOrDefault(LocalStorageKey.UseWaterAndBeds.IsEnabled, false))
                return false;

            var manaThreshold = LocalStorage.GetIntegerOrDefault(LocalStorageKey.UseWaterAndBeds.ManaThreshold, 1000);

            if (client.Stats.CurrentMana >= manaThreshold)
                return false;

            var tileX = LocalStorage.GetIntegerOrDefault(LocalStorageKey.UseWaterAndBeds.TileX, 5);
            var tileY = LocalStorage.GetIntegerOrDefault(LocalStorageKey.UseWaterAndBeds.TileY, 1);

            var user = new Point(client.Location.X, client.Location.Y);
            var targetLocation = new Point(tileX, tileY);

            if (!IsWithinRange(user, targetLocation))
                return false;

            var target = new SpellTarget(SpellTargetMode.AbsoluteTile, targetLocation);
            ClickTarget(target);

            waterAndBedsTimestamp = DateTime.Now;
            return true;
        }

        private bool DoSkillMacro(out bool didAssail)
        {
            didAssail = false;

            var isAssailQueued = false;
            var useSpaceForAssail = UserSettingsManager.Instance.Settings.UseSpaceForAssail;

            if (IsSpellCasting)
                useSpaceForAssail = false;

            var didUseSkill = false;
            var expectDialog = false;
            var skillList = new List<Skill>(100);
            var useShiftKey = UserSettingsManager.Instance.Settings.UseShiftForMedeniaPane;

            foreach (var skillName in client.Skillbook.ActiveSkills)
            {
                var skill = client.Skillbook.GetSkill(skillName);

                isAssailQueued |= skill.IsAssail;
                expectDialog |= skill.OpensDialog;

                if (skill == null || skill.IsEmpty || skill.IsOnCooldown || (skill.IsAssail && useSpaceForAssail))
                    continue;

                skillList.Add(skill);
            }

            foreach (var skill in skillList.OrderBy((s) => { return s.OpensDialog; }))
            {
                if (skill.RequiresDisarm || (skill.IsAssail && UserSettingsManager.Instance.Settings.DisarmForAssails))
                {
                    var isDisarmed = client.Equipment.IsEmpty(EquipmentSlot.Weapon | EquipmentSlot.Shield);

                    if (!isDisarmed)
                    {
                        SetPlayerStatus(PlayerMacroStatus.Disarming);
                        if (!client.DisarmAndWait(PanelTimeout))
                            continue;
                    }
                }

                // Min health percentage (ex: > 90%), skip if cannot use YET
                if (skill.MinHealthPercent.HasValue && skill.MinHealthPercent.Value >= client.Stats.HealthPercent)
                    continue;

                // Max health percentage (ex < 2%), skip if cannot use YET
                if (skill.MaxHealthPercent.HasValue && client.Stats.HealthPercent > skill.MaxHealthPercent.Value)
                    continue;

                if (client.SwitchToPanelAndWait(skill.Panel, TimeSpan.FromSeconds(1), out var didRequireSwitch, useShiftKey))
                {
                    if (didRequireSwitch)
                        Thread.Sleep(SwitchDelay);

                    client.DoubleClickSlot(skill.Panel, skill.Slot);
                    didUseSkill = !skill.IsAssail;
                }
            }

            // Close the dialog after a few seconds
            if (expectDialog)
                deferredDispatcher.DispatchAfter(client.CancelDialog, DialogDelay);

            if (useSpaceForAssail && isAssailQueued)
            {
                if (expectDialog)
                    client.CancelDialog();

                SetPlayerStatus(PlayerMacroStatus.Assailing);
                client.Assail();
                didAssail = true;
            }

            didAssail |= isAssailQueued;
            return didUseSkill;
        }

        private bool DoSpellMacro()
        {
            if (IsSpellCasting)
                return false;

            SpellQueueItem nextSpell;

            if (ShouldFasSpiorad())
                nextSpell = GetFasSpiorad();
            else
                nextSpell = GetNextSpell();

            if (lastUsedSpellItem != null && lastUsedSpellItem != nextSpell)
                lastUsedSpellItem.IsActive = false;

            if (nextSpell == null)
            {
                IsWaitingOnMana = false;
                return false;
            }

            var spellMetadata = SpellMetadataManager.Instance.GetSpell(nextSpell.Name);
            nextSpell.IsUndefined = spellMetadata == null;

            CastSpell(nextSpell);

            // Close the dialog after a few seconds
            if (spellMetadata?.OpensDialog ?? false)
                deferredDispatcher.DispatchAfter(client.CancelDialog, DialogDelay);

            lastUsedSpellItem = nextSpell;
            lastUsedSpellItem.IsActive = true;
            return true;
        }

        private bool DoFlowerMacro()
        {
            if (IsSpellCasting)
                return false;

            var prioritizeAlts = UserSettingsManager.Instance.Settings.FlowerAltsFirst;

            if (!client.HasLyliacPlant && !client.HasLyliacVineyard)
                return false;

            var checkedAlts = false;

            if (prioritizeAlts)
            {
                if (FlowerAlternateCharacters)
                    if (FlowerNextAltWaitingForMana())
                        return false;

                checkedAlts = true;
            }

            if (UseLyliacVineyard)
            {
                var vineyardSpell = client.Spellbook.GetSpell(Spell.LyliacVineyardKey);
                if (vineyardSpell != null && !vineyardSpell.IsOnCooldown)
                {
                    var vine = GetLyliacVineyard();
                    CastSpell(vine);
                    return true;
                }
            }

            var flowerSpell = client.Spellbook.GetSpell(Spell.LyliacPlantKey);
            int? flowerManaCost = null;

            if (flowerSpell != null)
                flowerManaCost = flowerSpell.ManaCost;

            if (ShouldFasSpiorad(flowerManaCost))
            {
                var fasSpiorad = GetFasSpiorad();
                CastSpell(fasSpiorad);
                return false;
            }

            var nextTarget = GetNextFlowerTarget();

            if (nextTarget != null)
            {
                if (UserSettingsManager.Instance.Settings.FlowerHasMinimum && ShouldFasSpiorad(UserSettingsManager.Instance.Settings.FlowerMinimumMana))
                {
                    var fasSpiorad = GetFasSpiorad();
                    CastSpell(fasSpiorad);
                    return false;
                }

                var lyliac = GetLyliacPlant(nextTarget.Target);
                CastSpell(lyliac);

                nextTarget.LastUsedTimestamp = DateTime.Now;
                nextTarget.ResetTimer();
                return true;
            }

            if (!checkedAlts)
            {
                if (FlowerAlternateCharacters)
                    if (FlowerNextAltWaitingForMana())
                        return true;
            }

            return false;
        }

        private void SetPlayerStatus(PlayerMacroStatus status)
        {
            PlayerStatus = status;

            switch (status)
            {
                case PlayerMacroStatus.Idle:
                    client.Status = "Idle";
                    break;

                case PlayerMacroStatus.Thinking:
                    client.Status = "Thinking";
                    break;

                case PlayerMacroStatus.Waiting:
                    client.Status = "Waiting";
                    break;

                case PlayerMacroStatus.WaitingForMana:
                    client.Status = "Waiting for Mana";
                    break;

                case PlayerMacroStatus.UsingSkills:
                    client.Status = "Using Skills";
                    break;

                case PlayerMacroStatus.Assailing:
                    client.Status = "Assailing";
                    break;

                case PlayerMacroStatus.FasSpiorad:
                    client.Status = "Fas Spiorading";
                    break;

                case PlayerMacroStatus.WaitingOnVineyard:
                    client.Status = "Waiting on Vineyard";
                    break;

                case PlayerMacroStatus.ReadyToFlower:
                    client.Status = "Ready to Flower";
                    break;

                case PlayerMacroStatus.Casting:
                    client.Status = string.Format("Casting {0}", client.Spellbook.ActiveSpell);
                    break;

                case PlayerMacroStatus.Flowering:
                    client.Status = "Flowering";
                    break;

                case PlayerMacroStatus.Vineyarding:
                    client.Status = "Casting Lyliac Vineyard";
                    break;

                case PlayerMacroStatus.SwitchingStaff:
                    client.Status = "Switching Staff";
                    break;

                case PlayerMacroStatus.Disarming:
                    client.Status = "Disarming";
                    break;

                case PlayerMacroStatus.Following:
                    client.Status = "Following";
                    break;

                case PlayerMacroStatus.Walking:
                    client.Status = "Walking";
                    break;

                case PlayerMacroStatus.ChatIsUp:
                    client.Status = "User Chatting";
                    break;

                case PlayerMacroStatus.Nothing:
                    client.Status = null;
                    break;
            }
        }

        private bool FlowerNextAltWaitingForMana()
        {
            var waitingAlt = FindAltWaitingOnMana();

            if (waitingAlt == null)
                return false;

            if (!client.Location.IsWithinRange(waitingAlt.Location))
                return false;

            var lyliacMetadata = SpellMetadataManager.Instance.GetSpell(Spell.LyliacPlantKey);
            var spell = GetLyliacPlant(new SpellTarget { Mode = SpellTargetMode.Character, CharacterName = waitingAlt.Name });
            var isFasSpiorading = false;

            if (lyliacMetadata != null)
            {
                if (ShouldFasSpiorad(lyliacMetadata.ManaCost))
                {
                    spell = GetFasSpiorad();
                    isFasSpiorading = true;
                }
            }

            if (!isFasSpiorading)
                waitingAlt.LastFlowerTimestamp = DateTime.Now;

            return CastSpell(spell);
        }

        private bool ShouldFasSpiorad(int? manaRequirement = null)
        {
            var autoFasSpiorad = UserSettingsManager.Instance.Settings.UseFasSpiorad;

            if (!client.HasFasSpiorad)
                return false;

            // If auto-fas spiorad is enabled, check the threshold
            if (autoFasSpiorad)
            {
                if (client.Stats.HasFullMana)
                    return false;

                if (client.Stats.CurrentMana < UserSettingsManager.Instance.Settings.FasSpioradThreshold)
                    return true;
            }

            // If the spell being cast has a mana requirement, check if fas-spiorad on demand
            if (manaRequirement.HasValue)
            {
                if (UserSettingsManager.Instance.Settings.UseFasSpioradOnDemand && client.Stats.CurrentMana < manaRequirement.Value)
                    return true;
            }

            return false;
        }

        private static Player FindAltWaitingOnMana()
        {
            Player waitingAlt = null;

            foreach (var macro in MacroManager.Instance.Macros)
            {
                if (macro.Status == MacroStatus.Running && macro.IsWaitingOnMana)
                {
                    if (waitingAlt == null || waitingAlt.TimeSinceFlower < macro.Client.TimeSinceFlower)
                        waitingAlt = macro.client;
                }
            }

            return waitingAlt;
        }

        private SpellQueueItem GetFasSpiorad()
        {
            if (fasSpioradQueueItem == null)
            {
                fasSpioradQueueItem = new SpellQueueItem();
                fasSpioradQueueItem.Target.Mode = SpellTargetMode.None;
                fasSpioradQueueItem.Name = Spell.FasSpioradKey;
            }

            return fasSpioradQueueItem;
        }

        private SpellQueueItem GetLyliacPlant(SpellTarget target)
        {
            lyliacPlantQueueItem ??= new SpellQueueItem
            {
                Name = Spell.LyliacPlantKey
            };

            lyliacPlantQueueItem.Target = target;
            return lyliacPlantQueueItem;
        }

        private SpellQueueItem GetLyliacVineyard()
        {
            if (lyliacVineyardQueueItem == null)
            {
                lyliacVineyardQueueItem = new SpellQueueItem();
                lyliacVineyardQueueItem.Target.Mode = SpellTargetMode.None;
                lyliacVineyardQueueItem.Name = Spell.LyliacVineyardKey;
            }

            return lyliacVineyardQueueItem;
        }

        private SpellQueueItem GetNextSpell()
        {
            if (spellQueue.Count < 1)
                return null;

            var skipOnCooldown = UserSettingsManager.Instance.Settings.SkipSpellsOnCooldown;

            var currentSpell = SpellQueueRotation switch
            {
                SpellRotationMode.None => GetNextSpell_NoRotation(skipOnCooldown),
                SpellRotationMode.Singular => GetNextSpell_SingularOrder(skipOnCooldown),
                SpellRotationMode.RoundRobin => GetNextSpell_RoundRobin(skipOnCooldown),
                _ => null,
            };

            if (currentSpell == null)
                return null;

            // Determine if we need to fas spiorad instead
            var currentSpellData = SpellMetadataManager.Instance.GetSpell(currentSpell.Name);

            if (currentSpellData != null && ShouldFasSpiorad(currentSpellData.ManaCost))
                return GetFasSpiorad();

            if (currentSpell.IsOnCooldown)
                return null;

            return !currentSpell.IsDone ? currentSpell : null;
        }

        private SpellQueueItem GetNextSpell_NoRotation(bool skipOnCooldown = true)
        {
            if (skipOnCooldown)
                return spellQueue.FirstOrDefault(spell => !spell.IsWaitingOnHealth && !spell.IsOnCooldown);
            else
                return spellQueue.FirstOrDefault(spell => !spell.IsWaitingOnHealth);
        }

        private SpellQueueItem GetNextSpell_SingularOrder(bool skipOnCooldown = true)
        {
            if (skipOnCooldown)
                return spellQueue.FirstOrDefault(spell => !spell.IsWaitingOnHealth && !spell.IsOnCooldown && !spell.IsDone);
            else
                return spellQueue.FirstOrDefault(spell => !spell.IsWaitingOnHealth && !spell.IsDone);
        }

        private SpellQueueItem GetNextSpell_RoundRobin(bool skipOnCooldown = true)
        {
            // All spells are unavailable, nothing to do
            if (spellQueue.All(spell => spell.IsWaitingOnHealth || spell.IsOnCooldown || spell.IsDone))
                return null;

            var currentSpell = spellQueue.ElementAt(spellQueueIndex);

            while (currentSpell.IsWaitingOnHealth 
                   || currentSpell.IsDone 
                   || (skipOnCooldown && currentSpell.IsOnCooldown)
                   || (currentSpell.CastIfManaBelowEnabled && Client.Stats.CurrentMana > currentSpell.CastIfManaBelow))
                currentSpell = AdvanceToNextSpell();

            // Round robin rotation for next time
            AdvanceToNextSpell();
            return currentSpell;
        }

        private SpellQueueItem AdvanceToNextSpell()
        {
            spellQueueIndex = (spellQueueIndex + 1) % spellQueue.Count;
            return spellQueue.Count > 0 ? spellQueue[spellQueueIndex] : null;
        }

        private FlowerQueueItem GetNextFlowerTarget()
        {
            var prioritizeAlts = UserSettingsManager.Instance.Settings.FlowerAltsFirst;

            if (!client.HasLyliacPlant)
                return null;

            if (flowerQueueIndex >= flowerQueue.Count)
                flowerQueueIndex = 0;

            if (flowerQueue.Count < 1)
                return null;

            var currentTarget = flowerQueue.ElementAt(flowerQueueIndex);
            var currentId = currentTarget.Id;
            bool isWithinManaThreshold;

            if (prioritizeAlts)
            {
                flowerQueueLock.EnterReadLock();
                try
                {
                    foreach (var altTarget in flowerQueue)
                    {
                        if (altTarget.Target.Mode != SpellTargetMode.Character)
                            continue;

                        var altClient = PlayerManager.Instance.GetPlayerByName(altTarget.Target.CharacterName);

                        if (altClient == null)
                            continue;

                        if (!client.Location.IsWithinRange(altClient.Location))
                            continue;

                        isWithinManaThreshold = altTarget.ManaThreshold.HasValue && altClient.Stats.CurrentMana < altTarget.ManaThreshold.Value;

                        if (altTarget.IsReady || isWithinManaThreshold)
                            return altTarget;
                    }
                }
                finally { flowerQueueLock.ExitReadLock(); }
            }

            isWithinManaThreshold = false;

            while (!currentTarget.IsReady && !isWithinManaThreshold)
            {
                isWithinManaThreshold = false;

                if (++flowerQueueIndex >= flowerQueue.Count)
                    flowerQueueIndex = 0;

                currentTarget = flowerQueue.ElementAt(flowerQueueIndex);

                if (currentTarget.Target.Mode == SpellTargetMode.Character && currentTarget.ManaThreshold.HasValue)
                {
                    var altClient = PlayerManager.Instance.GetPlayerByName(currentTarget.Target.CharacterName);
                    if (altClient != null)
                        isWithinManaThreshold = altClient.Stats.CurrentMana < currentTarget.ManaThreshold.Value;
                }

                if (currentTarget.Id == currentId)
                    return null;
            }

            flowerQueueIndex++;
            return currentTarget;
        }

        private bool CastSpell(SpellQueueItem item)
        {
            int? modifiedNumberOfLines = null;

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var useShiftKey = UserSettingsManager.Instance.Settings.UseShiftForMedeniaPane;

            var spell = client.Spellbook.GetSpell(item.Name);

            if (spell == null)
                return false;

            item.CurrentLevel = spell.CurrentLevel;
            item.MaximumLevel = spell.MaximumLevel;

            if (spell.IsOnCooldown)
                return false;

            if (UserSettingsManager.Instance.Settings.RequireManaForSpells)
            {
                if (spell.ManaCost > client.Stats.CurrentMana)
                {
                    IsWaitingOnMana = true;
                    return false;
                }
                else
                    IsWaitingOnMana = false;
            }
            else
                IsWaitingOnMana = false;

            bool didRequireSwitch;
            if (UserSettingsManager.Instance.Settings.AllowStaffSwitching)
            {
                if (!SwitchToBestStaff(item, out modifiedNumberOfLines, out didRequireSwitch))
                    return false;

                if (didRequireSwitch)
                    Thread.Sleep(SwitchDelay);
            }

            if (item.Target.Mode == SpellTargetMode.Character)
            {
                var alt = PlayerManager.Instance.GetPlayerByName(item.Target.CharacterName);

                if (alt == null || !alt.IsLoggedIn)
                    return false;

                if (!client.Location.IsWithinRange(alt.Location))
                    return false;
            }

            if (!modifiedNumberOfLines.HasValue)
            {
                var weapon = client.Equipment.GetSlot(EquipmentSlot.Weapon);

                if (weapon != null)
                    modifiedNumberOfLines = StaffMetadataManager.Instance.GetLinesWithStaff(weapon.Name, item.Name);

                if (!modifiedNumberOfLines.HasValue)
                    modifiedNumberOfLines = spell.NumberOfLines;
            }

            var numberOfLines = modifiedNumberOfLines.Value;
            client.Spellbook.ActiveSpell = spell.Name;

            if (client.SwitchToPanelAndWait(spell.Panel, PanelTimeout, out didRequireSwitch, useShiftKey))
            {
                if (didRequireSwitch)
                    Thread.Sleep(SwitchDelay);

                TimeSpan spellCastDuration;
                DateTime now;
                if (item.Target.Mode == SpellTargetMode.BuffTargets)
                {
                    // Console.WriteLine("COPYING AISLINGMANAGER AISLINGS");
                    var targets = Client.AislingManager.BuffTargets.Values.ToList(); // Create snapshot to avoid collection modification during enumeration
                    foreach (var aisling in targets)
                    {
                        var isSelfTarget = aisling.Name == client.Name;
                        if (!isSelfTarget && !aisling.IsVisible || aisling.IsHidden)
                            continue;

                        if (aisling.BuffExpirationTimes.TryGetValue(spell.Name, out var time))
                        {
                            if (DateTime.Now <= time)
                                continue;
                        }
                        
                        if (!isSelfTarget && !client.Location.IsWithinRange(aisling.X, aisling.Y))
                            continue;
                        
                        // TODO: if macro is walking then it needs to stop walking and wait for us to buff

                        var targetX = aisling.X;
                        var targetY = aisling.Y;
                        if (isSelfTarget)
                        {
                            targetX = client.Location.X;
                            targetY = client.Location.Y;
                        }
                        client.DoubleClickSlot(spell.Panel, spell.Slot);
                        ClickAbsoluteCoord(targetX, targetY);
                        
                        var expirationTime = DateTime.Now + spell.Duration;
                        aisling.BuffExpirationTimes.AddOrUpdate(spell.Name, expirationTime, (_,_) => expirationTime);

                        spellCastDuration = CalculateLineDuration(numberOfLines) + TimeSpan.FromMilliseconds(100);
                        now = DateTime.Now;

                        SpellCastDuration = spellCastDuration;
                        SpellCastTimestamp = now;
                        item.LastUsedTimestamp = now;
                        
                        if (spell.Cooldown > TimeSpan.Zero)
                        {
                            client.Spellbook.SetCooldownTimestamp(spell.Name, now.Add(spellCastDuration));
                            return true;
                        }
                        
                        // TODO: we may or may not need this sleep, walking does check if IsSpellCasting
                        Thread.Sleep(spellCastDuration);
                        // TODO: it's okay to resume walking now
                    }
                    return true;
                }
                
                if (item.Target.Mode == SpellTargetMode.HostileTargets)
                {
                    var targets = Client.Location.CurrentMap.HostileEntities.Values.ToList();
                    foreach (var target in targets)
                    {
                        if (target.DebuffExpirationTimes.TryGetValue(spell.Name, out var time))
                        {
                            if (DateTime.Now <= time)
                                continue;
                        }

                        if (client.Leader == null && !client.Location.IsWithinRange(target.X, target.Y))
                            continue;
                        
                        // If we're following a target then there are special rules for casting
                        if (client.Leader != null)
                        {
                            // Don't cast at all if the leader is on a different map
                            if (client.Location.MapNumber != client.Leader.Location.MapNumber)
                            {
                                return false;
                            }

                            // Don't cast at all if the leader is not within range
                            if (!client.Location.IsWithinRange(client.Leader.Location))
                            {
                                
                                return false;
                            }

                            // Otherwise just don't cast if the target is not also within range of the leader
                            if (!client.Location.IsWithinRange(target.X, target.Y) || !client.Leader.Location.IsWithinRange(target.X, target.Y))
                            {
                                continue;
                            }
                        }
                        
                        if (spell.ManaCost > client.Stats.CurrentMana)
                        {
                            IsWaitingOnMana = true;
                            return false;
                        }
                        
                        // TODO: if macro is walking then it needs to stop walking and wait for us to buff

                        client.DoubleClickSlot(spell.Panel, spell.Slot);
                        ClickAbsoluteCoord(target.X, target.Y);
                        
                        var expirationTime = DateTime.Now + spell.Duration;
                        target.DebuffExpirationTimes.AddOrUpdate(spell.Name, expirationTime, (_,_) => expirationTime);

                        spellCastDuration = CalculateLineDuration(numberOfLines) + TimeSpan.FromMilliseconds(100);
                        now = DateTime.Now;

                        SpellCastDuration = spellCastDuration;
                        SpellCastTimestamp = now;
                        item.LastUsedTimestamp = now;
                        
                        if (spell.Cooldown > TimeSpan.Zero)
                        {
                            client.Spellbook.SetCooldownTimestamp(spell.Name, now.Add(spellCastDuration));
                            return true;
                        }
                        
                        // TODO: we may or may not need this sleep, walking does check if IsSpellCasting
                        Thread.Sleep(spellCastDuration);
                        // TODO: it's okay to resume walking now
                    }
                    return true;
                }
                
                client.DoubleClickSlot(spell.Panel, spell.Slot);
                ClickTarget(item.Target);

                spellCastDuration = CalculateLineDuration(numberOfLines) + TimeSpan.FromMilliseconds(100);
                now = DateTime.Now;

                SpellCastDuration = spellCastDuration;
                SpellCastTimestamp = now;
                item.LastUsedTimestamp = now;

                client.Spellbook.SetCooldownTimestamp(spell.Name, now.Add(spellCastDuration));
                return true;
            }

            return false;
        }

        private bool SwitchToBestStaff(SpellQueueItem item, out int? numberOfLines, out bool didRequireSwitch)
        {
            didRequireSwitch = false;

            numberOfLines = null;

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var equippedStaff = client.Equipment.GetSlot(EquipmentSlot.Weapon);
            var availableList = new List<string>(client.Inventory.ItemNames);

            int? currentNumberOfLines;

            if (equippedStaff != null)
            {
                currentNumberOfLines = StaffMetadataManager.Instance.GetLinesWithStaff(equippedStaff.Name, item.Name);
                availableList.Add(equippedStaff.Name);
            }
            else
                currentNumberOfLines = null;

            var staffToUse = StaffMetadataManager.Instance.GetBestStaffForSpell(item.Name, out numberOfLines, availableList, client.Stats.Level, client.Stats.AbilityLevel);
            if (staffToUse == null)
                return true;

            if (currentNumberOfLines.HasValue && currentNumberOfLines.Value <= numberOfLines)
                return true;

            if (string.Equals(staffToUse.Name, "No Staff", StringComparison.OrdinalIgnoreCase))
            {
                SetPlayerStatus(PlayerMacroStatus.Disarming);
                return client.DisarmAndWait(PanelTimeout);
            }

            SetPlayerStatus(PlayerMacroStatus.SwitchingStaff);

            if (string.Equals(staffToUse.Name, equippedStaff.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!client.EquipItemAndWait(staffToUse.Name, EquipmentSlot.Weapon, PanelTimeout, out didRequireSwitch))
                return false;

            return true;
        }

        private void ClickAbsoluteCoord(int x, int y)
        {
            var pt = GetAbsoluteTilePoint(x, y);

            // scale for window
            if (client.Process.WindowScaleX > 0 && client.Process.WindowScaleX != 1)
                pt.X *= client.Process.WindowScaleX;

            if (client.Process.WindowScaleY > 0 && client.Process.WindowScaleY != 1)
                pt.Y *= client.Process.WindowScaleY;

            client.ClickAt(pt.X, pt.Y);
        }

        private void ClickTarget(SpellTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target.Mode == SpellTargetMode.None)
                return;

            var pt = new Point();

            switch (target.Mode)
            {
                case SpellTargetMode.AbsoluteXY:
                    pt = target.Location;
                    break;

                case SpellTargetMode.AbsoluteTile:
                    pt = GetAbsoluteTilePoint((int)target.Location.X, (int)target.Location.Y);
                    break;

                case SpellTargetMode.Character:
                    pt = GetCharacterPoint(target.CharacterName);
                    break;

                case SpellTargetMode.RelativeTile:
                    pt = GetRelativeTilePoint((int)target.Location.X, (int)target.Location.Y);
                    break;

                case SpellTargetMode.Self:
                    pt = new Point(315, 160);
                    break;

                case SpellTargetMode.AbsoluteRadius:
                    var absRadiusPoint = target.GetNextRadiusPoint();
                    pt = GetAbsoluteTilePoint((int)absRadiusPoint.X, (int)absRadiusPoint.Y);
                    break;

                case SpellTargetMode.RelativeRadius:
                    var relRadiusPoint = target.GetNextRadiusPoint();
                    pt = GetRelativeTilePoint((int)relRadiusPoint.X, (int)relRadiusPoint.Y);
                    break;
            }

            // scale for window
            if (client.Process.WindowScaleX > 0 && client.Process.WindowScaleX != 1)
                pt.X *= client.Process.WindowScaleX;

            if (client.Process.WindowScaleY > 0 && client.Process.WindowScaleY != 1)
                pt.Y *= client.Process.WindowScaleY;

            // apply final offset
            pt.Offset(target.Offset.X, target.Offset.Y);

            client.ClickAt(pt.X, pt.Y);
        }

        private Point GetCharacterPoint(string characterName)
        {
            var pt = new Point();

            var targetPlayer = PlayerManager.Instance.GetPlayerByName(characterName);

            if (targetPlayer == null || !targetPlayer.IsLoggedIn)
                return pt;

            if (targetPlayer.Location.Attributes.MapNumber != client.Location.Attributes.MapNumber)
                return pt;

            if (!string.Equals(targetPlayer.Location.Attributes.MapName, client.Location.Attributes.MapName, StringComparison.Ordinal))
                return pt;

            var deltaX = targetPlayer.Location.X - client.Location.X;
            var deltaY = targetPlayer.Location.Y - client.Location.Y;

            if (Math.Abs(deltaX) > 10 || Math.Abs(deltaY) > 10)
                return pt;

            pt = GetRelativeTilePoint(deltaX, deltaY);
            return pt;
        }

        private static Point GetRelativeTilePoint(int deltaX, int deltaY)
        {
            Point pt = new Point(315, 160);

            var tileX = ((deltaX - deltaY) / 2.0) * 56;
            var tileY = ((deltaY + deltaX) / 2.0) * 27;

            pt.Offset(tileX, tileY);
            return pt;
        }

        private Point GetAbsoluteTilePoint(int tileX, int tileY)
        {
            var deltaX = tileX - client.Location.X;
            var deltaY = tileY - client.Location.Y;

            if (Math.Abs(deltaX) > 10 || Math.Abs(deltaY) > 10)
                return new Point(0, 0);
            else
                return GetRelativeTilePoint(deltaX, deltaY);
        }

        private static bool IsWithinRange(Point a, Point b, int maxX = 10, int maxY = 10)
        {
            var deltaX = Math.Abs(a.X - b.X);
            var deltaY = Math.Abs(b.Y - b.Y);

            return deltaX <= maxX && deltaY <= maxY;
        }

        private static TimeSpan CalculateLineDuration(int numberOfLines)
        {
            if (numberOfLines == 0)
                return UserSettingsManager.Instance.Settings.ZeroLineDelay;
            else if (numberOfLines == 1)
                return UserSettingsManager.Instance.Settings.SingleLineDelay;
            else
            {
                var delay = UserSettingsManager.Instance.Settings.MultipleLineDelaySeconds * numberOfLines;
                return TimeSpan.FromSeconds(delay);
            }
        }

        protected override void StartMacro(object state = null)
        {
            InitializeMacro();
            base.StartMacro(state);
        }

        protected override void OnStatusChanged(MacroStatus status)
        {
            switch (status)
            {
                case MacroStatus.Running:
                    SetPlayerStatus(PlayerStatus);
                    break;

                case MacroStatus.Paused:
                    SetPlayerStatus(PlayerStatus);
                    break;

                case MacroStatus.Stopped:
                    ResetMacro();
                    client.Status = null;
                    break;
            }

            RaiseStatusChanged(status);
        }

        private void InitializeMacro()
        {
            spellQueueIndex = 0;
            spellCastTimestamp = DateTime.Now;
            spellCastDuration = TimeSpan.Zero;

            foreach (var spell in QueuedSpells)
                spell.Target.RadiusIndex = 0;

            foreach (var flower in FlowerTargets)
            {
                flower.ResetTimer();
                flower.Target.RadiusIndex = 0;
            }
        }

        private void ResetMacro()
        {
            client.Spellbook.ActiveSpell = null;

            if (lastUsedSpellItem != null)
                lastUsedSpellItem.IsActive = false;

            IsWaitingOnMana = false;

            SetPlayerStatus(PlayerMacroStatus.Nothing);
        }

        private void OnSpellAdded(SpellQueueItem spell) => SpellAdded?.Invoke(this, new SpellQueueItemEventArgs(spell));
        private void OnSpellUpdated(SpellQueueItem spell) => SpellUpdated?.Invoke(this, new SpellQueueItemEventArgs(spell));
        private void OnSpellRemoved(SpellQueueItem spell) => SpellRemoved?.Invoke(this, new SpellQueueItemEventArgs(spell));
        private void OnFlowerTargetAdded(FlowerQueueItem flower) => FlowerTargetAdded?.Invoke(this, new FlowerQueueItemEventArgs(flower));
        private void OnFlowerTargetUpdated(FlowerQueueItem flower) => FlowerTargetUpdated?.Invoke(this, new FlowerQueueItemEventArgs(flower));
        private void OnFlowerTargetRemoved(FlowerQueueItem flower) => FlowerTargetRemoved?.Invoke(this, new FlowerQueueItemEventArgs(flower));
    }
}
