#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System;
using Object = UnityEngine.Object;

public class ETGModConsole : IETGModMenu {

    /// <summary>
    /// All commands supported by the ETGModConsole. Add your own commands here!
    /// </summary>
    public static Dictionary<string, ConsoleCommand> Commands = new Dictionary<string, ConsoleCommand>();

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
    public static string CurrentCommand = "";

    public static Vector2 MainScrollPos;
    public static Vector2 CorrectScrollPos;

    private Rect _MainBoxRect     = new Rect(16,                 16 , Screen.width - 32, Screen.height - 32 );
    private Rect _InputBox        = new Rect(16, Screen.height - 32 , Screen.width - 32,                 32 );
    private Rect _AutoCorrectBox  = new Rect(16, Screen.height - 184, Screen.width - 32,                 120);

    private bool _CloseConsoleOnCommand = false;
    private bool _CutInputFocusOnCommand = false;

    private bool _NeedCorrectInput=false;

    string[] DisplayedCorrectCommands = new string[] { }, displayCorrectArguments=new string[] { };

    public void Start() {


        //Add commands
        Commands["help"]=new ConsoleCommand("help",delegate (string[] args) { foreach (KeyValuePair<string, ConsoleCommand> kvp in Commands) LoggedText.Add(kvp.Key); });

        Commands["exit"] = Commands["hide"] = Commands["quit"]   = new ConsoleCommand("<exit, hide, quit>", (string[] args) => ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None  );
        Commands["log"] = Commands["echo"]                       = new ConsoleCommand("<log, echo>"       , Echo                                                                  );
        Commands["roll_distance"]                                = new ConsoleCommand("roll_distance"     , DodgeRollDistance                                                     );
        Commands["roll_speed"]                                   = new ConsoleCommand("roll_speed"        , DodgeRollSpeed                                                        );
        Commands["tp"] = Commands["teleport"]                    = new ConsoleCommand("<tp, teleport>"    , Teleport                                                              );

        Commands["close_console_on_command"]   = new ConsoleCommand("close_console_on_command",   delegate (string[] args) { _CloseConsoleOnCommand          = SetBool(args, _CloseConsoleOnCommand        ); });
        Commands["cut_input_focus_on_command"] = new ConsoleCommand("cut_input_focus_on_command", delegate (string[] args) { _CutInputFocusOnCommand         = SetBool(args, _CutInputFocusOnCommand       ); });
        Commands["enable_damage_indicators"]   = new ConsoleCommand("enable_damage_indicators",   delegate (string[] args) { ETGModGUI.UseDamageIndicators  = SetBool(args, ETGModGUI.UseDamageIndicators); });

        Commands["set_shake"] = new ConsoleCommand("set_shake" ,SetShake );
        Commands["give"]      = new ConsoleCommand("give"      ,GiveItem);

    }

    public void Update() {

    }

