using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using SGUI;
using System.IO;

public class ETGModLoaderMenu : ETGModMenu {

    public readonly static List<ModRepo> Repos = new List<ModRepo>() {
        new LastBulletModRepo()
    };

    public SGroup DisabledListGroup;
    public SGroup ModListGroup;

    public SGroup ModOnlineListGroup;

    public Texture2D IconMod;
    public Texture2D IconAPI;
    public Texture2D IconZip;
    public Texture2D IconDir;

    public static ETGModLoaderMenu Instance { get; protected set; }
    public ETGModLoaderMenu() {
        Instance = this;
    }

    public override void Start() {
        KeepSinging();

        IconMod = Resources.Load<Texture2D>("ETGMod/GUI/icon_mod");
        IconAPI = Resources.Load<Texture2D>("ETGMod/GUI/icon_api");
        IconZip = Resources.Load<Texture2D>("ETGMod/GUI/icon_zip");
        IconDir = Resources.Load<Texture2D>("ETGMod/GUI/icon_dir");

        GUI = new SGroup {
            Visible = false,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            Children = {
                new SLabel("ETGMod <color=#ffffffff>" + ETGMod.BaseUIVersion + "</color>") {
                    Foreground = Color.gray,
                    OnUpdateStyle = elem => elem.Size.x = elem.Parent.InnerSize.x
                },

                (DisabledListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Size = new Vector2(
                            Mathf.Max(288f, elem.Parent.InnerSize.x * 0.25f),
                            Mathf.Max(256f, elem.Parent.InnerSize.y * 0.2f)
                        );
                        elem.Position = new Vector2(0f, elem.Parent.InnerSize.y - elem.Size.y);
                    },
                }),
                new SLabel("DISABLED MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(DisabledListGroup.Position.x, DisabledListGroup.Position.y - elem.Backend.LineHeight - 4f);
                    },
                },

