using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONTextureRule : JSONRule<Texture> {

    private static Dictionary<Texture2D, string> _DumpTex2DPathMap = new Dictionary<Texture2D, string>();
    private static Dictionary<string, int> _DumpNameIdMap = new Dictionary<string, int>();

    public static List<string> SharedDumpBlacklist = new List<string>() {
        "atlas0"
    };

    public override void Serialize(JsonHelperWriter json, object obj) {
        string name = ((Texture) obj).name;
        json.WriteProperty("name", name);
        json.WriteProperty("type", obj.GetType().Name.Substring(7));

        if (!(obj is Texture2D)) {
            return;
        }
        Texture2D t2D = (Texture2D) obj;
        json.WriteProperty("format", t2D.format);
        json.WriteProperty("mipmaps", 1 < t2D.mipmapCount);
        json.WriteProperty("readwrite", t2D.IsReadable());

        string dumproot = null;
        string path = name;
        bool sharedDumpBlacklisted = SharedDumpBlacklist.Contains(name);
        if (JSONHelper.SharedDir != null && !sharedDumpBlacklisted) {
            dumproot = Path.Combine(JSONHelper.SharedDir, "Texture2Ds");

            int id;
            if (!_DumpNameIdMap.TryGetValue(name, out id)) {
                id = -1;
            }
            _DumpNameIdMap[name] = ++id;

            if (id != 0) {
                path += "." + id;
            }
            _DumpTex2DPathMap[t2D] = path;

            json.WriteProperty(JSONHelper.META.EXTERNAL_IN, JSONHelper.META.EXTERNAL_IN_SHARED);
        } else if (json.DumpRelatively || sharedDumpBlacklisted) {
            dumproot = json.RelativeDir;
            json.WriteProperty(JSONHelper.META.EXTERNAL_IN, JSONHelper.META.EXTERNAL_IN_RELATIVE);
        }
        if (dumproot == null) {
            json.WriteProperty(JSONHelper.META.EXTERNAL_IN, "");
            json.WriteProperty(JSONHelper.META.EXTERNAL_PATH, "");
            return;
        }

        string dumppath = Path.Combine(dumproot, path.Replace('/', Path.DirectorySeparatorChar) + ".png");
        Directory.GetParent(dumppath).Create();
        if (File.Exists(dumppath)) {
            json.WriteProperty(JSONHelper.META.EXTERNAL_PATH, dumppath);
            return;
        }
        File.WriteAllBytes(dumppath, t2D.GetRW().EncodeToPNG());
        json.WriteProperty(JSONHelper.META.EXTERNAL_PATH, path);
    }

    public override object New(JsonHelperReader json, Type type) {
        return null;
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        Texture t = null;

        string name = (string) json.ReadRawProperty("name");
        string type = (string) json.ReadRawProperty("type");
        bool mipmaps, readwrite;

        if (type == "2D") {
            TextureFormat format = json.ReadProperty<TextureFormat>("format");
            mipmaps = (bool) json.ReadRawProperty("mipmaps");
            readwrite = (bool) json.ReadRawProperty("readwrite");

            obj = t = new Texture2D(2, 2, format, mipmaps);
        } else {
            throw new JsonReaderException("Texture types other than Texture2D not supported!");
        }

        t.name = name;

        if (t is Texture2D) {
            Texture2D t2D = (Texture2D) t;

            string dumppath = null;
            string @in = (string) json.ReadRawProperty(JSONHelper.META.EXTERNAL_IN);
            string path = (string) json.ReadRawProperty(JSONHelper.META.EXTERNAL_PATH);
            if (@in == JSONHelper.META.EXTERNAL_IN_SHARED && JSONHelper.SharedDir != null) {
                dumppath = Path.Combine(JSONHelper.SharedDir, "Texture2Ds");
            } else if (@in == JSONHelper.META.EXTERNAL_IN_RELATIVE && json.RelativeDir != null) {
                dumppath = json.RelativeDir;
            }
            if (dumppath == null) {
                t2D.Apply(mipmaps, !readwrite);
                return t2D;
            }

            dumppath = Path.Combine(dumppath, path);
            if (!File.Exists(dumppath)) {
                return t2D;
            }
            t2D.LoadImage(File.ReadAllBytes(dumppath), false);
            t2D.Apply(mipmaps, !readwrite);
            return t2D;
        }

        return t;
    }

}
