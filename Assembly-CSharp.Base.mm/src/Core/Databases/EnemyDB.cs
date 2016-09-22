using System;
using System.Collections.Generic;
public sealed class EnemyDB {
    internal EnemyDB () {}

    private Dictionary<string, AIActor> _ModEnemies = new Dictionary<string, AIActor>();

    public AIActor GetModEnemyByGuid(string guid) {
        if (_ModEnemies.ContainsKey(guid)) {
            return _ModEnemies[guid];
        }
        return null;
    }

    public AIActor CopyEnemyByGuid(string guid) {
        AIActor orig = EnemyDatabase.GetOrLoadByGuid(guid);

        // This is a workaround for Unity's shitty prefab quirks
        // While the object is a prefab, it's active status does not affect it (it isn't run)
        // But the moment you instantiate it, the clone is no longer a prefab and it inherits
        // the object's active status, which is normally (always?) true
        // That means the instantiated object will call Awake, Start and Update probably breaking some things
        // As a workaround, we disable the origin's active status, create an instance, then set it back to true.
        orig.gameObject.SetActive(false);
        AIActor thing = UnityEngine.Object.Instantiate(orig);
        orig.gameObject.SetActive(true);
        UnityEngine.Object.DontDestroyOnLoad(thing);
        return thing;
    }

    public string AddEnemy(AIActor actor, string guid) {
        EnemyDatabaseEntry entry = new EnemyDatabaseEntry(actor);
        _ModEnemies[guid] = actor;
        entry.myGuid = guid;
        entry.path = "Assets/Resources/ENEMYDB_GUID:" + guid + ".prefab";
        EnemyDatabase.Instance.Objects.Add(actor);
        EnemyDatabase.Instance.Entries.Add(entry);
        return guid;
    }

    public string AddEnemy(AIActor actor) {
        return AddEnemy(actor, Guid.NewGuid().ToString());
    }
}