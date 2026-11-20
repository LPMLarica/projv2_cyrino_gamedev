using System.IO;
using System.Threading;
using Nexus.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Nexus.EditorTools
{
    public static class ArtDownloader
    {
        const string Dir = "Assets/NexusCards/Resources/ChampionArt";

        [MenuItem("NEXUS/2 - Baixar artes dos campeões (Data Dragon)", false, 20)]
        public static void DownloadMenu()
        {
            Download(true);
        }

        public static void Download(bool interactive)
        {
            Directory.CreateDirectory(Dir);
            int ok = 0, failed = 0, skipped = 0;
            var list = CardDatabase.Champions;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var def = list[i];
                    string path = Dir + "/" + def.Id + ".jpg";
                    if (File.Exists(path)) { skipped++; continue; }
                    if (EditorUtility.DisplayCancelableProgressBar("NEXUS — artes dos campeões", def.Name, (float)i / list.Count)) break;
                    using (var req = UnityWebRequest.Get(string.Format(ArtLoader.LoadingArtUrl, def.Id)))
                    {
                        req.timeout = 15;
                        var op = req.SendWebRequest();
                        while (!op.isDone) Thread.Sleep(15);
                        if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.data != null && req.downloadHandler.data.Length > 1000)
                        {
                            File.WriteAllBytes(path, req.downloadHandler.data);
                            ok++;
                        }
                        else
                        {
                            failed++;
                            if (failed >= 3 && ok == 0) break;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.Refresh();
            foreach (var file in Directory.GetFiles(Dir, "*.jpg"))
            {
                var ti = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (ti == null || ti.maxTextureSize == 512) continue;
                ti.textureType = TextureImporterType.Default;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 512;
                ti.SaveAndReimport();
            }
            string msg = "Baixadas: " + ok + "   Já existiam: " + skipped + "   Falharam: " + failed;
            Debug.Log("[NEXUS] Artes dos campeões — " + msg);
            if (interactive)
                EditorUtility.DisplayDialog("NEXUS", msg + (failed > 0 ? "\n\nSem internet? O jogo usa artes de reserva e tenta baixar de novo durante a partida." : ""), "OK");
        }
    }
}
