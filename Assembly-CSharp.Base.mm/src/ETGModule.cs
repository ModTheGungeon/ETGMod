using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Game mod class. All game mods should have a class / type extending this.
/// </summary>
public abstract class ETGModule {

    private string _archivePath;
    /// <summary>
    /// Used by ETGMod itself and other mods to find the archive of the mod.
    /// The archive must contain a metadata file and can contain additional resources.
    /// 
    /// ETGBackends will have an empty path.
    /// 
    /// This property can only be set by ETGMod itself.
    /// </summary>
    public string ArchivePath {
        get {
            return _archivePath;
        }
        set {
            if (_archivePath != null) {
                throw new InvalidOperationException("The ETGModule archive path is read-only!");
            }
            _archivePath = value;
        }
    }

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

    public ETGModule() {
    }

    /// <summary>
    /// This method gets called when ETGMod "starts" (inits), after all mods have been loaded.
    /// Do not depend on any specific order in which the mods get started.
    /// </summary>
    public virtual void Start() { }

    /// <summary>
    /// This method gets called each update.
    /// Do not depend on any specific order in which the mods get poked.
    /// </summary>
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

    private string _name;
    /// <summary>
    /// The name of the mod. In case of backends, the name of the API (f.e. ExampleAPI) without spaces.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual string Name {
        get {
            return _name;
        }
        set {
            if (_name != null) {
                throw new InvalidOperationException("The ETGModuleMetadata name is read-only!");
            }
            _name = value;
        }
    }

    private Version _version;
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
            return _version;
        }
        set {
            if (_version != null) {
                throw new InvalidOperationException("The ETGModuleMetadata version is read-only!");
            }
            _version = value;
        }
    }

    private List<ETGModuleMetadata> _dependencies;
    /// <summary>
    /// The dependencies of the mod. In case of backends, this will return null.
    /// 
    /// Can only be set by ETGMod itself by default, unless you're having your own ETGModuleMetadata - extending type.
    /// </summary>
    public virtual ICollection<ETGModuleMetadata> Dependencies {
        get {
            if (_dependencies == null) {
                return null;
            }
            return _dependencies.AsReadOnly();
        }
        set {
            throw new InvalidOperationException("The ETGModuleMetadata dependency list is read-only!");
        }
    }

    public static ETGModuleMetadata Parse(Stream stream) {
        ETGModuleMetadata metadata = new ETGModuleMetadata();
        metadata._dependencies = new List<ETGModuleMetadata>();

        using (StreamReader reader = new StreamReader(stream)) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                line = line.Trim();
                if (line[0] == '#') {
                    continue;
                }
                string[] data = line.Split(':');
                string prop = data[0].Trim();
                data[1] = data[1].Trim();

                if (prop == "Name") {
                    metadata._name = data[1];

                } else if (prop == "Version") {
                    metadata._version = new Version(data[1]);

                } else if (prop == "Depends") {
                    ETGModuleMetadata dep = new ETGModuleMetadata();
                    dep._name = data[1];
                    dep._version = new Version(0, 0);
                    if (data[1].Contains(" ")) {
                        string[] depData = data[1].Split(' ');
                        dep._name = depData[0].Trim();
                        dep._version = new Version(depData[1].Trim());
                    }
                    metadata._dependencies.Add(dep);

                }
            }
        }
        return metadata;
    }

}
