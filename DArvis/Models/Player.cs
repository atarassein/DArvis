using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using DArvis.Common;
using DArvis.IO.Process;
using DArvis.Macro;
using DArvis.Settings;

namespace DArvis.Models
{
    public sealed class Player : UpdatableObject, IDisposable
    {
        private const string CharacterNameKey = @"CharacterName";
        private const string PacketIdKey = @"PacketId";

        private readonly ProcessMemoryAccessor accessor;
        private readonly ClientState gameClient;
        private readonly Inventory inventory;
        private readonly EquipmentSet equipment;
        private readonly Skillbook skillbook;
        private readonly Spellbook spellbook;
        private readonly PlayerStats stats;
        private readonly PlayerModifiers modifiers;
        private readonly MapLocation location;
        private readonly AislingManager aislingManager;
        private readonly BuffManager buffManager;
        private readonly TravelDestinationManager travelDestinationManager;
        
        private readonly Stream stream;
        private readonly BinaryReader reader;

        private ClientVersion version;
        
        private string name;
        private int packetId;
        private DateTime? loginTimestamp;
        private bool isLoggedIn;
        private string status;
        private bool isWalking;
        private bool isMacroRunning;
        private bool isMacroPaused;
        private bool isMacroStopped;
        private Hotkey hotkey;
        private int selectedTabIndex;
        private bool hasLyliacPlant;
        private bool hasLyliacVineyard;
        private bool hasFasSpiorad;
        
        private Player? _leader;
        private Player? follower;

        public AislingManager AislingManager => aislingManager;
        public BuffManager BuffManager => buffManager;
        
        public TravelDestinationManager TravelDestinationManager => travelDestinationManager;
        
        private DateTime lastFlowerTimestamp;
        public DateTime LastWalkCommand;
        public int WalkOrdinal { get; internal set; } // TODO: this might get removed later
        public event EventHandler LoggedIn;
        public event EventHandler LoggedOut;

        public ClientProcess Process { get; init; }

        public ClientVersion Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public nint ProcessHandle => accessor.ProcessHandle;

        public ProcessMemoryAccessor Accessor => accessor;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public int PacketId
        {
            get => packetId;
            set => SetProperty(ref packetId, value);
        }
        
        public Player? Leader
        {
            get => _leader;
            set => SetProperty(ref _leader, value, onChanged: leader => Location.Update());
        }
        
        public Player? Follower
        {
            get => follower;
            set => SetProperty(ref follower, value);
        }

        /// <summary>
        /// Checks if the player needs map data to be loaded.
        /// Current use cases: following a leader
        /// </summary>
        /// <returns></returns>
        public bool NeedsMapData()
        {
            if (Location?.CurrentMap == null)
            {
                return false; // No map to populate with data
            }
            
            if (Leader != null)
            {
                return true;
            }

            return false;
        }

        public ConcurrentDictionary<int, PathNode?> _lastPosition;

        public ConcurrentDictionary<int, PathNode?> LastPosition
        {
            get => _lastPosition ??= new ConcurrentDictionary<int, PathNode?>();
        }
        
        public ConcurrentDictionary<int, PathNode?> _breadcrumbs;

        public ConcurrentDictionary<int, PathNode?> Breadcrumbs
        {
            get => _breadcrumbs ??= new ConcurrentDictionary<int, PathNode?>();
        }
        
        public ClientState GameClient => gameClient;

        public Inventory Inventory => inventory;

        public EquipmentSet Equipment => equipment;

        public Skillbook Skillbook => skillbook;

        public Spellbook Spellbook => spellbook;

        public PlayerStats Stats => stats;

        public PlayerModifiers Modifiers => modifiers;
        
        public MapLocation Location => location;
        
        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => SetProperty(ref isLoggedIn, value);
        }

        public DateTime? LoginTimestamp
        {
            get => loginTimestamp;
            set => SetProperty(ref loginTimestamp, value);
        }

        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public bool IsWalking
        {
            get => isWalking;
            set => SetProperty(ref isWalking, value);
        }
        
        public bool IsMacroRunning
        {
            get => isMacroRunning;
            set => SetProperty(ref isMacroRunning, value);
        }

        public bool IsMacroPaused
        {
            get => isMacroPaused;
            set => SetProperty(ref isMacroPaused, value);
        }

        public bool IsMacroStopped
        {
            get => isMacroStopped;
            set => SetProperty(ref isMacroStopped, value);
        }

