using System;
public class patch_SaveManager {
    private static string secret;
    public extern static bool orig_Load<T> (SaveManager.SaveType saveType, out T obj, bool allowDecrypted, uint expectedVersion = 0u, Func<string, uint, string> versionUpdater = null, SaveManager.SaveSlot? overrideSaveSlot = null);
    public static bool Load<T> (SaveManager.SaveType saveType, out T obj, bool allowDecrypted, uint expectedVersion = 0u, Func<string, uint, string> versionUpdater = null, SaveManager.SaveSlot? overrideSaveSlot = null) {
        Console.WriteLine("boop boop I'm a dump");
        Console.WriteLine("here is your expected version " + expectedVersion);
        Console.WriteLine("DODGEROLL EXPOSED");
        foreach (char c in secret) {
            Console.Write(c + ", ");
        }
        Console.WriteLine();
        Console.WriteLine("thanks");
        return orig_Load(saveType, out obj, allowDecrypted, expectedVersion, versionUpdater, overrideSaveSlot);
    }

}
