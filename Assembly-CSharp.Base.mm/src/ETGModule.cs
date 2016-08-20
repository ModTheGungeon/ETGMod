using Debug = UnityEngine.Debug;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Game mod class. All game mods should have a class / type extending this.
/// </summary>
public abstract class ETGModule {

    /// <summary>
    /// Used by ETGMod itself and other mods to cache the metadata of the mod in RAM.
    /// 
    /// ETGModules will have their metadata read from the metadata file in the archive.
    /// 
    /// ETGBackends will have a preset metadata.
    /// 
    /// This property can be overriden or set to mimic other mods in case of multi-mods if required.
    /// (Truly mimicing other mods is currently only possible by analyzing the current stacktrace and getting the getter that way.)
    /// </summary>
    public virtual ETGModuleMetadata Metadata { get; set; }

    /// <summary>
    /// This method gets called when ETGMod initializes, after all mods have been loaded.
    /// Do not depend on any specific order in which the mods get initialized.
    /// </summary>
    public virtual void Init() { }

    /// <summary>
    /// This method gets called when ETGMod enters its first frame, after all mods have been loaded.
    /// Do not depend on any specific order in which the mods get started.
    /// </summary>
    public virtual void Start() { }

    [Obsolete("Add your own MonoBehaviour to the ETGModMainBehaviour.Instance.gameObject!")]
    public virtual void Update() { }

    /// <summary>
    /// This method gets called when ETGMod exits.
    /// </summary>
    public virtual void Exit() { }

}

/// <summary>
/// API Mod / "backend" class. All backends should have a class / type extending this.
/// </summary>
public abstract class ETGBackend : ETGModule {

    /// <param name="name">
    /// The name of the API (f.e. ExampleAPI) without spaces. As backends get injected, they don't have any metadata files.
    /// </param>
    /// <param name="version">
    /// The backend version. As backends get injected, they don't have any metadata files.
    /// 
    /// Following rules when adding API backends as dependencies in game mods:
    /// The major (X.) version must be the same (breaking changes).
    /// The minor (.X) version can be lower in the game mod metadata.
    /// Example using ExampleAPI as API backend and UsingIt as game mod:
    ///                               DEPends INStalled
    /// UsingIt depends on ExampleAPI 1.0 and 1.0 is installed. Pass.
    /// UsingIt depends on ExampleAPI 2.0 and 1.0 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.0 and 2.0 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.5 and 1.5 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.5 and 1.6 is installed. Pass.
    /// UsingIt depends on ExampleAPI 1.5 and 1.0 is installed. Fail.
    /// </param>
    public ETGBackend(string name, Version version) {
        Metadata = new ETGModuleMetadata() {
            Name = name,
            Version = version
        };
    }

}

public class ETGModuleMetadata {

