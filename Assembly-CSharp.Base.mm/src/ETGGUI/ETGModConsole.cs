#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public class ETGModConsole : IETGModMenu {

    /// <summary>
    /// All commands supported by the ETGModConsole. Add your own commands here!
    /// </summary>
    public static ConsoleCommandGroup Commands = new ConsoleCommandGroup(delegate (string[] args) {
      LoggedText.Add("Command or group " + args[0] + " doesn't exist");
    });

    /// <summary>
    /// All items in the game, name sorted. Used for the give command.
    /// </summary>
    public static Dictionary<string, int> AllItems = new Dictionary<string, int>();

    /// <summary>
    /// All console logged text lines. Feel free to add your lines here!
    /// </summary>
    public static List<string> LoggedText = new List<string>();
    /// <summary>
    /// The currently typed in command in the text box.
    /// </summary>
    public static string CurrentTextFieldText = "";

    public static bool StatSetEnabled = false;

    public static Vector2 MainScrollPos;
    public static Vector2 CorrectScrollPos;

    private Rect _MainBoxRect           = new Rect(16f,                 16f , Screen.width - 32f, Screen.height - 32f );
    private Rect _InputBox              = new Rect(16f, Screen.height - 32f , Screen.width - 32f,                 32f );
    private Rect _AutocompletionBox     = new Rect(16f, Screen.height - 184f, Screen.width - 32f,                 120f);

    private bool _CloseConsoleOnCommand = false;
    private bool _CutInputFocusOnCommand = false;

    private bool _AutocompleteOnNextFrame = false;

    private bool _NeedCorrectInput = false;

    private string[] _CurrentAutocompletionData = null;

    private static bool _FocusOnInputBox = true;

    private static char[] _SplitArgsCharacters = new char[] {' '};

    private static AutocompletionSettings _GiveAutocompletionSettings = new AutocompletionSettings(delegate(string input) {
        List<string> ret = new List<string>();
        foreach (string key in AllItems.Keys) {
            if (key.AutocompletionMatch(input.ToLower())) {
                ret.Add(key);
            }
        }
        return ret.ToArray();
    });

    private static AutocompletionSettings _StatAutocompletionSettings = new AutocompletionSettings(delegate(string input) {
        List<string> ret = new List<string>();
        foreach (string key in Enum.GetNames(typeof(TrackedStats))) {
            if (key.AutocompletionMatch(input.ToUpper())) {
                ret.Add(key.ToLower());
            }
        }
        return ret.ToArray();
    });

    public void Start() {
        // GLOBAL NAMESPACE
        Commands
                .AddUnit ("help", delegate(string[] args) {
                    List<List<string>> paths = Commands.ConstructPaths();
                    for (int i = 0; i < paths.Count; i++) {
                        LoggedText.Add(string.Join(" ", paths[i].ToArray()));
                    }
                })
                .AddUnit ("exit", (string[] args) => ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None)
                .AddUnit ("give", GiveItem, _GiveAutocompletionSettings)
                .AddUnit ("screenshake", SetShake)
                .AddUnit ("echo",        Echo    )
                .AddUnit ("tp",          Teleport)
                .AddUnit ("clear",       (string[] args) => LoggedText.Clear())
                .AddUnit ("godmode", delegate(string[] args) {
                    GameManager.Instance.PrimaryPlayer.healthHaver.IsVulnerable = SetBool(args, GameManager.Instance.PrimaryPlayer.healthHaver.IsVulnerable);
                });

        // STAT NAMESPACE
        Commands.AddGroup("stat");

        Commands.GetGroup("stat")
                .AddUnit ("get",  StatGet, _StatAutocompletionSettings)
                .AddGroup("set",  StatSetCurrentCharacter, _StatAutocompletionSettings)
                .AddUnit ("mod",  StatMod, _StatAutocompletionSettings)
                .AddUnit ("list", StatList);

        Commands.GetGroup ("stat").GetGroup ("set")
                                  .AddUnit  ("session", StatSetSession, _StatAutocompletionSettings);

        // ROLL NAMESPACE
        Commands.AddGroup ("roll");

        Commands.GetGroup ("roll")
                .AddUnit  ("distance", DodgeRollDistance)
                .AddUnit  ("speed",    DodgeRollSpeed   );

        // TEST NAMESPACE
        Commands.AddUnit  ("test", new ConsoleCommandGroup());

        Commands.GetGroup ("test")
                .AddGroup ("spawn", SpawnGUID)
                .AddGroup ("resources");

        //// TEST.RESOURCES NAMESPACE
        Commands.GetGroup ("test").GetGroup ("resources")
                .AddUnit  ("load", ResourcesLoad);

        //// TEST.SPAWN NAMESPACE
        Commands.GetGroup ("test").GetGroup ("spawn")
                .AddUnit  ("chest", SpawnChest);

        // DUMP NAMESPACE
        Commands.AddUnit  ("dump", new ConsoleCommandGroup());

        Commands.GetGroup ("dump")
                .AddGroup ("sprites",      (args) => SetBool(args, ref ETGMod.Assets.DumpSprites        ))
                .AddUnit  ("packer",       (args) => ETGMod.Assets.Dump.DumpPacker());

        Commands.GetGroup ("dump").GetGroup ("sprites")
                .AddUnit  ("metadata", (args) => SetBool (args, ref ETGMod.Assets.DumpSpritesMetadata));

        // CONF NAMESPACE
        Commands.AddGroup ("conf");

        Commands.GetGroup ("conf")
                .AddUnit  ("close_console_on_command", (args) => SetBool (args, ref _CloseConsoleOnCommand))
                .AddUnit  ("cut_input_focus_on_command", (args) => SetBool (args, ref _CutInputFocusOnCommand))
                .AddUnit  ("enable_damage_indicators", (args) => SetBool (args, ref ETGModGUI.UseDamageIndicators))
                .AddUnit  ("match_contains", (args) => SetBool (args, ref AutocompletionSettings.MatchContains))
                .AddUnit  ("enable_achievements", (args) => SetBool (args, ref ETGMod.Platform.EnableAchievements))
                .AddUnit  ("enable_stat_set", (args) => SetBool(args, ref StatSetEnabled));
    }

    public void Update() {

    }

    public TextEditor GetTextEditor() {
        return (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
    }

    public string[] SplitArgs(string args) {
        return args.Split (_SplitArgsCharacters, StringSplitOptions.RemoveEmptyEntries);
    }

    public void OnGUI() {

        TextEditor te = GetTextEditor();
        bool rancommand = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && CurrentTextFieldText.Length > 0;
        if (rancommand) {
            string[] input = SplitArgs(te.text.Trim());
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

        //GUI.skin=skin;

        //THIS HAS TO BE CALLED TWICE, once on input, and once the frame after!
        //For some reason?....

        if (_NeedCorrectInput) {
            TextEditor texteditor = GetTextEditor ();
            if (texteditor != null) {
                texteditor.MoveTextEnd();
            }
            _NeedCorrectInput=false;
        }

        //Input
        GUI.SetNextControlName("CommandBox");
        string textfieldvalue = GUI.TextField(_InputBox, CurrentTextFieldText);
        if (textfieldvalue != CurrentTextFieldText) {
            _OnTextChanged (CurrentTextFieldText, textfieldvalue);
            CurrentTextFieldText = textfieldvalue;
        }

        if (_FocusOnInputBox) {
            GUI.FocusControl ("CommandBox");
            _FocusOnInputBox = false;
        }

        _MainBoxRect       = new Rect(16,                      16 , Screen.width - 32, Screen.height - 32 - 29 );
        _InputBox          = new Rect(16, Screen.height - 16 - 24 , Screen.width - 32,                      24 );
        _AutocompletionBox = new Rect(16, Screen.height - 16 - 144, Screen.width - 32,                     120 );

        GUI.Box(_MainBoxRect   , "Console");

        //Logged text
        GUILayout.BeginArea(_MainBoxRect);
        MainScrollPos = GUILayout.BeginScrollView(MainScrollPos);

        for (int i = 0; i < LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (_AutocompleteOnNextFrame) {
            // HACK
            // This is how you set the cursor position...
            te.cursorIndex = te.text.Length;
            te.selectIndex = te.text.Length;

            _AutocompleteOnNextFrame = false;
        }

        if (_CurrentAutocompletionData != null) {
            GUI.Box(_AutocompletionBox, "Autocompletion");

            GUILayout.BeginArea(_AutocompletionBox);
            CorrectScrollPos = GUILayout.BeginScrollView(CorrectScrollPos);

            for(int i = 0; i < _CurrentAutocompletionData.Length; i++) {
                GUILayout.Label(_CurrentAutocompletionData[i]);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // AUTO COMPLETIONATOR 3000
        // (by Zatherz)
        //
        // TODO: Make Tab autocomplete to the shared part of completions
        // TODO: AutocompletionRule interface and class per rule?
        var autocompletionrequested = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab;
        if (autocompletionrequested) {
            // Create an input array by splitting it on spaces
            string inputtext = te.text.Substring (0, te.cursorIndex);
            string[] input = SplitArgs (inputtext);
            string otherinput = string.Empty;
            if (te.cursorIndex < te.text.Length) {
                otherinput = te.text.Substring (te.cursorIndex + 1);
            }
            // Get where the command appears in the path so that we know where the arguments start
            int commandindex = Commands.GetFirstNonUnitIndexInPath (input);
            List<string> pathlist = new List<string> ();
            for (int i = 0; i < input.Length - (input.Length - commandindex); i++) {
                pathlist.Add (input [i]);
            }

            string[] path = pathlist.ToArray ();

            ConsoleCommandUnit unit = Commands.GetUnit (path);
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
            if (!inputtext.EndsWith (" ")) {
                matchindex--;
                matchkeyword = input[input.Length - 1];
            }

            string[] completions = unit.Autocompletion.Match (matchindex, matchkeyword);

            if (completions == null) {
                Debug.Log ("ETGModConsole: no completions available (match returned null)");
            } else if (completions.Length == 1) {
                if (path.Length > 0) {
                    CurrentTextFieldText = string.Join (" ", path) + " " + completions [0] + " " + otherinput;
                } else {
                    CurrentTextFieldText = completions [0] + " " + otherinput;
                }

                _AutocompleteOnNextFrame = true;
            } else if (completions.Length > 1) {
                _CurrentAutocompletionData = completions;
            }
        }

        //Command handling
        if (rancommand && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) {

            //If this command is valid
            // No new line when we ran a command.
            CurrentTextFieldText="";
            if (_CutInputFocusOnCommand)
                GUI.FocusControl("");
            if (_CloseConsoleOnCommand) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }


    }

    public void OnOpen() { }

    public void OnClose() {
        _FocusOnInputBox = true;
    }

    public void OnDestroy() { }

    private void _OnTextChanged(string previous, string current) {
        _CurrentAutocompletionData = null;
    }

    // Use like this:
    //     if (!ArgCount(args, MIN_ARGS, OPTIONAL_MAX_ARGS)) return;
    // Automatically handles error reporting for you
    // ALL VALUES INCLUSIVE!
    public static bool ArgCount(string[] args, int min) {
        if (args.Length >= min) return true;
        LoggedText.Add ("Error: need at least " + min + " argument(s)");
        return false;
    }

    public static bool ArgCount(string[] args, int min, int max) {
        if (args.Length >= min && args.Length <= max) return true;
        if (min == max) {
            LoggedText.Add ("Error: need exactly " + min + " argument(s)");
        } else {
            LoggedText.Add ("Error: need between " + min + " and " + max + " argument(s)");
        }
        return false;
    }

    /// <summary>
    /// Runs the provided command with the provided args.
    /// </summary>
    public static void RunCommand(string[] unit, string[] args) {
        ConsoleCommandUnit command = Commands.GetUnit (unit);
        if (command == null) {
            if (Commands.GetGroup (unit) == null) {
                LoggedText.Add ("Command doesn't exist");
            }
        } else {
            try {
                command.RunCommand (args);
            } catch (Exception e) {
                LoggedText.Add (e.ToString ());
            }
        }
        CurrentTextFieldText = string.Empty;
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
        LoggedText.Add(str);
    }

    void DodgeRollDistance(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.distance = float.Parse(args[0]);
        }
    }

    void DodgeRollSpeed(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.time = float.Parse(args[0]);
        }
    }

    void Teleport(string[] args) {
        if (!ArgCount (args, 2, 2)) return;

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.TeleportToPoint(new Vector2(
                float.Parse(args[0]),
                float.Parse(args[1])
            ),true);
        }
    }

    public void SetBool(string[] args, ref bool value) {
        value = SetBool(args, value);
    }
    public bool SetBool(string[] args, bool fallbackValue) {
        if (args.Length!=1)
            return fallbackValue;

        if (args[0].ToLower()=="true")
            return true;
        else if (args[0].ToLower()=="false")
            return false;
        else
            return fallbackValue;
    }

    void GiveItem(string[] args) {
        if (!ArgCount (args, 1, 2)) return;

        if (!GameManager.Instance.PrimaryPlayer) {
            LoggedText.Add ("Couldn't access Player Controller");
            return;
        }

        int id = -1;

        try {
            id = int.Parse(args[0]);
        }
        catch {
            //Arg isn't an id, so it's probably an item name.
            // Are you Brent?
            id = AllItems[args[0]];
        }

        if (id==-1) {
            LoggedText.Add("Invalid item ID/name!");
            return;
        }

        LoggedText.Add ("Attempting to spawn item ID " + args[0] + " (numeric " + id.ToString() + ")" + ", class " + PickupObjectDatabase.GetById (id).GetType());

        if (args.Length == 2) {
            int count = int.Parse(args[1]);

            for (int i = 0; i<count; i++)
                ETGMod.Player.GiveItemID(id);
        } else {
            ETGMod.Player.GiveItemID(id);
        }
    }

    void SetShake(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        LoggedText.Add ("Vlambeer set to " + args[0]);
        ScreenShakeSettings.GLOBAL_SHAKE_MULTIPLIER = float.Parse (args [0]);
    }

    void SpawnGUID(string[] args) {
        if (!ArgCount (args, 1, 2)) return;
        AIActor enemyPrefab = EnemyDatabase.GetOrLoadByGuid (args[0]);
        if (enemyPrefab == null) {
            LoggedText.Add("GUID " + args[0] + " doesn't exist");
            return;
        }
        LoggedText.Add ("Spawning GUID " + args[0]);
        int count = 1;
        if (args.Length > 1) {
            bool success = int.TryParse (args[1], out count);
            if (!success) {
                LoggedText.Add ("Second argument must be an integer (number)");
                return;
            }
        }
        for (int i = 0; i < count; i++) {
            IntVector2? targetCenter = new IntVector2? (GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2 (VectorConversions.Floor));
            Pathfinding.CellValidator cellValidator = delegate (IntVector2 c) {
                for (int j = 0; j < enemyPrefab.Clearance.x; j++) {
                    for (int k = 0; k < enemyPrefab.Clearance.y; k++) {
                        if (GameManager.Instance.Dungeon.data.isTopWall (c.x + j, c.y + k)) {
                            return false;
                        }
                        if (targetCenter.HasValue) {
                            if (IntVector2.Distance (targetCenter.Value, c.x + j, c.y + k) < 4) {
                                return false;
                            }
                            if (IntVector2.Distance (targetCenter.Value, c.x + j, c.y + k) > 20) {
                                return false;
                            }
                        }
                    }
                }
                return true;
            };
            IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell (new IntVector2? (enemyPrefab.Clearance), new Dungeonator.CellTypes? (enemyPrefab.PathableTiles), false, cellValidator);
            if (randomAvailableCell.HasValue) {
                AIActor aIActor = AIActor.Spawn (enemyPrefab, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                aIActor.HandleReinforcementFallIntoRoom (0);
            }
        }
    }

    void SpawnChest(string[] args) {
        if (!ArgCount (args, 1, 2)) return;
        Dungeonator.RoomHandler currentRoom = GameManager.Instance.PrimaryPlayer.CurrentRoom;
        RewardManager rewardManager = GameManager.Instance.RewardManager;
        Chest chest;
        bool glitched = false;
        string name = args [0].ToLower ();
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
            LoggedText.Add ("Chest type " + args [0] + " doesn't exist! Valid types: brown, blue, green, red, black, rainbow");
            return;
        }
        WeightedGameObject wGameObject = new WeightedGameObject ();
        wGameObject.gameObject = chest.gameObject;
        WeightedGameObjectCollection wGameObjectCollection = new WeightedGameObjectCollection ();
        wGameObjectCollection.Add (wGameObject);
        int count = 1;
        float origMimicChance = chest.overrideMimicChance;
        chest.overrideMimicChance = 0f;
        if (args.Length > 1) {
            bool success = int.TryParse (args[1], out count);
            if (!success) {
                LoggedText.Add ("Second argument must be an integer (number)");
                return;
            }
        }
        for (int i = 0; i < count; i++) {
            Chest spawnedChest = currentRoom.SpawnRoomRewardChest (wGameObjectCollection, currentRoom.GetBestRewardLocation (new IntVector2 (2, 1), Dungeonator.RoomHandler.RewardLocationStyle.PlayerCenter, true));
            spawnedChest.ForceUnlock ();
            if (glitched) {
                spawnedChest.BecomeGlitchChest ();
            }
        }
        chest.overrideMimicChance = origMimicChance;
    }

    void ResourcesLoad(string[] args) {
        if (!ArgCount (args, 1)) return;
        string resourcepath = string.Join(" ", args);
        object resource = Resources.Load(resourcepath);
        if (resource == null) {
            LoggedText.Add("Couldn't load resource " + resourcepath);
            return;
        }
        LoggedText.Add("Loaded (and threw away) " + resourcepath);
    }

    private TrackedStats? _GetStatFromString(string statname) {
        TrackedStats stat;
        try {
            stat = (TrackedStats)Enum.Parse(typeof(TrackedStats), statname);
        } catch {
            return null;
        }
        return stat;
    }

    private bool _VerifyStatSetEnabled(string command) {
        if (!StatSetEnabled) {
            LoggedText.Add ("The '" + command + "' command is disabled by default!");
            LoggedText.Add ("This command can be very damaging as it sets arbitrary values in your save file.");
            LoggedText.Add ("There is *no way* to undo this action.");
            LoggedText.Add ("If someone told you to run this command, there is a high chance they are");
            LoggedText.Add ("maliciously trying to destroy your save file.");
            LoggedText.Add ("This command can also cause achievements to be unlocked. Achievements are");
            LoggedText.Add ("disabled by default, but they can be enabled with 'conf enable_achievements true'.");
            LoggedText.Add ("If you are *CERTAIN* that you want to run this command, you can enable it by");
            LoggedText.Add ("running the following command: 'conf enable_stat_set true'.");
            LoggedText.Add ("");
            LoggedText.Add ("Be careful.");
            return false;
        }
        return true;
    }

    void StatGet(string[] args) {
        if (!ArgCount (args, 1)) return;
        TrackedStats? stat = _GetStatFromString(args [0].ToUpper ());
        if (!stat.HasValue) {
            LoggedText.Add ("The stat isn't a real TrackedStat");
            return;
        }
        if (GameManager.Instance.PrimaryPlayer != null) {
            float characterstat = GameStatsManager.Instance.GetCharacterStatValue (stat.Value);
            LoggedText.Add ("Character: " + characterstat);
            float sessionstat = GameStatsManager.Instance.GetSessionStatValue (stat.Value);
            LoggedText.Add ("Session: " + sessionstat);
        } else {
            LoggedText.Add ("Character and Session stats are unavailable, please select a character first");
        }
        float playerstat = GameStatsManager.Instance.GetPlayerStatValue (stat.Value);
        LoggedText.Add ("This save file: " + playerstat);
    }

    void StatSetSession(string[] args) {
        if (!_VerifyStatSetEnabled ("stat set session")) return;
        if (!ArgCount (args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args [0].ToUpper ());
        if (!stat.HasValue) {
            LoggedText.Add ("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse (args [1], out value)) {
            LoggedText.Add ("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.SetStat (stat.Value, value);
    }

    void StatSetCurrentCharacter(string[] args) {
        if (!_VerifyStatSetEnabled ("stat set")) return;
        if (!ArgCount (args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args [0].ToUpper ());
        if (!stat.HasValue) {
            LoggedText.Add ("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse (args [1], out value)) {
            LoggedText.Add ("The value isn't a proper number");
            return;
        }
        PlayableCharacters currentCharacter = GameManager.Instance.PrimaryPlayer.characterIdentity;
        GameStatsManager.Instance.m_characterStats [currentCharacter].SetStat (stat.Value, value);
    }

    void StatMod(string[] args) {
        if (!_VerifyStatSetEnabled ("stat mod")) return;
        if (!ArgCount (args, 2)) return;
        TrackedStats? stat = _GetStatFromString(args [0].ToUpper ());
        if (!stat.HasValue) {
            LoggedText.Add ("The value isn't a proper number");
            return;
        }
        float value;
        if (!float.TryParse (args [1], out value)) {
            LoggedText.Add ("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.RegisterStatChange (stat.Value, value);
    }

    void StatList(string[] args) {
        if (!ArgCount (args, 0)) return;
        foreach (var value in Enum.GetValues(typeof(TrackedStats))) {
            LoggedText.Add (value.ToString().ToLower());
        }
    }
}

