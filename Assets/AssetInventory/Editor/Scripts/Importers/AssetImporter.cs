using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;

namespace AssetInventory
{
    public abstract class AssertImporter
    {
        public static string CurrentMain { get; protected set; }
        public static int MainCount { get; protected set; }
        public static int MainProgress { get; protected set; }
        public static string CurrentSub { get; protected set; }
        public static int SubCount { get; protected set; }
        public static int SubProgress { get; protected set; }
        public static bool CancellationRequested { get; set; }

        protected void ResetState()
        {
            CurrentMain = null;
            CurrentSub = null;
            MainCount = 0;
            MainProgress = 0;
            SubCount = 0;
            SubProgress = 0;
        }

        protected Asset Fetch(Asset asset)
        {
            // return non-deprecated version first (deprecated < published in sorting)
            return DBAdapter.DB.Table<Asset>().Where(a => a.SafeName == asset.SafeName).OrderBy(a => a.OfficialState).LastOrDefault();
        }

        protected AssetFile Fetch(AssetFile asset)
        {
            return DBAdapter.DB.Find<AssetFile>(a => a.Path == asset.Path);
        }

        protected void Persist(Asset asset)
        {
            if (asset.Id > 0)
            {
                DBAdapter.DB.Update(asset);
                return;
            }

            Asset existing = DBAdapter.DB.Find<Asset>(a => a.SafeName == asset.SafeName);
            if (existing != null)
            {
                asset.Id = existing.Id;
                existing.SafeCategory = asset.SafeCategory;
                existing.SafePublisher = asset.SafePublisher;
                existing.CurrentState = asset.CurrentState;
                existing.AssetSource = asset.AssetSource;
                existing.PackageSize = asset.PackageSize;
                existing.Location = asset.Location;
                existing.PreviewImage = asset.PreviewImage;

                DBAdapter.DB.Update(existing);
            }
            else
            {
                DBAdapter.DB.Insert(asset);
            }
        }

        protected void Persist(AssetFile file)
        {
            if (file.Id > 0)
            {
                DBAdapter.DB.Update(file);
                return;
            }

            AssetFile existing;
            if (string.IsNullOrEmpty(file.Guid))
            {
                existing = DBAdapter.DB.Find<AssetFile>(f => f.Path == file.Path && f.AssetId == file.AssetId);
            }
            else
            {
                existing = DBAdapter.DB.Find<AssetFile>(f => f.Guid == file.Guid && f.AssetId == file.AssetId);
            }
            if (existing != null)
            {
                file.Id = existing.Id;
                DBAdapter.DB.Update(file);
            }
            else
            {
                DBAdapter.DB.Insert(file);
            }
        }

        protected async Task ProcessMediaAttributes(string file, AssetFile af, Asset asset)
        {
            // special processing for supported file types
            if (af.Type == "png" || af.Type == "jpg")
            {
                Texture2D tmpTexture = new Texture2D(1, 1);
                byte[] assetContent = File.ReadAllBytes(file);
                if (tmpTexture.LoadImage(assetContent))
                {
                    af.Width = tmpTexture.width;
                    af.Height = tmpTexture.height;
                }
            }

            if (AssetInventory.IsFileType(af.FileName, "Audio"))
            {
                string contentFile = asset.AssetSource != Asset.Source.Directory ? await AssetInventory.EnsureMaterializedAsset(asset, af) : file;
                try
                {
                    AudioClip clip = await AssetUtils.LoadAudioFromFile(contentFile);
                    if (clip != null) af.Length = clip.length;
                }
                catch
                {
                    Debug.LogWarning($"Audio file '{Path.GetFileName(file)}' from {af} seems to have incorrect format.");
                }
            }
        }
    }
}