    public void OnGUI() {

        //GUI.skin=skin;

        //THIS HAS TO BE CALLED TWICE, once on input, and once the frame after!
        //For some reason?....
        if (_NeedCorrectInput) {
            TextEditor txt = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

            if (txt!=null) {
                txt.MoveTextEnd();
            }
            _NeedCorrectInput=false;
        }

        bool ranCommand = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && CurrentCommand.Length > 0;
        if (ranCommand) {

            string[] splitCommand = CurrentCommand.Split(' ');

            if (!Commands.ContainsKey(splitCommand[0])) {
                //If there's no matching command for what the user has typed, we'll try to auto-correct it.
                List<string> validStrings = new List<string>();

                validStrings.AddRange(DisplayedCorrectCommands);

                for (int i = 0; i<CurrentCommand.Length; i++) {
                    List<string> toRemove = new List<string>();

                    for (int j = 0; j<validStrings.Count; j++) 
                        if (validStrings[j][i]!=CurrentCommand[i])
                            toRemove.Add(validStrings[j]);
                    

                    foreach (string s in toRemove)
                        validStrings.Remove(s);

                    if (validStrings.Count==1)
                        break;
                    else if (validStrings.Count==0) {
                        LoggedText.Add("There's no matching command for "+'"'+CurrentCommand+'"');
                        break;
                    }
                }

                if (validStrings.Count>0) {
                    CurrentCommand=validStrings[0] + " ";

                    //Move selection to end of text
                    TextEditor txt = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

                    if (txt!=null) {
                        txt.MoveTextEnd();
                    }
                    _NeedCorrectInput=true;
                }

                ranCommand=false;
            } else if (!Commands[splitCommand[0]].IsCommandCorrect(splitCommand)) {

                try {
                    //If the parameters don't match, we auto-correct those too.

                    int currCommandIndex = splitCommand.Length-1;

                    List<string> validStrings = new List<string>();

                    validStrings.AddRange(displayCorrectArguments);

                    for (int i = 0; i<splitCommand[currCommandIndex].Length; i++) {
                        List<string> toRemove = new List<string>();

                        for (int j = 0; j<validStrings.Count; j++) {
                            if (validStrings[j][i]!=splitCommand[currCommandIndex][i])
                                toRemove.Add(validStrings[j]);
                        }

                        foreach (string s in toRemove)
                            validStrings.Remove(s);

                        if (validStrings.Count==1) {
                            break;
                        } else if (validStrings.Count==0) {
                            LoggedText.Add("There's no matching argument for "+'"'+CurrentCommand+'"');
                            break;
                        }
                    }

                    if (validStrings.Count>0) {
                        splitCommand[splitCommand.Length-1]=validStrings[0];
                        CurrentCommand="";
                        for (int i = 0; i<splitCommand.Length; i++) {
                            CurrentCommand+=( i==0 ? ""  : " ") + splitCommand[i];
                        }

                        CurrentCommand+=" ";

                        //Move selection to end of text
                        TextEditor txt = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

                        if (txt!=null) {
                            txt.MoveTextEnd();
                        }
                        _NeedCorrectInput=true;
                    }
                } catch (System.Exception e) {
                    LoggedText.Add(e.ToString());
                }
                ranCommand=false;
            } else  {
                //If we're all good, run the command!

                RunCommand();
            }
        }

        _MainBoxRect    = new Rect(16,                      16 , Screen.width - 32, Screen.height - 32 - 29 );
        _InputBox       = new Rect(16, Screen.height - 16 - 24 , Screen.width - 32,                      24 );
        _AutoCorrectBox = new Rect(16, Screen.height - 16 - 144, Screen.width - 32,                     120 );

        GUI.Box(_MainBoxRect   , "Console");

        //Input
        string changedCommand=GUI.TextField(_InputBox, CurrentCommand);

        if(changedCommand != CurrentCommand) {
            CurrentCommand=changedCommand;
            _OnTextChanged();
        }

        //Logged text
        GUILayout.BeginArea(_MainBoxRect);
        MainScrollPos = GUILayout.BeginScrollView(MainScrollPos);

        for (int i = 0; i < LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        //Auto-correct box.
        if(CurrentCommand.Length>0){
            GUI.Box(_AutoCorrectBox, "Auto-Correct");

            GUILayout.BeginArea(_AutoCorrectBox);
            CorrectScrollPos=GUILayout.BeginScrollView(CorrectScrollPos);

            for(int i = 0; i < DisplayedCorrectCommands.Length; i++) {
                GUILayout.Label(DisplayedCorrectCommands[i]);
            }

            for(int i = 0; i <displayCorrectArguments.Length; i++) {
                GUILayout.Label(displayCorrectArguments[i]);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        //Command handling
        if (ranCommand && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) {

            //If this command is valid
            // No new line when we ran a command.
            CurrentCommand="";
            if (_CutInputFocusOnCommand)
                GUI.FocusControl("");
            if (_CloseConsoleOnCommand) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }


    }

    public void OnDestroy() {

    }

    private void _OnTextChanged() {

        //Set auto-correct data

        if (CurrentCommand.Length>0) {
            string[] seperatedCommand = CurrentCommand.Split(' ');

            //If there's no arguments.....
            if (seperatedCommand.Length==1) {
                GUILayout.Label("Command");
                displayCorrectArguments=new string[] { };
                List<string> avaliablCommands = new List<string>();

                foreach (string s in Commands.Keys) {
                    if (s.Contains(seperatedCommand[0]))
                        avaliablCommands.Add(s);
                }

                DisplayedCorrectCommands=avaliablCommands.ToArray();
            } else {
                DisplayedCorrectCommands=new string[] { };
                GUILayout.Label("Args");
                //If there's arguments....
                List<string> avaliablCommands = new List<string>();

                GUILayout.Label(Commands[seperatedCommand[0]].AcceptedArguments.ToString());

                if (Commands[seperatedCommand[0]].AcceptedArguments==null)
                    return;

                GUILayout.Label(Commands[seperatedCommand[0]].AcceptedArguments[0].Length.ToString());

                for (int i = 0; i<seperatedCommand.Length-1; i++) {
                    //Index of the argument in the seperated command. i is the index in the arguments array.
                    int realIndex = i+1;

                    if (Commands[seperatedCommand[0]].AcceptedArguments.Length<=i)
                        break;

                    foreach (string s in Commands[seperatedCommand[0]].AcceptedArguments[i]) {
                        if (s.Contains(seperatedCommand[realIndex]))
                            avaliablCommands.Add(s);
                    }
                }

                displayCorrectArguments=avaliablCommands.ToArray();
            }
        }

    }

    /// <summary>
    /// Runs the currently typed in command.
    /// </summary>
    public static void RunCommand() {
        try {
            RunCommand(CurrentCommand.TrimEnd(' '));
        } catch (System.Exception e){
            LoggedText.Add(e.ToString());
        }
        CurrentCommand = string.Empty;
    }


    private readonly static string[] _EmptyStringArray = new string[0];
    /// <summary>
    /// Runs a given command.
    /// </summary>
    /// <param name="command">Command to run.</param>
    public static void RunCommand(string command) {
        string[] parts;
        string[] args;

        if (command.Contains(" ")) {
            parts = command.Split(' ');
            args = new string[parts.Length - 1];

            for (int i = 1; i < parts.Length; i++) {
                args[i - 1] = parts[i];
            }
        } else {
            parts = new string[] { command };
            args = _EmptyStringArray;
        }

        if (Commands.ContainsKey(parts[0])) {
            Commands[parts[0]].RunCommand(args);
            LoggedText.Add("Executed command " + parts[0]);
        } else {
            LoggedText.Add("Command " + parts[0] + " not found.");
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
        LoggedText.Add(str);
    }

    void DodgeRollDistance(string[] args) {
        if (args.Length != 1) {
            return;
        }
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.distance = float.Parse(args[0]);
        }
    }

    void DodgeRollSpeed(string[] args) {
        if (args.Length != 1) {
            return;
        }
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.time = float.Parse(args[0]);
        }
    }

    void Teleport(string[] args) {
        if (args.Length != 3) {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.transform.position = new Vector3(
                float.Parse(args[0]),
                float.Parse(args[1]),
                float.Parse(args[2])
            );
        }
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
        if (args.Length < 1 || args.Length > 2) {
            LoggedText.Add ("Command requires 1-2 arguments (int|string, int)");
            return;
        }
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

        if (args.Length==2) {
            int count = int.Parse(args[1]);

            for (int i = 0; i<count; i++)
                ETGMod.Player.GiveItemID(id);
        } else {
            ETGMod.Player.GiveItemID(id);
        }
    }

    void SetShake(string[] args) {
        if (args.Length != 1) {
            LoggedText.Add ("Command requires 1 argument (int)");
            return;
        }
        LoggedText.Add ("Vlambeer set to " + args[0]);
        ScreenShakeSettings.GLOBAL_SHAKE_MULTIPLIER = float.Parse (args [0]);
    }

}

