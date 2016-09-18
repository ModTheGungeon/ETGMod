using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

public abstract class ModRepo {

    public abstract IEnumerator GetRemoteMods();

}

public sealed class LastBulletModRepo : ModRepo {

    public readonly static string URL = "http://lastbullet.net/mydownloads.php?action=browse_cat&cid=286";

    private Thread _CurrentThread;
    private bool _Alive = false;
    private RemoteMod _Current;

    public override IEnumerator GetRemoteMods() {
        if (_CurrentThread != null) {
            _Alive = false;
            _CurrentThread.Join();
        }

        _CurrentThread = new Thread(_AsyncGetRemoteMods) {
            Name = "Last Bullet mod repo web async thread",
            IsBackground = true
        };
        _Current = null;
        _Alive = true;
        _CurrentThread.Start();

        while (_Current != null || (_Alive && _CurrentThread.IsAlive)) {
            yield return null;

            if (_Current != null) {
                yield return _Current;
                _Current = null;
            }
        }

    }

    private const string DOWNLOAD_START = "<!-- start: mydownloads_downloads_download -->";
    private const string DOWNLOAD_END = "<!-- end: mydownloads_downloads_download -->";
    private void _AsyncGetRemoteMods() {
        try {
            string html;
            using (WebClient wc = new WebClient()) {
                Console.WriteLine("Downloading " + URL);
                html = wc.DownloadString(URL);
            }

            int i = -1;
            while ((i = html.IndexOfInvariant(DOWNLOAD_START, i + 1)) > 0 && _Alive) {
                string download = html.Substring(i + DOWNLOAD_START.Length, html.IndexOfInvariant(DOWNLOAD_END, i) - i - DOWNLOAD_START.Length).Trim();
                string[] lines = download.Split('\n');

                RemoteMod mod = new RemoteMod();

                mod.Name = lines[5].Substring(lines[5].IndexOfInvariant("title=\"") + 7);
                mod.Name = mod.Name.Substring(0, mod.Name.IndexOf('"'));
                mod.Name = _UnescapedAttribute(mod.Name);

                mod.Author = lines[6].Substring(lines[6].LastIndexOfInvariant("\">", lines[6].IndexOfInvariant("</a>")) + 2);
                mod.Author = mod.Author.Substring(0, mod.Author.IndexOfInvariant("</a>"));

                mod.URL = lines[2].Substring(lines[2].IndexOfInvariant("<a href=\"") + 9);
                mod.URL = mod.URL.Substring(0, mod.URL.IndexOf('"'));
                mod.URL = _UnescapedAttribute(mod.URL);

                Console.WriteLine($"Found mod: \"{mod.Name}\" by {mod.Author}");
                _Current = mod;
                while (_Current != null) {
                    Thread.Sleep(16);
                }
            }
        } catch (Exception e) {
            _Current = new RemoteMod {
                Name = e.ToString()
            };
            Console.WriteLine(e);
        }

        _Alive = false;
    }

    private static string _UnescapedAttribute(string escaped) {
        return SecurityElement.FromString($"<t a=\"{escaped}\" />").Attribute("a");
    }

}