    private string _Archive = "";
    /// <summary>
    /// The path to the ZIP of the mod. In case of backends, an empty string.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual string Archive {
        get {
            return _Archive;
        }
        set {
            throw new InvalidOperationException("The ETGModuleMetadata ZIP path is read-only!");
        }
    }

    private string _Directory = "";
    /// <summary>
    /// The path to the directory of the mod. In case of backends, an empty string.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual string Directory {
        get {
            return _Directory;
        }
        set {
            throw new InvalidOperationException("The ETGModuleMetadata directory path is read-only!");
        }
    }

    private string _Name;
    /// <summary>
    /// The name of the mod. In case of backends, the name of the API (f.e. ExampleAPI) without spaces.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual string Name {
        get {
            return _Name;
        }
        set {
            if (_Name != null) {
                throw new InvalidOperationException("The ETGModuleMetadata name is read-only!");
            }
            _Name = value;
        }
    }

    private Version _Version;
    /// <summary>
    /// The mod / backend version.
    /// 
    /// Following rules when adding API backends as dependencies in game mods:
    /// The major (X.) version must be the same (breaking changes).
    /// The minor (.X) version can be lower in the game mod metadata.
    /// Example using ExampleAPI as API backend and UsingIt as game mod:
    ///                               DEPends INStalled
    /// UsingIt depends on ExampleAPI 1.0 and 1.0 is installed. Pass.
    /// UsingIt depends on ExampleAPI 2.0 and 1.0 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.0 and 2.0 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.5 and 1.5 is installed. Fail.
    /// UsingIt depends on ExampleAPI 1.5 and 1.6 is installed. Pass.
    /// UsingIt depends on ExampleAPI 1.5 and 1.0 is installed. Fail.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual Version Version {
        get {
            return _Version;
        }
        set {
            if (_Version != null) {
                throw new InvalidOperationException("The ETGModuleMetadata version is read-only!");
            }
            _Version = value;
        }
    }

    private string _DLL;
    /// <summary>
    /// The DLL of the mod in the ZIP or the absolute DLL path with folder mods. In case of backends, an empty string.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual string DLL {
        get {
            return _DLL;
        }
        set {
            if (_DLL != null) {
                throw new InvalidOperationException("The ETGModuleMetadata DLL path is read-only!");
            }
            _DLL = value;
        }
    }

    private bool _Prelinked = true;
    /// <summary>
    /// Whether the mod has been prelinked or not. In case of backends, always true.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual bool Prelinked {
        get {
            return _Prelinked;
        }
        set {
            throw new InvalidOperationException("The ETGModuleMetadata Prelinked flag is read-only!");
        }
    }

    private ETGMod.Profile _Profile;
    /// <summary>
    /// The base profile used to compile this mod.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual ETGMod.Profile Profile {
        get {
            return _Profile;
        }
        set {
            if (_Profile != null) {
                throw new InvalidOperationException("The ETGModuleMetadata profile is read-only!");
            }
            _Profile = value;
        }
    }

    private List<ETGModuleMetadata> _Dependencies;
    /// <summary>
    /// The dependencies of the mod. In case of backends, this will return null.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual ICollection<ETGModuleMetadata> Dependencies {
        get {
            if (_Dependencies == null) {
                return null;
            }
            return _Dependencies.AsReadOnly();
        }
        set {
            throw new InvalidOperationException("The ETGModuleMetadata dependency list is read-only!");
        }
    }

    public override string ToString() {
        return Name + " " + Version;
    }

    internal static ETGModuleMetadata Parse(string archive, string directory, Stream stream) {
        ETGModuleMetadata metadata = new ETGModuleMetadata();
        metadata._Archive = archive;
        metadata._Directory = directory;
        metadata._Prelinked = false;
        metadata._Profile = ETGMod.BaseProfile; // Works as if it were set to Release
        metadata._Dependencies = new List<ETGModuleMetadata>();

        using (StreamReader reader = new StreamReader(stream)) {
            int lineN = -1;
            while (!reader.EndOfStream) {
                ++lineN;
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                line = line.Trim();
                if (line[0] == '#') {
                    continue;
                }
                if (!line.Contains(":")) {
                    Debug.LogWarning("INVALID METADATA LINE #" + lineN);
                    continue;
                }
                string[] data = line.Split(':');
                if (data.Length < 2) {
                    Debug.LogWarning("INVALID METADATA LINE #" + lineN);
                    continue;
                }
                if (2 < data.Length) {
                    StringBuilder newData = new StringBuilder();
                    for (int i = 1; i < data.Length; i++) {
                        newData.Append(data[i]);
                        if (i < data.Length - 1) {
                            newData.Append(':');
                        }
                    }
                    data = new string[] { data[0], newData.ToString() };
                }
                string prop = data[0].Trim();
                data[1] = data[1].Trim();

                if (prop == "Name") {
                    metadata._Name = data[1];

                } else if (prop == "Version") {
                    metadata._Version = new Version(data[1]);

                } else if (prop == "DLL") {
                    metadata._DLL = data[1].Replace("\\", "/");

                } else if (prop == "Prelinked") {
                    metadata._Prelinked = data[1].ToLowerInvariant() == "true";

                } else if (prop == "Profile") {
                    int pid;
                    string pname = "";
                    if (!int.TryParse(data[1], out pid)) {
                        pname = data[1].ToLowerInvariant();
                        if (pname != ETGMod.BaseProfile.Name) {
                            pid = int.MaxValue;
                        } else {
                            pid = ETGMod.BaseProfile.Id;
                        }
                    }
                    metadata._Profile = new ETGMod.Profile(pid, pname);

                } else if (prop == "Depends" || prop == "Dependency") {
                    ETGModuleMetadata dep = new ETGModuleMetadata();
                    dep._Name = data[1];
                    dep._Version = new Version(0, 0);
                    if (data[1].Contains(" ")) {
                        string[] depData = data[1].Split(' ');
                        dep._Name = depData[0].Trim();
                        dep._Version = new Version(depData[1].Trim());
                    }
                    metadata._Dependencies.Add(dep);

                }
            }
        }

        // Set the DLL path to be absolute in folder mods if not already absolute
        if (!string.IsNullOrEmpty(directory) && !File.Exists(metadata._DLL)) {
            metadata._DLL = Path.Combine(directory, metadata._DLL.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
        }

        // Add dependency to Base 1.0 if missing.
        bool dependsOnBase = false;
        foreach (ETGModuleMetadata dependency in metadata.Dependencies) {
            if (dependency.Name == "Base") {
                dependsOnBase = true;
                break;
            }
        }
        if (!dependsOnBase) {
            Debug.Log("WARNING: No dependency to Base found in " + metadata + "! Adding dependency to Base 1.0...");
            metadata._Dependencies.Insert(0, new ETGModuleMetadata() {
                _Name = "Base",
                _Version = new Version(1, 0)
            });
        }

        return metadata;
    }

}
