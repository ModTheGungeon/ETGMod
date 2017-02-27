#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;
using SGUI;

public class ETGModConsole : ETGModMenu {

    public const int REVISION = 2;

    public static ETGModConsole Instance { get; protected set; }
    public ETGModConsole() {
        Instance = this;
    }

    /// <summary>
    /// All commands supported by the ETGModConsole. Add your own commands here!
    /// </summary>
    public static ConsoleCommandGroup Commands = new ConsoleCommandGroup(delegate (string[] args) {
        Log("Command or group " + args[0] + " doesn't exist");
    });

    /// <summary>
    /// All items in the game, name sorted. Used for the give command.
    /// </summary>
    public static Dictionary<string, int> AllItems = new Dictionary<string, int>();

    public static bool StatSetEnabled = false;

    protected bool _CloseConsoleOnCommand = false;
    protected bool _CutInputFocusOnCommand = false;

    protected static char[] _SplitArgsCharacters = { ' ' };

    protected static AutocompletionSettings _GiveAutocompletionSettings = new AutocompletionSettings(delegate (string input) {
        List<string> ret = new List<string>();
        foreach (string key in Gungeon.Items.IDs) {
            if (key.AutocompletionMatch(input.ToLower())) {
                Console.WriteLine($"INPUT {input} KEY {key} MATCH!");
                ret.Add(key.Replace("gungeon:", ""));
            } else {
                Console.WriteLine($"INPUT {input} KEY {key} NO MATCH!");
            }
        }
        return ret.ToArray();
    });

    protected static AutocompletionSettings _SpawnAutocompletionSettings = new AutocompletionSettings(delegate (string input) {
        List<string> ret = new List<string>();
        foreach (string key in Gungeon.Enemies.IDs) {
            if (key.AutocompletionMatch(input.ToLower())) {
                ret.Add(key.Replace("gungeon:", ""));
            }
        }
        return ret.ToArray();
    });

    protected static AutocompletionSettings _StatAutocompletionSettings = new AutocompletionSettings(delegate (string input) {
        List<string> ret = new List<string>();
        foreach (string key in Enum.GetNames(typeof(TrackedStats))) {
            if (key.AutocompletionMatch(input.ToUpper())) {
                ret.Add(key.ToLower());
            }
        }
        return ret.ToArray();
    });

