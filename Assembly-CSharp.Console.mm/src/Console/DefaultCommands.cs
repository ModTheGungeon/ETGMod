using System;
using System.Text;
using ETGMod.GUI;

namespace ETGMod.Console {
    public partial class Console : Backend {
        private Logger.Subscriber _LoggerSubscriber;

        // for the debug/mods command
        private void _GetModInfo(StringBuilder builder, ModLoader.ModInfo info, string indent = "") {
            builder.AppendLine($"{indent}- {info.Name}: {info.Resources.ResourceCount} resources");
            foreach (var mod in info.EmbeddedMods) {
                if (mod.Parent == info) {
                    _GetModInfo(builder, mod, indent + "  ");
                }
            }
        }

        internal void AddDefaultCommands() {
            _LoggerSubscriber = (logger, loglevel, indent, str) => {
                PrintLine(logger.String(loglevel, str, indent: indent), color: _LoggerColors[loglevel]);
            };


            AddCommand("!!", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Execute(histindex.Value - 1);
            });

            AddCommand("!'", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Entries[histindex.Value - 1];
            });

            AddCommand("echo", (args) => {
                return string.Join(" ", args.ToArray());
            }).WithSubCommand("hello", (args) => {
                return "Hello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!";
            });

            AddGroup("debug")
                .WithSubCommand("parser-bounds-test", (args) => {
                    var text = "echo Hello! \"Hello world!\" This\\ is\\ great \"It\"works\"with\"\\ wacky\" stuff\" \\[\\] \"\\[\\]\" [e[echo c][echo h][echo [echo \"o\"]] \"hel\"[echo lo][echo !]]";
                    CurrentCommandText = text;
                    return null;
                })
                .WithSubCommand("mods", (args) => {
                    var s = new StringBuilder();

                    s.AppendLine("Loaded mods:");
                    foreach (var mod in ETGMod.ModLoader.LoadedMods) {
                        _GetModInfo(s, mod);
                    }
                    return s.ToString();
                })
                .WithSubCommand("give", (args) => {
                    foreach (var c in ETGMod.Items[args[0]].gameObject.GetComponents(typeof(UnityEngine.Component))) {
                        PrintLine(c.GetType().FullName);
                    }
                    LootEngine.TryGivePrefabToPlayer(ETGMod.Items[args[0]].gameObject, GameManager.Instance.PrimaryPlayer, true);
                    return args[0];
                })
                .WithSubGroup(
                    new Group("dump")
                    .WithSubCommand("items", (args) => {
                        var b = new StringBuilder();
                        var db = PickupObjectDatabase.Instance.Objects;
                        for (int i = 0; i < db.Count; i++) {
                            PickupObject obj = null;
                            string name = null;
                            try {
                                obj = db[i];
                            } catch {
                                name = "[ERROR: failed getting object by index]";
                            }
                            if (obj != null) {
                                try {
                                    var displayname = obj.encounterTrackable.journalData.PrimaryDisplayName;
                                    name = StringTableManager.ItemTable[displayname].GetWeightedString();
                                } catch {
                                    name = "[ERROR: failed getting ammonomicon name]";
                                }
                                if (name == null) {
                                    try {
                                        name = obj.EncounterNameOrDisplayName;
                                    } catch {
                                        name = "[ERROR: failed getting encounter or display name]";
                                    }
                                }
                            }
                            if (name == null && obj != null) {
                                name = "[NULL NAME (but object is not null)]";
                            }

                            if (name != null) {
                                b.AppendLine($"{i}: {name}");
                                _Logger.Info($"{i}: {name}");
                            }
                        }
                        return b.ToString();
                    })
                );

            AddGroup("log")
                .WithSubCommand("sub", (args) => {
                    if (_Subscribed) return "Already subscribed.";
                    Logger.Subscribe(_LoggerSubscriber);
                    _Subscribed = true;
                    return "Done.";
                })
                .WithSubCommand("unsub", (args) => {
                    if (!_Subscribed) return "Not subscribed yet.";
                    Logger.Unsubscribe(_LoggerSubscriber);
                    _Subscribed = false;
                    return "Done.";
                })
                .WithSubCommand("level", (args) => {
                    if (args.Count == 0) {
                        return _LogLevel.ToString().ToLowerInvariant();
                    } else {
                        switch (args[0]) {
                        case "debug": _LogLevel = Logger.LogLevel.Debug; break;
                        case "info": _LogLevel = Logger.LogLevel.Info; break;
                        case "warn": _LogLevel = Logger.LogLevel.Warn; break;
                        case "error": _LogLevel = Logger.LogLevel.Error; break;
                        default: throw new Exception($"Unknown log level '{args[0]}");
                        }
                        return "Done.";
                    }
                });
        }
    }
}