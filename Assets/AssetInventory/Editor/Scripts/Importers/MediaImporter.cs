using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public class MediaImporter : AssertImporter
    {
        private const int BreakInterval = 30;

        public async Task Index(FolderSpec spec)
        {
            ResetState();

            List<string> searchPatterns = new List<string>();
            List<string> types = new List<string>();
            switch (spec.scanFor)
            {
                case 0:
                    types.AddRange(new[] {"Audio", "Images", "Models"});
                    break;

                case 2:
                    types.Add("Audio");
                    break;

                case 3:
                    types.Add("Images");
                    break;

                case 4:
                    types.Add("Models");
                    break;

                case 6:
                    if (!string.IsNullOrWhiteSpace(spec.pattern)) searchPatterns.AddRange(spec.pattern.Split(';'));
                    break;
            }
            types.ForEach(t => searchPatterns.AddRange(AssetInventory.TypeGroups[t].Select(ext => $"*.{ext}")));

            string[] files = IOUtils.GetFiles(spec.location, searchPatterns, SearchOption.AllDirectories).ToArray();
            SubCount = files.Length;
            MainProgress = 1; // small hack to trigger UI update in the end
            if (spec.createPreviews) PreviewGenerator.Init(files.Length);

            int progressId = MetaProgress.Start("Updating media files index");

            Asset noAsset = DBAdapter.DB.Find<Asset>(a => a.SafeName == Asset.None);
            if (noAsset == null)
            {
                noAsset = Asset.GetNoAsset();
                Persist(noAsset);
            }
            string previewPath = AssetInventory.GetPreviewFolder();

            for (int i = 0; i < files.Length; i++)
            {
                if (CancellationRequested) break;
                if (i % BreakInterval == 0) await Task.Yield(); // let editor breath in case many files are already indexed

                string file = files[i];
                MetaProgress.Report(progressId, i + 1, files.Length, file);
                CurrentSub = file;
                SubProgress = i + 1;

                AssetFile af = new AssetFile();
                af.AssetId = noAsset.Id;
                af.Path = file;

                AssetFile existing = Fetch(af);
                if (existing != null) continue;

                long size = new FileInfo(file).Length;
                CurrentSub = file + " (" + EditorUtility.FormatBytes(size) + ")";
                await Task.Yield(); // let editor breath

                af.FileName = Path.GetFileName(af.Path);
                af.Size = size;
                af.Type = Path.GetExtension(file).Replace(".", string.Empty).ToLower();
                if (AssetInventory.Config.gatherExtendedMetadata)
                {
                    await ProcessMediaAttributes(file, af, noAsset); // must be run on main thread
                }
                Persist(af);

                if (spec.createPreviews)
                {
                    // let Unity generate a preview
                    string previewFile = Path.Combine(previewPath, "af-" + af.Id + ".png");

                    PreviewGenerator.RegisterPreviewRequest(af.Id, af.Path, previewFile, req =>
                    {
                        if (!File.Exists(req.DestinationFile)) return;
                        AssetFile paf = DBAdapter.DB.Find<AssetFile>(req.ID);
                        if (paf == null) return;

                        if (req.Obj is Texture2D tex)
                        {
                            paf.Width = tex.width;
                            paf.Height = tex.height;
                        }
                        if (req.Obj is AudioClip clip)
                        {
                            paf.Length = clip.length;
                        }

                        paf.PreviewFile = Path.GetFileName(previewFile);
                        Persist(paf);
                    });

                    // from time to time store the previews in case something goes wrong
                    if (PreviewGenerator.ActiveRequestCount() > 100)
                    {
                        CurrentSub = "Generating preview images...";
                        await PreviewGenerator.ExportPreviews(10);
                    }
                }
            }
            if (spec.createPreviews)
            {
                CurrentSub = "Finalizing preview images...";
                await PreviewGenerator.ExportPreviews();
                PreviewGenerator.Clear();
            }
            MetaProgress.Remove(progressId);
            ResetState();
        }
    }
}