    public override void Start() {
        // GUI
        GUI = new SGroup {
            Visible = false,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            Children = {
                new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Fill();
                        elem.Size -= new Vector2(0f, elem.Backend.LineHeight - 4f); // Command line input space
                    },
                    Children = {
                        new SLabel("ETGMod <color=#ffffffff>" + ETGMod.BaseUIVersion + "</color>") { Foreground = Color.gray },
                        new SLabel("Console backend <color=#ffffffff>R" + REVISION + "</color>") { Foreground = Color.gray },
                        new SLabel("THIS CONSOLE IS <color=#ff0000ff>WORK IN PROGRESS</color>."),
                        new SLabel("Use <color=#ffffffff>help</color> to find out how to use the console.") { Foreground = Color.gray },
                        new SLabel(),
                        new SLabel("Wee this text gets randomized in order!") {
                            With = {
                                new SRandomLabelModifier()
                            }
                        },
                        new SLabel(),
                    }
                },
                new STextField {
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Size.x = elem.Parent.InnerSize.x;
                        elem.Position.x = 0f;
                        elem.Position.y = elem.Parent.InnerSize.y - elem.Size.y;
                    },
                    OnTextUpdate = delegate(STextField elem, string prevText) {
                        HideAutocomplete();
                    },
                    OverrideTab = true,
                    OnKey = delegate(STextField field, bool keyDown, KeyCode keyCode) {
                        if (!keyDown) {
                            return;
                        }
                        if (keyCode == KeyCode.Escape || keyCode == KeyCode.F2 || keyCode == KeyCode.Slash || keyCode == KeyCode.BackQuote) {
                            ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None;
                            return;
                        }
                        if (keyCode == KeyCode.Tab) {
                            ShowAutocomplete();
                            return;
                        }
                    },
                    OnSubmit = delegate(STextField elem, string text) {
                        if (text.Length == 0) return;
                        ParseCommand(text);
                        if (_CloseConsoleOnCommand) {
                            ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None;
                        }
                    }
                }
            }
        };

        // GLOBAL NAMESPACE
        Commands
                .AddUnit("help", delegate (string[] args) {
                    List<List<string>> paths = Commands.ConstructPaths();
                    for (int i = 0; i < paths.Count; i++) {
                        Log(string.Join(" ", paths[i].ToArray()));
                    }
                })
                .AddUnit("exit", (string[] args) => ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None)
                .AddUnit("screenshake", SetShake)
                .AddUnit("echo", Echo)
                .AddUnit("tp", Teleport)
                .AddUnit("character", SwitchCharacter)
                .AddUnit("clear", (string[] args) => GUI[0].Children.Clear())
                .AddUnit("godmode", delegate (string[] args) {
                    GameManager.Instance.PrimaryPlayer.healthHaver.IsVulnerable = SetBool(args, GameManager.Instance.PrimaryPlayer.healthHaver.IsVulnerable);
                })
                .AddGroup("spawn", Spawn, _SpawnAutocompletionSettings);

        // SPAWN NAMESAPCE
        Commands.GetGroup("spawn")
                .AddUnit("chest", SpawnChest)
                .AddUnit("all", SpawnAll);

        // GIVE NAMESPACE
        Commands.AddGroup("give", GiveItem, _GiveAutocompletionSettings);

        Commands.GetGroup("give")
                .AddGroup("all", (string[] args) => {
                    foreach (string id in Gungeon.Items.IDs)
                        if (id != "gungeon:cell_key" /* (bugged!) */) Gungeon.PrimaryPlayer.GiveItem(id);
                });

        // GIVE ALL NAMESPACE
        Commands.GetGroup("give").GetGroup("all")
                                 .AddUnit("guns", (string[] args) => {
                                 foreach (string id in Gungeon.Items.IDs)
                                         if (ETGMod.Databases.Items[id] is Gun) Gungeon.PrimaryPlayer.GiveItem(id);
                                 })
                                 .AddUnit("passives", (string[] args) => {
                                     foreach (string id in Gungeon.Items.IDs)
                                         if (ETGMod.Databases.Items[id] is PassiveItem) Gungeon.PrimaryPlayer.GiveItem(id);
                                 })
                                 .AddUnit("actives", (string[] args) => {
                                     foreach (string id in Gungeon.Items.IDs)
                                         if (ETGMod.Databases.Items[id] is PlayerItem) Gungeon.PrimaryPlayer.GiveItem(id);
                                 });

        // STAT NAMESPACE
        Commands.AddGroup("stat");

        Commands.GetGroup("stat")
                .AddUnit("get", StatGet, _StatAutocompletionSettings)
                .AddGroup("set", StatSetCurrentCharacter, _StatAutocompletionSettings)
                .AddUnit("mod", StatMod, _StatAutocompletionSettings)
                .AddUnit("list", StatList);

        Commands.GetGroup("stat").GetGroup("set")
                                  .AddUnit("session", StatSetSession, _StatAutocompletionSettings);

        // ROLL NAMESPACE
        Commands.AddGroup("roll");

        Commands.GetGroup("roll")
                .AddUnit("distance", DodgeRollDistance)
                .AddUnit("speed", DodgeRollSpeed);

        // TEST NAMESPACE
        Commands.AddUnit("test", new ConsoleCommandGroup());

        Commands.GetGroup("test")
                .AddGroup("resources")
                .AddUnit("skiplevel", delegate (string[] args) {
                    Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
                    GameManager.Instance.DelayedLoadNextLevel(0.5f);
                }).AddUnit("charselect", delegate (string[] args) {
                    Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
                    GameManager.Instance.DelayedLoadCharacterSelect(0.5f);
                });

        //// TEST.RESOURCES NAMESPACE
        Commands.GetGroup("test").GetGroup("resources")
                                  .AddUnit("load", ResourcesLoad);

        // DUMP NAMESPACE
        Commands.AddUnit("dump", new ConsoleCommandGroup());

        Commands.GetGroup("dump")
                .AddGroup("sprites", (args) => SetBool(args, ref ETGMod.Assets.DumpSprites))
                .AddUnit("packer", (args) => ETGMod.Assets.Dump.DumpPacker())
                .AddUnit("synergies", DumpSynergies)
                .AddUnit("pickups", DumpPickups);

        Commands.GetGroup("dump").GetGroup("sprites")
                                  .AddUnit("metadata", (args) => SetBool(args, ref ETGMod.Assets.DumpSpritesMetadata));

        // CONF NAMESPACE
        Commands.AddGroup("conf");

        Commands.GetGroup("conf")
                .AddUnit("close_console_on_command", (args) => SetBool(args, ref _CloseConsoleOnCommand))
                .AddUnit("cut_input_focus_on_command", (args) => SetBool(args, ref _CutInputFocusOnCommand))
                .AddUnit("enable_damage_indicators", (args) => SetBool(args, ref ETGModGUI.UseDamageIndicators))
                .AddUnit("match_contains", (args) => SetBool(args, ref AutocompletionSettings.MatchContains))
                .AddUnit("enable_stat_set", (args) => SetBool(args, ref StatSetEnabled))
                .AddUnit("player", (args) => ETGMod.Player.PlayerReplacement = args.Length == 1 ? args[0] : null)
                .AddUnit("player_coop", (args) => ETGMod.Player.CoopReplacement = args.Length == 1 ? args[0] : null);
    }

    public override void Update() {

    }

    protected virtual void _Log(string text) {
        GUI[0].Children.Add(new SLabel(text));
        ((SGroup)GUI[0]).ScrollPosition.y = float.MaxValue;
    }
    public static void Log(string text, bool debuglog = false) {
        Instance._Log(text);
        if (debuglog) {
            Debug.Log(text);
        }
    }

    public string[] SplitArgs(string args) {
        return args.Split(_SplitArgsCharacters, StringSplitOptions.RemoveEmptyEntries);
    }

    public void ParseCommand(string text) {
        string[] input = SplitArgs(text.Trim());
        int commandindex = Commands.GetFirstNonUnitIndexInPath(input);

        List<string> path = new List<string>();
        for (int i = 0; i < input.Length - (input.Length - commandindex); i++) {
            if (!string.IsNullOrEmpty(input[i])) path.Add(input[i]);
        }

        List<string> args = new List<string>();
        for (int i = commandindex; i < input.Length; i++) {
            if (!string.IsNullOrEmpty(input[i])) args.Add(input[i]);
        }
        RunCommand(path.ToArray(), args.ToArray());
    }

    public void ShowAutocomplete() {
        if (GUI.Children.Count >= 3) {
            return;
        }
        STextField field = (STextField)GUI[1];

        // AUTO COMPLETIONATOR 3000
        // (by Zatherz)
        //
        // TODO: Make Tab autocomplete to the shared part of completions
        // TODO: AutocompletionRule interface and class per rule?
        // Create an input array by splitting it on spaces
        string inputtext = field.Text.Substring(0, field.CursorIndex);
        string[] input = SplitArgs(inputtext);
        string otherinput = string.Empty;
        if (field.CursorIndex < field.Text.Length) {
            otherinput = field.Text.Substring(field.CursorIndex + 1);
        }
        // Get where the command appears in the path so that we know where the arguments start
        int commandindex = Commands.GetFirstNonUnitIndexInPath(input);
        List<string> pathlist = new List<string>();
        for (int i = 0; i < input.Length - (input.Length - commandindex); i++) {
            pathlist.Add(input[i]);
        }

        string[] path = pathlist.ToArray();

        ConsoleCommandUnit unit = Commands.GetUnit(path);
        // Get an array of available completions
        int matchindex = input.Length - path.Length;
        /*
		HACK! blame Zatherz
		matchindex will be off by +1 if the current keyword your cursor is on isn't empty
		this will check if there are no spaces on the left on the cursor
		and if so, decrease matchindex
		if there *are* spaces on the left of the cursor, that means the current
		"token" the cursor is on is an empty string, so that doesn't have any problems
		Hopefully this is a good enough explanation, if not, ping @Zatherz on Discord
		*/

        string matchkeyword = string.Empty;
        if (!inputtext.EndsWithInvariant(" ")) {
            matchindex--;
            matchkeyword = input[input.Length - 1];
        }

        string[] completions = unit.Autocompletion.Match(Math.Max(matchindex, 0), matchkeyword);

        if (completions == null || completions.Length == 0) {
            Debug.Log("ETGModConsole: no completions available (match returned null)");
        } else if (completions.Length == 1) {
            if (path.Length > 0) {
                field.Text = string.Join(" ", path) + " " + completions[0] + " " + otherinput;
            } else {
                field.Text = completions[0] + " " + otherinput;
            }

            field.MoveCursor(field.Text.Length);
        } else if (completions.Length > 1) {
            SGroup hints = new SGroup {
                Parent = GUI,
                AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                ScrollDirection = SGroup.EDirection.Vertical,
                OnUpdateStyle = delegate (SElement elem) {
                    elem.Size = new Vector2(elem.Parent.InnerSize.x, Mathf.Min(((SGroup)elem).ContentSize.y, 160f));
                    elem.Position = GUI[1].Position - new Vector2(0f, elem.Size.y + 4f);
                }
            };

            for (int i = 0; i < completions.Length; i++) {
                hints.Children.Add(new SLabel(completions[i]));
            }
        }
    }
    public void HideAutocomplete() {
        if (GUI.Children.Count < 3) {
            return;
        }
        GUI.Children.RemoveAt(2);
    }

    public override void OnOpen() {
        base.OnOpen();
        GUI[1].Focus();
    }

    // Use like this:
    //     if (!ArgCount(args, MIN_ARGS, OPTIONAL_MAX_ARGS)) return;
    // Automatically handles error reporting for you
    // ALL VALUES INCLUSIVE!
    public static bool ArgCount(string[] args, int min) {
        if (args.Length >= min) return true;
        Log("Error: need at least " + min + " argument(s)");
        return false;
    }

    public static bool ArgCount(string[] args, int min, int max) {
        if (args.Length >= min && args.Length <= max) return true;
        if (min == max) {
            Log("Error: need exactly " + min + " argument(s)");
        } else {
            Log("Error: need between " + min + " and " + max + " argument(s)");
        }
        return false;
    }

    /// <summary>
    /// Runs the provided command with the provided args.
    /// </summary>
    public static void RunCommand(string[] unit, string[] args) {
        ConsoleCommandUnit command = Commands.GetUnit(unit);
        if (command == null) {
            if (Commands.GetGroup(unit) == null) {
                Log("Command doesn't exist");
            }
        } else {
            try {
                command.RunCommand(args);
            } catch (Exception e) {
                Log(e.ToString());

                Debug.LogError("Exception occured while running command:" + e.ToString());
            }
        }
    }

    // Example commands

    void Echo(string[] args) {
        StringBuilder combined = new StringBuilder();
        for (int i = 0; i < args.Length; i++) {
            combined.Append(args[i]);
            if (i < args.Length - 1) {
                combined.Append(' ');
            }
        }
        string str = combined.ToString();
        Debug.Log(str);
        Log(str);
    }

    void DodgeRollDistance(string[] args) {
        if (!ArgCount(args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.distance = float.Parse(args[0]);
        }
    }

    void DodgeRollSpeed(string[] args) {
        if (!ArgCount(args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.time = float.Parse(args[0]);
        }
    }

    void Teleport(string[] args) {
        if (!ArgCount(args, 2, 2)) return;

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.TeleportToPoint(new Vector2(
                float.Parse(args[0]),
                float.Parse(args[1])
            ), true);
        }
    }

    public void SetBool(string[] args, ref bool value) {
        value = SetBool(args, value);
    }
    public void SetBool(string[] args, ref bool value, bool fallbackValue) {
        value = SetBool(args, fallbackValue);
    }
    public bool SetBool(string[] args, bool fallbackValue) {
        if (args.Length != 1)
            return fallbackValue;

        if (args[0].ToLower() == "true")
            return true;
        else if (args[0].ToLower() == "false")
            return false;
        else
            return fallbackValue;
    }

    void GiveItem(string[] args) {
        if (!ArgCount(args, 1, 2)) return;

        if (!GameManager.Instance.PrimaryPlayer) {
            Log("Couldn't access Player Controller");
            return;
        }

        string id = args[0];
        if (!Gungeon.Items.ContainsID(id)) {
            Log($"Invalid item ID {id}!");
            return;
        }

        Log("Attempting to spawn item ID " + args[0] + " (numeric " + id + ")" + ", class " + Gungeon.Items.Get(id).GetType());

        if (args.Length == 2) {
            int count = int.Parse(args[1]);

            for (int i = 0; i < count; i++)
                Gungeon.PrimaryPlayer.GiveItem(id);
        } else {
            Gungeon.PrimaryPlayer.GiveItem(id);
        }
    }

    void SetShake(string[] args) {
        if (!ArgCount(args, 1, 1)) return;
        Log("Vlambeer set to " + args[0]);
        ScreenShakeSettings.GLOBAL_SHAKE_MULTIPLIER = float.Parse(args[0]);
    }

    void Spawn(string[] args) {
        if (!ArgCount(args, 1, 2)) return;
        string id = args[0];
        if (!Gungeon.Enemies.ContainsID(id)) {
            Log($"Enemy with ID {id} doesn't exist");
            return;
        }
        AIActor enemyPrefab = Gungeon.Enemies[id];
        Log("Spawning ID " + id);
        int count = 1;
        if (args.Length > 1) {
            bool success = int.TryParse(args[1], out count);
            if (!success) {
                Log("Second argument must be an integer (number)");
                return;
            }
        }
        for (int i = 0; i < count; i++) {
            IntVector2? targetCenter = new IntVector2?(GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2(VectorConversions.Floor));
            Pathfinding.CellValidator cellValidator = delegate (IntVector2 c) {
                for (int j = 0; j < enemyPrefab.Clearance.x; j++) {
                    for (int k = 0; k < enemyPrefab.Clearance.y; k++) {
                        if (GameManager.Instance.Dungeon.data.isTopWall(c.x + j, c.y + k)) {
                            return false;
                        }
                        if (targetCenter.HasValue) {
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) < 4) {
                                return false;
                            }
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) > 20) {
                                return false;
                            }
                        }
                    }
                }
                return true;
            };
            IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell(new IntVector2?(enemyPrefab.Clearance), new Dungeonator.CellTypes?(enemyPrefab.PathableTiles), false, cellValidator);
            if (randomAvailableCell.HasValue) {
                AIActor aIActor = AIActor.Spawn(enemyPrefab, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                aIActor.HandleReinforcementFallIntoRoom(0);
            }
        }
    }

    void SpawnChest(string[] args) {
        if (!ArgCount(args, 1, 2)) return;
        Dungeonator.RoomHandler currentRoom = GameManager.Instance.PrimaryPlayer.CurrentRoom;
        RewardManager rewardManager = GameManager.Instance.RewardManager;
        Chest chest;
        bool glitched = false;
        string name = args[0].ToLower();
        switch (name) {
            case "brown":
            case "d":
                chest = rewardManager.D_Chest;
                break;
            case "blue":
            case "c":
                chest = rewardManager.C_Chest;
                break;
            case "green":
            case "b":
                chest = rewardManager.B_Chest;
                break;
            case "red":
            case "a":
                chest = rewardManager.A_Chest;
                break;
            case "black":
            case "s":
                chest = rewardManager.S_Chest;
                break;
            case "rainbow":
            case "r":
                chest = rewardManager.Rainbow_Chest;
                break;
            case "glitched":
            case "g":
                chest = rewardManager.B_Chest;
                glitched = true;
                break;
            default:
                Log("Chest type " + args[0] + " doesn't exist! Valid types: brown, blue, green, red, black, rainbow");
                return;
        }
        WeightedGameObject wGameObject = new WeightedGameObject();
        wGameObject.gameObject = chest.gameObject;
        WeightedGameObjectCollection wGameObjectCollection = new WeightedGameObjectCollection();
        wGameObjectCollection.Add(wGameObject);
        int count = 1;
        float origMimicChance = chest.overrideMimicChance;
        chest.overrideMimicChance = 0f;
        if (args.Length > 1) {
            bool success = int.TryParse(args[1], out count);
            if (!success) {
                Log("Second argument must be an integer (number)");
                return;
            }
        }
        for (int i = 0; i < count; i++) {
            Chest spawnedChest = currentRoom.SpawnRoomRewardChest(wGameObjectCollection, currentRoom.GetBestRewardLocation(new IntVector2(2, 1), Dungeonator.RoomHandler.RewardLocationStyle.PlayerCenter, true));
            spawnedChest.ForceUnlock();
            if (glitched) {
                spawnedChest.BecomeGlitchChest();
            }
        }
        chest.overrideMimicChance = origMimicChance;
    }

    void SpawnAll(string[] args) {
        if (!ArgCount(args, 0)) return;
        foreach (EnemyDatabaseEntry enemy in EnemyDatabase.Instance.Entries) {
            AIActor enemyPrefab = enemy?.GetPrefab<AIActor>();
            if (enemyPrefab == null) continue;
            IntVector2? targetCenter = GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2(VectorConversions.Floor);
            Pathfinding.CellValidator cellValidator = delegate (IntVector2 c) {
                for (int j = 0; j < enemyPrefab.Clearance.x; j++) {
                    for (int k = 0; k < enemyPrefab.Clearance.y; k++) {
                        if (GameManager.Instance.Dungeon.data.isTopWall(c.x + j, c.y + k)) {
                            return false;
                        }
                        if (targetCenter.HasValue) {
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) < 4) {
                                return false;
                            }
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) > 20) {
                                return false;
                            }
                        }
                    }
                }
                return true;
            };
            IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell(enemyPrefab.Clearance, enemyPrefab.PathableTiles, false, cellValidator);
            if (randomAvailableCell.HasValue) {
                AIActor aIActor = AIActor.Spawn(enemyPrefab, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                aIActor.HandleReinforcementFallIntoRoom(0);
            }
        }
    }

    void ResourcesLoad(string[] args) {
        if (!ArgCount(args, 1)) return;
        string resourcepath = string.Join(" ", args);
        object resource = Resources.Load(resourcepath);
        if (resource == null) {
            Log("Couldn't load resource " + resourcepath);
            return;
        }
        Log("Loaded (and threw away) " + resourcepath);
    }

    protected TrackedStats? _GetStatFromString(string statname) {
        TrackedStats stat;
        try {
            stat = (TrackedStats)Enum.Parse(typeof(TrackedStats), statname);
        } catch {
            return null;
        }
        return stat;
    }

    protected bool _VerifyStatSetEnabled(string command) {
        if (!StatSetEnabled) {
            Log("The '" + command + "' command is disabled by default!");
            Log("This command can be very damaging as it sets arbitrary values in your save file.");
            Log("There is *no way* to undo this action.");
            Log("If someone told you to run this command, there is a high chance they are");
            Log("maliciously trying to destroy your save file.");
            Log("This command can also cause achievements to be unlocked. Achievements are");
            Log("disabled by default, but they can be enabled with 'conf enable_achievements true'.");
            Log("If you are *CERTAIN* that you want to run this command, you can enable it by");
            Log("running the following command: 'conf enable_stat_set true'.");
            Log("");
            Log("Be careful.");
            return false;
        }
        return true;
    }

    void StatGet(string[] args) {
        if (!ArgCount(args, 1)) return;
        TrackedStats? stat = _GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue) {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        if (GameManager.Instance.PrimaryPlayer != null) {
            float characterstat = GameStatsManager.Instance.GetCharacterStatValue(stat.Value);
            Log("Character: " + characterstat);
            float sessionstat = GameStatsManager.Instance.GetSessionStatValue(stat.Value);
            Log("Session: " + sessionstat);
        } else {
            Log("Character and Session stats are unavailable, please select a character first");
        }
        float playerstat = GameStatsManager.Instance.GetPlayerStatValue(stat.Value);
        Log("This save file: " + playerstat);
    }

    void StatSetSession(string[] args) {
        if (!_VerifyStatSetEnabled("stat set session")) return;
        if (!ArgCount(args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue) {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse(args[1], out value)) {
            Log("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.SetStat(stat.Value, value);
    }

    void StatSetCurrentCharacter(string[] args) {
        if (!_VerifyStatSetEnabled("stat set")) return;
        if (!ArgCount(args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue) {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse(args[1], out value)) {
            Log("The value isn't a proper number");
            return;
        }
        PlayableCharacters currentCharacter = GameManager.Instance.PrimaryPlayer.characterIdentity;
        GameStatsManager.Instance.m_characterStats[currentCharacter].SetStat(stat.Value, value);
    }

    void StatMod(string[] args) {
        if (!_VerifyStatSetEnabled("stat mod")) return;
        if (!ArgCount(args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue) {
            Log("The value isn't a proper number");
            return;
        }
        float value;
        if (!float.TryParse(args[1], out value)) {
            Log("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.RegisterStatChange(stat.Value, value);
    }

    void StatList(string[] args) {
        if (!ArgCount(args, 0)) return;
        foreach (var value in Enum.GetValues(typeof(TrackedStats))) {
            Log(value.ToString().ToLower());
        }
    }

    void SwitchCharacter(string[] args) {
        if (!ArgCount(args, 1, 2)) return;
        var prefab = (GameObject)Resources.Load("CHARACTERDB:" + args[0]);
        if (prefab == null) {
            Debug.Log(args[0] + " is not a mod character, checking if it's one of the standard characters");
            prefab = (GameObject)Resources.Load("Player" + args[0]);
        }
        if (prefab == null) {
            Log("Failed getting prefab for " + args[0]);
            return;
        }

        Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
        bool wasInGunGame = false;
        if (GameManager.Instance.PrimaryPlayer) {
            wasInGunGame = GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns;
        }
        GameManager.Instance.PrimaryPlayer.SetInputOverride("getting deleted");

        PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
        Vector3 position = primaryPlayer.transform.position;
        UnityEngine.Object.Destroy(primaryPlayer.gameObject);
        GameManager.Instance.ClearPrimaryPlayer();
        GameManager.PlayerPrefabForNewGame = prefab;
        PlayerController component = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>();
        GameStatsManager.Instance.BeginNewSession(component);
        PlayerController playerController;
        GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(GameManager.PlayerPrefabForNewGame, position, Quaternion.identity);
        GameManager.PlayerPrefabForNewGame = null;
        gameObject.SetActive(true);
        playerController = gameObject.GetComponent<PlayerController>();
        if (args.Length == 2) {
            playerController.SwapToAlternateCostume();
        }
        GameManager.Instance.PrimaryPlayer = playerController;
        playerController.PlayerIDX = 0;

        GameManager.Instance.MainCameraController.ClearPlayerCache();
        GameManager.Instance.MainCameraController.SetManualControl(false, true);
        Foyer.Instance.PlayerCharacterChanged(playerController);

        Pixelator.Instance.FadeToBlack(0.5f, true, 0f);

        if (wasInGunGame) {
            GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns = true;
            while (GameManager.Instance.PrimaryPlayer.inventory.AllGuns.Count > 1) {
                var gun = GameManager.Instance.PrimaryPlayer.inventory.AllGuns[1];
                GameManager.Instance.PrimaryPlayer.inventory.RemoveGunFromInventory(gun);
                UnityEngine.Object.Destroy(gun.gameObject);
            }
        }
    }

    void DumpSynergies(string[] args) {
        if (!ArgCount(args, 0, 0)) return;
        var db = GameManager.Instance.SynergyManager;
        Console.WriteLine("synergies:");
        foreach (var entry in db.synergies) {
            Console.WriteLine("  - items_or: " + entry.ItemsOR.ToString().ToLowerInvariant());
            Console.WriteLine("    guns_or: " + entry.GunsOR.ToString().ToLowerInvariant());
            if (entry.statModifiers.Count == 0) {
                Console.WriteLine("    stat_mods: [] ");
            } else {
                Console.WriteLine("    stat_mods:");
                foreach (var mod in entry.statModifiers) {
                    Console.WriteLine("      - stat: " + (int)mod.statToBoost);
                    Console.WriteLine("        operation: " + (int)mod.modifyType);
                    Console.WriteLine("        amount: " + mod.amount.ToStringInvariant());
                }
            }
            if (entry.itemIDs.Count == 0) {
                Console.WriteLine("    items: []");
            } else {
                Console.WriteLine("    items:");
                foreach (var item in entry.itemIDs) {
                    var pickupobject = PickupObjectDatabase.Instance.InternalGetById(item);
                    Console.WriteLine("      - name: " + pickupobject.EncounterNameOrDisplayName ?? "null");
                    Console.WriteLine("        id: " + item);
                }
            }
            if (entry.gunIDs.Count == 0) {
                Console.WriteLine("    guns: []");
            } else {
                Console.WriteLine("    guns:");
                foreach (var item in entry.gunIDs) {
                    var pickupobject = PickupObjectDatabase.Instance.InternalGetById(item);
                    Console.WriteLine("      - name: " + pickupobject.EncounterNameOrDisplayName ?? "null");
                    Console.WriteLine("        id: " + item);
                }
            }
            if (entry.bonusSynergies.Count == 0) {
                Console.WriteLine("    bonus: []");
            } else {
                Console.WriteLine("    bonus:");
                foreach (var synergy in entry.bonusSynergies) {
                    Console.WriteLine("      - " + (int)synergy);
                }
            }
        }
    }

    void DumpPickups(string[] args) {
        if (!ArgCount(args, 0, 0)) return;

        foreach (var entry in PickupObjectDatabase.Instance.Objects) {
            if (entry != null) {
                var id = entry.PickupObjectId;
                string name = null;
                string desc = null;
                try {
                    name = entry.EncounterNameOrDisplayName ?? "[null]";
                } catch {
                    name = "[error]";
                }
                try {
                    desc = entry.encounterTrackable?.journalData?.GetNotificationPanelDescription() ?? "[null]";
                } catch {
                    desc = "[error]";
                }
                Console.WriteLine($"{id}: {name}: {desc}");
            }
        }
    }
}