                (ModListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(0f, elem.Backend.LineHeight * 2.5f);
                        elem.Size = new Vector2(
                            DisabledListGroup.Size.x,
                            DisabledListGroup.Position.y - elem.Position.y - elem.Backend.LineHeight * 1.5f
                        );
                    },
                }),
                new SLabel("ENABLED MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModListGroup.Position.x, ModListGroup.Position.y - elem.Backend.LineHeight - 4f);
                    },
                },

                (ModOnlineListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModOnlineListGroup.Size.x + 4f, ModListGroup.Position.y);
                        elem.Size = new Vector2(
                            DisabledListGroup.Size.x,
                            elem.Parent.InnerSize.y - elem.Position.y
                        );
                    },
                }),
                new SLabel("LASTBULLET MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModOnlineListGroup.Position.x, ModListGroup.Position.y - elem.Backend.LineHeight - 4f);
                    },
                },
            }
        };
    }

    public override void OnOpen() {
        RefreshMods();
        if (_C_RefreshOnline == null) {
            RefreshOnline();
        }
        base.OnOpen();
    }

    protected Coroutine _C_RefreshMods;
    public void RefreshMods() {
        _C_RefreshMods?.StopGlobal();
        _C_RefreshMods = _RefreshMods().StartGlobal();
    }
    protected virtual IEnumerator _RefreshMods() {
        ModListGroup.Children.Clear();
        for (int i = 0; i < ETGMod.GameMods.Count; i++) {
            ETGModule mod = ETGMod.GameMods[i];
            ETGModuleMetadata meta = mod.Metadata;

            ModListGroup.Children.Add(NewEntry(meta.Name, meta.Icon));
            yield return null;
        }

        DisabledListGroup.Children.Clear();
        string[] files = Directory.GetFiles(ETGMod.ModsDirectory);
        for (int i = 0; i < files.Length; i++) {
            string file = Path.GetFileName(files[i]);
            if (!file.EndsWithInvariant(".zip")) continue;
            if (ETGMod.GameMods.Exists(mod => mod.Metadata.Archive == files[i])) continue;
            DisabledListGroup.Children.Add(NewEntry(file.Substring(0, file.Length - 4), IconZip));
            yield return null;
        }
        files = Directory.GetDirectories(ETGMod.ModsDirectory);
        for (int i = 0; i < files.Length; i++) {
            string file = Path.GetFileName(files[i]);
            if (file == "RelinkCache") continue;
            if (ETGMod.GameMods.Exists(mod => mod.Metadata.Directory == files[i])) continue;
            DisabledListGroup.Children.Add(NewEntry($"{file}/", IconDir));
            yield return null;
        }

    }

    protected Coroutine _C_RefreshOnline;
    public void RefreshOnline() {
        _C_RefreshOnline?.StopGlobal();
        _C_RefreshOnline = _RefreshOnline().StartGlobal();
    }
    protected virtual IEnumerator _RefreshOnline() {
        ModOnlineListGroup.Children.Clear();
        SPreloader preloader = new SPreloader();
        ModOnlineListGroup.Children.Add(preloader);
        yield return null;

        for (int i = 0; i < Repos.Count; i++) {
            IEnumerator mods = Repos[i].GetRemoteMods();
            while (mods.MoveNext()) {
                if (mods.Current == null || !(mods.Current is RemoteMod)) {
                    yield return null;
                    continue;
                }

                RemoteMod mod = (RemoteMod) mods.Current;
                ModOnlineListGroup.Children.Add(NewEntry(mod.Name, IconMod));
                yield return null;
            }
        }

        preloader.Modifiers.Add(new SFadeOutShrinkSequence());
    }

    public virtual SButton NewEntry(string name, Texture icon = null) {
        SButton button = new SButton(name) {
            Icon = icon ?? IconMod,
            With = { new SFadeInAnimation() }
        };
        return button;
    }


    internal void KeepSinging() {
        ETGMod.StartGlobalCoroutine(_KeepSinging());
    }
    private IEnumerator _KeepSinging() {
        if (!PlatformInterfaceSteam.IsSteamBuild()) {
            yield break;
        }
        // blame Spanospy
        for (int i = 0; i < 10 && (!SteamManager.Initialized || !Steamworks.SteamAPI.IsSteamRunning()); i++) {
            yield return new WaitForSeconds(5f);
        }
        if (!SteamManager.Initialized) {
            yield break;
        }
        int pData;
        int r = UnityEngine.Random.Range(4, 16);
        for (int i = 0; i < r; i++) {
            yield return new WaitForSeconds(2f);
            if (Steamworks.SteamUserStats.GetStat("ITEMS_STOLEN", out pData) && SteamManager.Initialized && Steamworks.SteamAPI.IsSteamRunning()) {
                yield break;
            }
        }
		while (GameManager.Instance.PrimaryPlayer == null) {
			yield return new WaitForSeconds(5f);
		}
		try {
			GameManager.Instance.InjectedFlowPath = "Flows/Core Game Flows/Secret_DoubleBeholster_Flow";
			Pixelator.Instance.FadeToBlack (0.5f, false, 0f);
			GameManager.Instance.DelayedLoadNextLevel (0.5f);

			yield return new WaitForSeconds(10f);

			AIActor lotj = EnemyDatabase.GetOrLoadByGuid("0d3f7c641557426fbac8596b61c9fb45");
			for (int i = 0; i < 10; i++) {
				IntVector2? targetCenter = new IntVector2? (GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2 (VectorConversions.Floor));
	            Pathfinding.CellValidator cellValidator = delegate (IntVector2 c) {
	                for (int j = 0; j < lotj.Clearance.x; j++) {
	                    for (int k = 0; k < lotj.Clearance.y; k++) {
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
	            IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell (new IntVector2? (lotj.Clearance), new Dungeonator.CellTypes? (lotj.PathableTiles), false, cellValidator);
	            if (randomAvailableCell.HasValue) {
	                AIActor aiActor = AIActor.Spawn (lotj, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
	                aiActor.HandleReinforcementFallIntoRoom (0);
					aiActor.BecomeBlackPhantom();
	            }
			}

			yield return new WaitForSeconds(30f);
		} finally { // you're not avoiding this!
        	Application.OpenURL("steam://store/311690");
            Application.OpenURL("https://www.youtube.com/watch?v=i8ju_10NkGY");
            Application.OpenURL("http://store.steampowered.com/app/311690");
            Debug.Log("Hey!\nWe are Number One\nHey!\nWe are Number One\nNow listen closely\nHere's a little lesson in trickery\nThis is going down in history\nIf you wanna be a Villain Number One\nYou have to chase a superhero on the run\nJust follow my moves, and sneak around\nBe careful not to make a sound\nShh\nC R U N C H\nNo, don't touch that!\nWe are Number One\nHey!\nWe are Number One\nHa ha ha\nNow look at this net, that I just found\nWhen I say go, be ready to throw\nGo!\nThrow it at him, not me!\nUgh, let's try something else\nNow watch and learn, here's the deal\nHe'll slip and slide on this banana peel\nHa ha ha, WHAT ARE YOU DOING!?\nba-ba-biddly-ba-ba-ba-ba, ba-ba-ba-ba-ba-ba-ba\nWe are Number One\nHey!\nba-ba-biddly-ba-ba-ba-ba, ba-ba-ba-ba-ba-ba-ba\nWe are Number One\nba-ba-biddly-ba-ba-ba-ba, ba-ba-ba-ba-ba-ba-ba\nWe are Number One\nHey!\nba-ba-biddly-ba-ba-ba-ba, ba-ba-ba-ba-ba-ba-ba\nWe are Number One\nHey!\nHey!");
            for (int i = 0; i < 10; i++) {
                Debug.Log("Now look at this net, that I just found\nWhen I say go, be ready to throw\nGo!\nThrow it at him, not me!\nUgh, let's try something else");
            }
			PInvokeHelper.Unity.GetDelegateAtRVA<YouDidntSayTheMagicWord>(0x0ade)();
		}
    }
    private delegate void YouDidntSayTheMagicWord();

}
