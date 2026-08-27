using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public partial class WorldSettingsModel : ObservableObject
{
    // ==========================================
    // 🏷️ General World Settings
    // ==========================================
    [ObservableProperty]
    private string _worldName = string.Empty;

    [ObservableProperty]
    private long _seed;

    [ObservableProperty]
    private int _gameType; // 0 = Survival, 1 = Creative, 2 = Adventure, 3 = Spectator

    [ObservableProperty]
    private int _difficulty = 2; // 0 = Peaceful, 1 = Easy, 2 = Normal, 3 = Hard

    [ObservableProperty]
    private bool _isHardcore;

    [ObservableProperty]
    private int _storageVersion = 10;

    [ObservableProperty]
    private string _inventoryVersion = "1.26.21";

    [ObservableProperty]
    private string _baseGameVersion = "*";

    // ==========================================
    // ⏰ Time & Weather
    // ==========================================
    [ObservableProperty]
    private long _dayCount;

    [ObservableProperty]
    private int _timeOfDay; // 0 .. 23999 ticks

    [ObservableProperty]
    private bool _doDaylightCycle = true;

    [ObservableProperty]
    private int _weatherType; // 0 = Clear, 1 = Rain, 2 = Thunder

    [ObservableProperty]
    private int _rainTime = 40000;

    [ObservableProperty]
    private int _lightningTime = 60000;

    [ObservableProperty]
    private bool _doWeatherCycle = true;

    public long TotalTime
    {
        get => Math.Max(0, DayCount) * 24000 + Math.Clamp(TimeOfDay, 0, 23999);
        set
        {
            var total = Math.Max(0, value);
            DayCount = total / 24000;
            TimeOfDay = (int)(total % 24000);
        }
    }

    // ==========================================
    // ❤️ Player Stats & Attributes
    // ==========================================
    [ObservableProperty]
    private float _health = 20f;

    [ObservableProperty]
    private float _maxHealth = 20f;

    [ObservableProperty]
    private float _hunger = 20f;

    [ObservableProperty]
    private float _saturation = 20f;

    [ObservableProperty]
    private int _xpLevel;

    [ObservableProperty]
    private float _xpProgress; // 0.0 - 1.0

    [ObservableProperty]
    private double _posX;

    [ObservableProperty]
    private double _posY;

    [ObservableProperty]
    private double _posZ;

    [ObservableProperty]
    private int _dimension; // 0 = Overworld, 1 = Nether, 2 = The End

    // ==========================================
    // ⚙️ Game Rules (Gamerules)
    // ==========================================
    [ObservableProperty]
    private bool _fallDamage = true;

    [ObservableProperty]
    private bool _fireDamage = true;

    [ObservableProperty]
    private bool _drowningDamage = true;

    [ObservableProperty]
    private bool _freezeDamage = true;

    [ObservableProperty]
    private bool _keepInventory;

    [ObservableProperty]
    private bool _mobGriefing = true;

    [ObservableProperty]
    private bool _doMobSpawning = true;

    [ObservableProperty]
    private bool _doMobLoot = true;

    [ObservableProperty]
    private bool _doTileDrops = true;

    [ObservableProperty]
    private bool _doEntityDrops = true;

    [ObservableProperty]
    private bool _naturalRegeneration = true;

    [ObservableProperty]
    private bool _pvp = true;

    [ObservableProperty]
    private bool _showCoordinates = true;

    [ObservableProperty]
    private bool _doImmediateRespawn;

    [ObservableProperty]
    private bool _tntExplodes = true;

    [ObservableProperty]
    private bool _respawnBlocksExplode = true;

    [ObservableProperty]
    private bool _showDaysPlayed = true;

    [ObservableProperty]
    private int _randomTickSpeed = 1;

    [ObservableProperty]
    private int _playersSleepingPercentage = 100;

    [ObservableProperty]
    private int _spawnRadius = 10;

    // ==========================================
    // 🏆 Cheats & Xbox Achievements
    // ==========================================
    [ObservableProperty]
    private bool _cheatsEnabled;

    [ObservableProperty]
    private bool _commandsEnabled;

    [ObservableProperty]
    private bool _hasBeenLoadedInCreative;

    // Helper to format Time of Day into readable clock
    public string FormattedTimeOfDay
    {
        get
        {
            // In Minecraft: 0 ticks = 06:00, 6000 = 12:00, 18000 = 00:00
            int totalHours = (TimeOfDay / 1000 + 6) % 24;
            int totalMinutes = (int)((TimeOfDay % 1000) * 60 / 1000.0);
            return $"{totalHours:D2}:{totalMinutes:D2}";
        }
    }
}