        public string HotkeyString => hotkey?.ToString();

        public Hotkey Hotkey
        {
            get => hotkey;
            set => SetProperty(ref hotkey, value, onChanged: (playerClass) => { RaisePropertyChanged(nameof(HotkeyString)); RaisePropertyChanged(nameof(HasHotkey)); });
        }

        public bool HasHotkey => !string.IsNullOrWhiteSpace(HotkeyString);

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public bool HasLyliacPlant
        {
            get => hasLyliacPlant;
            set => SetProperty(ref hasLyliacPlant, value);
        }

        public bool HasLyliacVineyard
        {
            get => hasLyliacVineyard;
            set => SetProperty(ref hasLyliacVineyard, value);
        }

        public bool HasFasSpiorad
        {
            get => hasFasSpiorad;
            set => SetProperty(ref hasFasSpiorad, value);
        }

        public DateTime LastFlowerTimestamp
        {
            get => lastFlowerTimestamp;
            set => SetProperty(ref lastFlowerTimestamp, value, onChanged: (p) => { RaisePropertyChanged(nameof(TimeSinceFlower)); });
        }

        public TimeSpan TimeSinceFlower => DateTime.Now - lastFlowerTimestamp;

        public Player(ClientProcess process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            accessor = new ProcessMemoryAccessor(process.ProcessId, ProcessAccess.Read);

            stream = accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            gameClient = new ClientState(this);
            inventory = new Inventory(this);
            equipment = new EquipmentSet(this);
            skillbook = new Skillbook(this);
            spellbook = new Spellbook(this);
            stats = new PlayerStats(this);
            modifiers = new PlayerModifiers(this);
            location = new MapLocation(this);
            aislingManager = new AislingManager(this);
            buffManager = new BuffManager(this);
            travelDestinationManager = new TravelDestinationManager(this);
        }

        ~Player() => Dispose(false);

        protected override void Dispose(bool shouldDispose)
        {
            if (isDisposed)
                return;

            if (shouldDispose)
            {
                IsDisposing = true;
                gameClient.Dispose();
                inventory.Dispose();
                equipment.Dispose();
                skillbook.Dispose();
                spellbook.Dispose();
                stats.Dispose();
                modifiers.Dispose();
                location.Dispose();

                stream.Dispose();
                reader.Dispose();
                accessor.Dispose();
            }

            base.Dispose(shouldDispose);
        }

        protected override void OnUpdate()
        {
            GameClient.VersionKey = Version?.Key ?? "Unknown";

            Process.TryUpdate();
            gameClient.TryUpdate();

            try
            {
                UpdateName(accessor);
                UpdatePacketId(accessor);
            }
            catch { }

            stats.TryUpdate();
            modifiers.TryUpdate();
            location.TryUpdate();
            inventory.TryUpdate();
            equipment.TryUpdate();
            skillbook.TryUpdate();
            spellbook.TryUpdate();

            var wasLoggedIn = IsLoggedIn;
            var isNowLoggedIn = !string.IsNullOrWhiteSpace(Name) && stats.Level > 0;
            
            if (isNowLoggedIn && !wasLoggedIn)
                OnLoggedIn();
            else if (wasLoggedIn && !isNowLoggedIn)
                OnLoggedOut();
        }

        private void UpdateName(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            string name = null;

            if (version != null && version.TryGetVariable(CharacterNameKey, out var nameVariable))
                nameVariable.TryReadString(reader, out name);

            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
        }

        private void UpdatePacketId(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            int packetId = -1;

            if (version != null && version.TryGetVariable(PacketIdKey, out var packetIdVariable))
                packetIdVariable.TryReadInt32(reader, out packetId);

            if (int.IsPositive(packetId))
                PacketId = packetId;
        }

        public bool IsNearby(Player? otherPlayer, int distance = 2)
        {
            if (otherPlayer == null || otherPlayer.IsDisposing)
                return false;

            return Location.IsNearby(otherPlayer.Location, distance);
        }
        
        public bool IsOnSameMapAs(Player otherPlayer)
        {
            return Location.MapNumber == otherPlayer.Location.MapNumber;
        }
        
        private void OnLoggedIn()
        {
            IsLoggedIn = true;
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }

        void OnLoggedOut()
        {
            // This memory gets re-allocated when a new character logs into the same client instance
            skillbook.ResetCooldownPointer();

            IsLoggedIn = false;
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString() => Name ?? string.Format("Process {0}", Process.ProcessId.ToString());
    }
}
