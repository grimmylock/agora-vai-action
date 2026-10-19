// Batch 29: drop-box for updates from the cloud workspace. Any .zip placed in <project>/Incoming/ is
// unpacked over the project root (paths inside the zip are project-relative, e.g. "Assets/Scripts/...")
// as soon as the editor is idle (not playing, not compiling), then deleted, and the AssetDatabase is
// refreshed. Lets a whole batch of scripts + sprites arrive as one file.
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class IncomingSync
{
    static double _next;
    static IncomingSync() { EditorApplication.update += Poll; }

    static string Root => Path.GetDirectoryName(Application.dataPath);
    static string Inbox => Path.Combine(Root, "Incoming");

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 2.0;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!Directory.Exists(Inbox)) return;
        var zips = Directory.GetFiles(Inbox, "*.zip");
        if (zips.Length == 0) return;
        System.Array.Sort(zips);
        int files = 0;
        foreach (var zip in zips)
        {
            try
            {
                using (var fs = File.OpenRead(zip))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (var e in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(e.Name)) continue;                         // folder entry
                        var rel = e.FullName.Replace('\\', '/');
                        if (rel.Contains("..")) continue;                                    // stay inside the project
                        var dest = Path.Combine(Root, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        if (File.Exists(dest)) File.SetAttributes(dest, FileAttributes.Normal);
                        using (var src = e.Open())
                        using (var dst = File.Create(dest)) src.CopyTo(dst);
                        files++;
                    }
                }
                File.Delete(zip);
                Debug.Log($"[IncomingSync] unpacked {Path.GetFileName(zip)}");
            }
            catch (IOException ex) { Debug.LogWarning($"[IncomingSync] {Path.GetFileName(zip)}: {ex.Message} (will retry)"); return; }
        }
        if (files > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
