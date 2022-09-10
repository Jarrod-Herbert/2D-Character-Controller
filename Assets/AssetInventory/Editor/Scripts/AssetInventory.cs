﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JD.EditorAudioUtils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public delegate void TagsChangedData();

    public delegate void ActionDone();

    public static class AssetInventory
    {
        public const string ToolVersion = "1.3.0";
        public static readonly string[] ScanDependencies = {"prefab", "mat", "controller", "anim", "asset", "physicmaterial", "physicsmaterial", "sbs", "sbsar", "cubemap", "shadergraph", "shadersubgraph"};

        public static string CurrentMain { get; set; }
        public static int MainCount { get; set; }
        public static int MainProgress { get; set; }
        public static string UsedConfigLocation { get; private set; }

        public static event TagsChangedData OnTagsChanged;
        public static event ActionDone OnIndexingDone;

        private const int BreakInterval = 5;
        private const int MaxDropdownItems = 25;
        private const string ConfigName = "AssetInventoryConfig.json";
        private const string AssetStoreFolderName = "Asset Store-5.x";
        private static readonly Regex FileGuid = new Regex("guid: (?:([a-z0-9]*))");

        private static List<TagInfo> Tags
        {
            get
            {
                if (_tags == null) LoadTagAssignments();
                return _tags;
            }
        }

        private static List<TagInfo> _tags;

        public static AssetInventorySettings Config
        {
            get
            {
                if (_config == null) LoadConfig();
                return _config;
            }
        }

        private static AssetInventorySettings _config;

        public static bool IndexingInProgress { get; private set; }
        public static bool ClearCacheInProgress { get; private set; }

        public static Dictionary<string, string[]> TypeGroups { get; } = new Dictionary<string, string[]>
        {
            {"Audio", new[] {"wav", "mp3", "ogg", "aiff", "aif", "mod", "it", "s3m", "xm"}},
            {"Images", new[] {"png", "jpg", "jpeg", "bmp", "tga", "tif", "tiff", "psd", "svg", "webp", "ico", "exr", "gif", "hdr", "iff", "pict"}},
            {"Video", new[] {"mp4"}},
            {"Prefabs", new[] {"prefab"}},
            {"Materials", new[] {"mat", "physicmaterial", "physicsmaterial", "sbs", "sbsar", "cubemap"}},
            {"Shaders", new[] {"shader", "shadergraph", "shadersubgraph", "compute"}},
            {"Models", new[] {"fbx", "obj", "blend", "dae", "3ds", "dxf", "max", "c4d", "mb", "ma"}},
            {"Scripts", new[] {"cs", "php"}},
            {"Libraries", new[] {"zip", "unitypackage", "so", "bundle", "dll", "jar"}},
            {"Documents", new[] {"md", "doc", "docx", "txt", "json", "rtf", "pdf", "html", "readme", "xml", "chm"}}
        };

        public static int AssetTagHash { get; private set; }

        public static bool IsFileType(string path, string type)
        {
            if (path == null) return false;
            return TypeGroups[type].Contains(Path.GetExtension(path).ToLower().Replace(".", string.Empty));
        }

        public static string GetStorageFolder()
        {
            if (!string.IsNullOrEmpty(Config.customStorageLocation)) return Path.GetFullPath(Config.customStorageLocation);

            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AssetInventory");
        }

        private static string GetConfigLocation()
        {
            // search for local project-specific override first
            string guid = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(ConfigName)).FirstOrDefault();
            if (guid != null) return AssetDatabase.GUIDToAssetPath(guid);

            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConfigName);
        }

        public static string GetPreviewFolder(string customFolder = null)
        {
            string previewPath = IOUtils.PathCombine(customFolder ?? GetStorageFolder(), "Previews");
            if (!Directory.Exists(previewPath)) Directory.CreateDirectory(previewPath);
            return previewPath;
        }

        private static string GetMaterializePath()
        {
            return IOUtils.PathCombine(GetStorageFolder(), "Extracted");
        }

        private static string GetMaterializedAssetPath(Asset asset)
        {
            return IOUtils.PathCombine(GetMaterializePath(), asset.SafeName);
        }

        public static async Task<string> ExtractAsset(Asset asset)
        {
            string tempPath = GetMaterializedAssetPath(asset);
            int retries = 0;
            while (retries < 5 && Directory.Exists(tempPath))
            {
                try
                {
                    await Task.Run(() => Directory.Delete(tempPath, true));
                    break;
                }
                catch (Exception)
                {
                    retries++;
                    await Task.Delay(500);
                }
            }
            if (Directory.Exists(tempPath)) Debug.LogWarning("Could not remove temporary directory: " + tempPath);

            await Task.Run(() => TarUtil.ExtractGz(asset.Location, tempPath));

            return Directory.Exists(tempPath) ? tempPath : null;
        }

        public static bool IsMaterialized(Asset asset, AssetFile assetFile)
        {
            if (asset.AssetSource == Asset.Source.Directory) return true;

            string sourcePath = Path.Combine(GetMaterializedAssetPath(asset), assetFile.SourcePath);
            return File.Exists(sourcePath);
        }

        public static async Task<string> EnsureMaterializedAsset(AssetInfo info)
        {
            string targetPath = await EnsureMaterializedAsset(info.ToAsset(), info);
            info.IsMaterialized = IsMaterialized(info.ToAsset(), info);
            return targetPath;
        }

        public static async Task<string> EnsureMaterializedAsset(Asset asset, AssetFile assetFile)
        {
            if (asset.AssetSource == Asset.Source.Directory) return assetFile.Path;

            string sourcePath = Path.Combine(GetMaterializedAssetPath(asset), assetFile.SourcePath);
            if (!File.Exists(sourcePath)) await ExtractAsset(asset);
            if (!File.Exists(sourcePath)) return null;

            string targetPath = Path.Combine(Path.GetDirectoryName(sourcePath), "Content", Path.GetFileName(assetFile.Path));
            if (!File.Exists(targetPath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(targetPath))) Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(sourcePath, targetPath);
            }
            string sourceMetaPath = sourcePath + ".meta";
            string targetMetaPath = targetPath + ".meta";
            if (File.Exists(sourceMetaPath) && !File.Exists(targetMetaPath)) File.Copy(sourceMetaPath, targetMetaPath);

            return targetPath;
        }

        public static async Task CalculateDependencies(AssetInfo assetInfo)
        {
            string targetPath = await EnsureMaterializedAsset(assetInfo.ToAsset(), assetInfo);
            if (targetPath == null) return;

            assetInfo.Dependencies = await Task.Run(() => DoCalculateDependencies(assetInfo, targetPath));
            assetInfo.DependencySize = assetInfo.Dependencies.Sum(af => af.Size);
            assetInfo.MediaDependencies = assetInfo.Dependencies.Where(af => af.Type != "cs").ToList();
            assetInfo.ScriptDependencies = assetInfo.Dependencies.Where(af => af.Type == "cs").ToList();
        }

        private static async Task<List<AssetFile>> DoCalculateDependencies(AssetInfo assetInfo, string path)
        {
            List<AssetFile> result = new List<AssetFile>();

            // only scan file types that contain guid references
            if (!ScanDependencies.Contains(Path.GetExtension(path).Replace(".", string.Empty))) return result;

            string content = File.ReadAllText(path);
            // TODO: handle binary serialized files, e.g. prefabs
            if (!content.StartsWith("%YAML"))
            {
                assetInfo.DependencyState = AssetInfo.DependencyStateOptions.NotPossible;
                return result;
            }
            MatchCollection matches = FileGuid.Matches(content);

            foreach (Match match in matches)
            {
                string guid = match.Groups[1].Value;
                if (result.Any(r => r.Guid == guid)) continue; // break recursion

                AssetFile af = DBAdapter.DB.Find<AssetFile>(a => a.Guid == guid);
                if (af == null) continue; // ignore missing guids as they are not in the package so we can't do anything about them
                result.Add(af);

                // recursive
                string targetPath = await EnsureMaterializedAsset(assetInfo.ToAsset(), af);
                if (targetPath == null)
                {
                    Debug.LogWarning($"Could not materialize dependency: {af.Path}");
                    continue;
                }

                result.AddRange(await Task.Run(() => DoCalculateDependencies(assetInfo, targetPath)));
            }

            return result;
        }

        public static List<AssetInfo> LoadAssets()
        {
            string indexedQuery = "select *, Count(*) as FileCount, Sum(af.Size) as UncompressedSize from AssetFile af left join Asset on Asset.Id = af.AssetId group by af.AssetId order by Lower(Asset.SafeName)";
            List<AssetInfo> indexedResult = DBAdapter.DB.Query<AssetInfo>(indexedQuery);

            string allQuery = "select *, Id as AssetId from Asset order by Lower(SafeName)";
            List<AssetInfo> allResult = DBAdapter.DB.Query<AssetInfo>(allQuery);

            // sqlite does not support "right join", therefore merge two queries manually 
            List<AssetInfo> result = allResult;
            result.ForEach(asset =>
            {
                AssetInfo match = indexedResult.FirstOrDefault(indexedAsset => indexedAsset.Id == asset.Id);
                if (match == null) return;
                asset.FileCount = match.FileCount;
                asset.UncompressedSize = match.UncompressedSize;
            });

            return result;
        }

        public static string[] ExtractAssetNames(IEnumerable<AssetInfo> assets)
        {
            bool intoSubmenu = Config.groupLists && assets.Count(a => a.FileCount > 0) > MaxDropdownItems;
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(assets
                .Where(a => !a.Exclude)
                .Where(a => a.FileCount > 0)
                .Select(a => intoSubmenu && !a.SafeName.StartsWith("-") ? a.SafeName.Substring(0, 1).ToUpperInvariant() + "/" + a.SafeName : a.SafeName)
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            // move -none- to top
            int noneIdx = result.FindIndex(a => a == Asset.None);
            if (noneIdx >= 0)
            {
                string tmp = result[noneIdx];
                result.RemoveAt(noneIdx);
                result.Insert(1, tmp);

                if (result.Count == 3) result.RemoveAt(2);
            }

            return result.ToArray();
        }

        public static string[] ExtractTagNames(List<Tag> tags)
        {
            bool intoSubmenu = Config.groupLists && tags.Count > MaxDropdownItems;
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(tags
                .Select(a => intoSubmenu && !a.Name.StartsWith("-") ? a.Name.Substring(0, 1).ToUpperInvariant() + "/" + a.Name : a.Name)
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] ExtractPublisherNames(IEnumerable<AssetInfo> assets)
        {
            bool intoSubmenu = Config.groupLists && assets.Count(a => a.FileCount > 0) > MaxDropdownItems; // approximation, publishers != assets but roughly the same
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(assets
                .Where(a => !a.Exclude)
                .Where(a => a.FileCount > 0)
                .Where(a => !string.IsNullOrEmpty(a.SafePublisher))
                .Select(a => intoSubmenu ? a.SafePublisher.Substring(0, 1).ToUpperInvariant() + "/" + a.SafePublisher : a.SafePublisher)
                .Distinct()
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] ExtractCategoryNames(IEnumerable<AssetInfo> assets)
        {
            bool intoSubmenu = Config.groupLists;
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(assets
                .Where(a => !a.Exclude)
                .Where(a => a.FileCount > 0)
                .Where(a => !string.IsNullOrEmpty(a.SafeCategory))
                .Select(a =>
                {
                    if (intoSubmenu)
                    {
                        string[] arr = a.GetDisplayCategory.Split('/');
                        return arr[0] + "/" + a.SafeCategory;
                    }
                    return a.SafeCategory;
                })
                .Distinct()
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] LoadTypes()
        {
            List<string> result = new List<string> {"-all-", string.Empty};

            string query = "select Distinct(Type) from AssetFile where Type not null and Type != \"\" order by Type";
            List<string> raw = DBAdapter.DB.QueryScalars<string>($"{query}");

            List<string> groupTypes = new List<string>();
            foreach (KeyValuePair<string, string[]> group in TypeGroups)
            {
                groupTypes.AddRange(group.Value);
                foreach (string type in group.Value)
                {
                    if (raw.Contains(type))
                    {
                        result.Add($"{group.Key}");
                        break;
                    }
                }
            }
            if (result.Last() != "") result.Add(string.Empty);

            // others
            result.AddRange(raw.Where(r => !groupTypes.Contains(r)).Select(type => $"Others/{type}"));

            // all
            result.AddRange(raw.Select(type => $"All/{type}"));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static async Task<long> GetCacheFolderSize()
        {
            return await IOUtils.GetFolderSize(GetMaterializePath());
        }

        public static async void RefreshIndex()
        {
            IndexingInProgress = true;
            AssertImporter.CancellationRequested = false;

            Init();

            // special handling for normal asset store assets since directory structure yields additional information
            if (Config.indexAssetStore)
            {
                string assetStoreDownloadCache = GetAssetDownloadPath();
                if (!Directory.Exists(assetStoreDownloadCache))
                {
                    Debug.LogWarning($"Could not find the asset download folder: {assetStoreDownloadCache}");
                    EditorUtility.DisplayDialog("Error", $"Could not find the asset download folder: {assetStoreDownloadCache}.\n\nEither nothing was downloaded yet through the Package Manager or you changed the Asset cache location. In the latter case, please add the new location as an additional folder.", "OK");
                    IndexingInProgress = false;
                    return;
                }
                await new PackageImporter().Index(assetStoreDownloadCache, true);
            }

            // scan custom folders
            for (int i = 0; i < Config.folders.Count; i++)
            {
                if (AssertImporter.CancellationRequested) break;

                FolderSpec spec = Config.folders[i];
                if (!spec.enabled) continue;
                if (!Directory.Exists(spec.location))
                {
                    Debug.LogWarning($"Specified folder to scan for assets does not exist anymore: {spec.location}");
                    continue;
                }
                switch (spec.folderType)
                {
                    case 0:
                        bool hasAssetStoreLayout = Path.GetFileName(spec.location) == AssetStoreFolderName;
                        await new PackageImporter().Index(spec.location, hasAssetStoreLayout);
                        break;

                    case 1:
                        await new MediaImporter().Index(spec);
                        break;

                    default:
                        Debug.LogError($"Unsupported folder scan type: {spec.folderType}");
                        break;
                }
            }

            IndexingInProgress = false;
            OnIndexingDone?.Invoke();
        }

        private static string GetAssetDownloadPath()
        {
#if UNITY_EDITOR_WIN
            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", AssetStoreFolderName);
#endif
#if UNITY_EDITOR_OSX
            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Unity", AssetStoreFolderName);
#endif
#if UNITY_EDITOR_LINUX
            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local/share/unity3d", AssetStoreFolderName);
#endif
        }

        public static void Init()
        {
            string folder = GetStorageFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            DBAdapter.InitDB();
            PerformUpgrades();
            LoadTagAssignments();
        }

        private static void PerformUpgrades()
        {
            // filename was introduced in version 2
            AppProperty dbVersion = DBAdapter.DB.Find<AppProperty>("Version");

            if (dbVersion == null)
            {
                // Upgrade from Initial to v2
                // add filenames to DB
                List<AssetFile> assetFiles = DBAdapter.DB.Table<AssetFile>().ToList();
                foreach (AssetFile assetFile in assetFiles)
                {
                    assetFile.FileName = Path.GetFileName(assetFile.Path);
                }
                DBAdapter.DB.UpdateAll(assetFiles);
            }
            else if (dbVersion.Value == "2")
            {
                // force refetching of asset details to get state
                DBAdapter.DB.Execute("update Asset set ETag=null");
                FetchAssetsDetails();
            }

            if (dbVersion?.Value != "3")
            {
                AppProperty newVersion = new AppProperty("Version", "3");
                DBAdapter.DB.InsertOrReplace(newVersion);
            }
        }

        public static void ClearCache(Action callback = null)
        {
            ClearCacheInProgress = true;
            Task _ = Task.Run(() =>
            {
                if (Directory.Exists(GetMaterializePath())) Directory.Delete(GetMaterializePath(), true);
                callback?.Invoke();
                ClearCacheInProgress = false;
            });
        }

        private static void LoadConfig()
        {
            string configLocation = GetConfigLocation();
            UsedConfigLocation = configLocation;

            if (configLocation == null || !File.Exists(configLocation))
            {
                _config = new AssetInventorySettings();
                return;
            }
            _config = JsonConvert.DeserializeObject<AssetInventorySettings>(File.ReadAllText(configLocation));
            if (_config == null) _config = new AssetInventorySettings();
            if (_config.folders == null) _config.folders = new List<FolderSpec>();
        }

        public static void SaveConfig()
        {
            string configFile = GetConfigLocation();
            if (configFile == null) return;

            File.WriteAllText(configFile, JsonConvert.SerializeObject(_config));
        }

        public static void ResetConfig()
        {
            DBAdapter.Close(); // in case DB path changes

            _config = new AssetInventorySettings();
            SaveConfig();
            AssetDatabase.Refresh();
        }

        public static async Task<AssetPurchases> FetchOnlineAssets()
        {
            AssetStore.CancellationRequested = false;
            AssetPurchases assets = await AssetStore.RetrievePurchases();
            if (assets == null) return null; // happens if token was invalid 

            CurrentMain = "Updating purchases";
            MainCount = assets.results.Count;
            MainProgress = 1;
            int progressId = MetaProgress.Start("Updating purchases");

            bool tagsChanged = false;
            for (int i = 0; i < MainCount; i++)
            {
                MainProgress = i + 1;
                MetaProgress.Report(progressId, i + 1, MainCount, string.Empty);
                if (i % BreakInterval == 0) await Task.Yield(); // let editor breath
                if (AssetStore.CancellationRequested) break;

                AssetPurchase purchase = assets.results[i];

                Asset asset = DBAdapter.DB.Find<Asset>(a => a.ForeignId == purchase.packageId);
                if (asset == null)
                {
                    // some assets actually differ in tiny capitalization aspects, e.g. Early Prototyping Material kit
                    asset = DBAdapter.DB.Find<Asset>(a => a.SafeName.ToLower() == purchase.CalculatedSafeName.ToLower());
                }

                // check if indeed same package or copy/new version of another, e.g. Buttons Switches and Toggles
                if (asset != null && asset.ForeignId != purchase.packageId && asset.ForeignId > 0) asset = null;

                // temporarily store guessed safe name to ensure locally indexed files are mapped correctly
                // will be overridden in detail run
                if (asset != null)
                {
                    asset.AssetSource = Asset.Source.AssetStorePackage;
                    asset.DisplayName = purchase.displayName.Trim();
                    asset.ForeignId = purchase.packageId;
                    if (string.IsNullOrEmpty(asset.SafeName)) asset.SafeName = purchase.CalculatedSafeName;
                    DBAdapter.DB.Update(asset);
                }
                else
                {
                    asset = purchase.ToAsset();
                    asset.SafeName = purchase.CalculatedSafeName;
                    DBAdapter.DB.Insert(asset);
                }

                // handle tags
                if (purchase.tagging != null)
                {
                    foreach (string tag in purchase.tagging)
                    {
                        if (AddAssetTag(asset, tag, true)) tagsChanged = true;
                    }
                }
            }

            if (tagsChanged)
            {
                LoadTags();
                LoadTagAssignments();
            }

            CurrentMain = null;
            MetaProgress.Remove(progressId);

            return assets;
        }

        public static async void FetchAssetsDetails()
        {
            List<AssetInfo> assets = LoadAssets()
                .Where(a => a.AssetSource == Asset.Source.AssetStorePackage && string.IsNullOrEmpty(a.ETag))
                .ToList();

            CurrentMain = "Updating package details";
            MainCount = assets.Count;
            MainProgress = 1;
            int progressId = MetaProgress.Start("Updating package details");

            for (int i = 0; i < MainCount; i++)
            {
                int id = assets[i].ForeignId;
                if (id <= 0) continue;

                MainProgress = i + 1;
                MetaProgress.Report(progressId, i + 1, MainCount, string.Empty);
                if (i % BreakInterval == 0) await Task.Yield(); // let editor breath
                if (AssetStore.CancellationRequested) break;

                AssetDetails details = await AssetStore.RetrieveAssetDetails(id, assets[i].ETag);
                if (details == null) continue; // happens if unchanged through etag

                // check if disabled, then download links are not available anymore, deprecated would still work
                DownloadInfo downloadDetails = null;
                if (details.state != "disabled")
                {
                    downloadDetails = await AssetStore.RetrieveAssetDownloadInfo(id);
                    if (downloadDetails == null || string.IsNullOrEmpty(downloadDetails.filename_safe_package_name))
                    {
                        Debug.Log($"Could not fetch download detail information for '{assets[i].SafeName}'");
                        continue;
                    }
                }

                // reload asset to ensure working on latest copy, otherwise might loose package size information
                Asset asset = DBAdapter.DB.Find<Asset>(a => a.ForeignId == id);
                if (asset == null)
                {
                    Debug.LogWarning($"Formerly saved package '{assets[i].DisplayName}' disappeared.");
                    continue;
                }
                asset.OfficialState = details.state;
                asset.ETag = details.ETag;
                asset.DisplayName = details.name;
                asset.DisplayPublisher = details.productPublisher.name;
                asset.DisplayCategory = details.category.name;
                if (downloadDetails != null)
                {
                    asset.SafeName = downloadDetails.filename_safe_package_name;
                    asset.SafeCategory = downloadDetails.filename_safe_category_name;
                    asset.SafePublisher = downloadDetails.filename_safe_publisher_name;
                }
                if (string.IsNullOrEmpty(asset.SafeName)) asset.SafeName = AssetUtils.GuessSafeName(details.name);
                asset.Description = details.description;
                asset.Requirements = string.Join(", ", details.requirements);
                asset.Keywords = string.Join(", ", details.keyWords);
                asset.SupportedUnityVersions = string.Join(", ", details.supportedUnityVersions);
                asset.Revision = details.revision;
                asset.Slug = details.slug;
                asset.Version = details.version.name;
                asset.LastRelease = details.version.publishedDate;
                if (details.productReview != null)
                {
                    asset.AssetRating = details.productReview.ratingAverage;
                    asset.RatingCount = int.Parse(details.productReview.ratingCount);
                }
                asset.CompatibilityInfo = details.compatibilityInfo;
                asset.MainImage = details.mainImage.url;
                asset.ReleaseNotes = details.publishNotes;
                asset.KeyFeatures = details.keyFeatures;

                DBAdapter.DB.Update(asset);
                await Task.Delay(Random.Range(500, 1500)); // don't flood server
            }

            CurrentMain = null;
            MetaProgress.Remove(progressId);
        }

        public static int CountPurchasedAssets(IEnumerable<AssetInfo> assets)
        {
            return assets.Count(a => a.AssetSource == Asset.Source.AssetStorePackage);
        }

        public static List<AssetInfo> CalculateAssetUsage()
        {
            List<AssetInfo> result = new List<AssetInfo>();
            List<string> guids = GatherGuids(new[] {Application.dataPath});
            foreach (string guid in guids)
            {
                List<AssetInfo> files = Guid2File(guid);
                if (files.Count == 0)
                {
                    // found unindexed asset
                    AssetInfo ai = new AssetInfo();
                    ai.Guid = guid;
                    ai.CurrentState = Asset.State.Unknown;
                    result.Add(ai);
                    continue;
                }
                if (files.Count > 1)
                {
                    Debug.LogWarning("Duplicate guids found: " + string.Join(", ", files.Select(ai => ai.Path)));
                    continue;
                }
                result.Add(files[0]);
            }

            return result;
        }

        public static List<AssetInfo> Guid2File(string guid)
        {
            string query = "select * from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where Guid=?";
            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>($"{query}", guid);
            return files;
        }

        private static List<string> GatherGuids(IEnumerable<string> folders)
        {
            List<string> result = new List<string>();

            foreach (string folder in folders)
            {
                // scan for all meta files and return corresponding asset
                string[] assets = Directory.GetFiles(folder, "*.meta", SearchOption.AllDirectories);
                for (int i = 0; i < assets.Length; i++)
                {
                    assets[i] = assets[i].Substring(0, assets[i].Length - 5).Replace("\\", "/");
                    assets[i] = assets[i].Substring(Application.dataPath.Length - 6); // leave "Assets/" in
                }
                foreach (string asset in assets)
                {
                    string guid = GetAssetGuid(asset);
                    if (string.IsNullOrEmpty(guid)) continue;

                    result.Add(guid);
                }
            }

            return result;
        }

        private static string GetAssetGuid(string assetFile)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetFile);
            if (!string.IsNullOrEmpty(guid)) return guid;

            // hidden files might not be indexed
            string metaFile = $"{assetFile}.meta";
            if (!File.Exists(metaFile)) return null;

            using (StreamReader reader = new StreamReader(metaFile))
            {
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    if (!line.StartsWith("guid:")) continue;
                    return line.Substring(5).Trim();
                }
            }

            return null;
        }

        public static void MoveDatabase(string targetFolder)
        {
            string targetDBFile = Path.Combine(targetFolder, Path.GetFileName(DBAdapter.GetDBPath()));
            if (File.Exists(targetDBFile)) File.Delete(targetDBFile);
            string oldStorageFolder = GetStorageFolder();
            DBAdapter.Close();

            bool success = false;
            try
            {
                // for safety copy first, then delete old state after everything is done
                EditorUtility.DisplayProgressBar("Moving Database", "Copying database to new location...", 0.2f);
                File.Copy(DBAdapter.GetDBPath(), targetDBFile);
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayProgressBar("Moving Preview Images", "Copying preview images to new location...", 0.4f);
                IOUtils.CopyDirectory(GetPreviewFolder(), GetPreviewFolder(targetFolder));
                EditorUtility.ClearProgressBar();

                // set new location
                SwitchDatabase(targetFolder);
                success = true;
            }
            catch
            {
                EditorUtility.DisplayDialog("Error Moving Data", "There were errors moving the existing database to a new location. Check the error log for details. Current database location remains unchanged.", "OK");
            }

            if (success)
            {
                EditorUtility.DisplayProgressBar("Freeing Up Space", "Removing backup files from old location...", 0.8f);
                Directory.Delete(oldStorageFolder, true);
                EditorUtility.ClearProgressBar();
            }
        }

        public static void SwitchDatabase(string targetFolder)
        {
            DBAdapter.Close();
            Config.customStorageLocation = targetFolder;
            SaveConfig();
            Init();
        }

        public static Asset ForgetAsset(int id)
        {
            DBAdapter.DB.Execute("delete from AssetFile where AssetId=?", id);

            Asset existing = DBAdapter.DB.Find<Asset>(id);
            if (existing == null) return null;

            existing.CurrentState = Asset.State.New;
            DBAdapter.DB.Update(existing);

            return existing;
        }

        public static void RemoveAsset(int id)
        {
            Asset existing = ForgetAsset(id);
            if (existing == null) return;

            DBAdapter.DB.Execute("delete from Asset where Id=?", id);
        }

        public static async Task<string> CopyTo(AssetInfo assetInfo, string selectedPath, bool withDependencies = false, bool withScripts = false)
        {
            string result = null;
            string sourcePath = await EnsureMaterializedAsset(assetInfo);
            if (sourcePath != null)
            {
                string finalPath = selectedPath;

                // put into subfolder if multiple files are affected
                if (withDependencies)
                {
                    finalPath = Path.Combine(finalPath, Path.GetFileNameWithoutExtension(assetInfo.FileName));
                    if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);
                }

                string targetPath = Path.Combine(finalPath, Path.GetFileName(sourcePath));
                DoCopyTo(sourcePath, targetPath);
                result = targetPath;

                if (withDependencies)
                {
                    List<AssetFile> deps = withScripts ? assetInfo.Dependencies : assetInfo.MediaDependencies;
                    for (int i = 0; i < deps.Count; i++)
                    {
                        // check if already in target
                        if (!string.IsNullOrEmpty(deps[i].Guid))
                        {
                            if (!string.IsNullOrWhiteSpace(AssetDatabase.GUIDToAssetPath(deps[i].Guid))) continue;
                        }

                        sourcePath = await EnsureMaterializedAsset(assetInfo.ToAsset(), deps[i]);
                        targetPath = Path.Combine(finalPath, Path.GetFileName(deps[i].Path));
                        DoCopyTo(sourcePath, targetPath);
                    }
                }

                AssetDatabase.Refresh();
                assetInfo.ProjectPath = AssetDatabase.GUIDToAssetPath(assetInfo.Guid);
            }

            return result;
        }

        private static void DoCopyTo(string sourcePath, string targetPath)
        {
            File.Copy(sourcePath, targetPath, true);

            string sourceMetaPath = sourcePath + ".meta";
            string targetMetaPath = targetPath + ".meta";
            if (File.Exists(sourceMetaPath)) File.Copy(sourceMetaPath, targetMetaPath, true);
        }

        public static async Task PlayAudio(AssetInfo assetInfo)
        {
            string targetPath = await EnsureMaterializedAsset(assetInfo);

            EditorAudioUtility.StopAllPreviewClips();
            if (targetPath != null)
            {
                AudioClip clip = await AssetUtils.LoadAudioFromFile(targetPath);
                if (clip != null) EditorAudioUtility.PlayPreviewClip(clip);
            }
        }

        public static void SetAssetExclusion(int id, bool exclude)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(id);
            if (asset == null) return;

            asset.Exclude = exclude;
            DBAdapter.DB.Update(asset);
        }

        public static bool AddAssetTag(Asset asset, string tag, bool fromAssetStore = false)
        {
            Tag existingT = AddTag(tag, fromAssetStore);
            if (existingT == null) return false;

            TagAssignment existingA = DBAdapter.DB.Find<TagAssignment>(t => t.TagId == existingT.Id && t.TargetId == asset.Id && t.TagTarget == TagAssignment.Target.Asset);
            if (existingA != null) return false; // already added

            TagAssignment newAssignment = new TagAssignment(existingT.Id, TagAssignment.Target.Asset, asset.Id);
            DBAdapter.DB.Insert(newAssignment);

            return true;
        }

        public static bool AddAssetTag(AssetInfo info, string tag)
        {
            if (!AddAssetTag(info.ToAsset(), tag)) return false;

            LoadTagAssignments(info);

            return true;
        }

        public static void RemoveAssetTag(AssetInfo info, TagInfo tagInfo, bool autoReload = true)
        {
            DBAdapter.DB.Delete<TagAssignment>(tagInfo.Id);

            if (autoReload) LoadTagAssignments(info);
        }

        public static void RemoveAssetTag(List<AssetInfo> infos, string name)
        {
            infos.ForEach(info =>
            {
                TagInfo tagInfo = info.AssetTags?.Find(t => t.Name == name);
                if (tagInfo == null) return;
                RemoveAssetTag(info, tagInfo, false);
                info.SetAssetTagsDirty();
            });
            LoadTagAssignments();
        }

        private static void LoadTagAssignments(AssetInfo info = null)
        {
            string dataQuery = "select *, TagAssignment.Id as Id from TagAssignment inner join Tag on Tag.Id = TagAssignment.TagId order by TagTarget, TargetId";
            _tags = DBAdapter.DB.Query<TagInfo>($"{dataQuery}").ToList();
            AssetTagHash = Random.Range(0, int.MaxValue);

            info?.SetAssetTagsDirty();
            OnTagsChanged?.Invoke();
        }

        public static List<TagInfo> GetAssetTags(int assetId)
        {
            return Tags?.Where(t => t.TagTarget == TagAssignment.Target.Asset && t.TargetId == assetId).OrderBy(t => t.Name).ToList();
        }

        public static void SaveTag(Tag tag)
        {
            DBAdapter.DB.Update(tag);
            LoadTagAssignments();
        }

        public static Tag AddTag(string name, bool fromAssetStore = false)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            Tag tag = DBAdapter.DB.Find<Tag>(t => t.Name.ToLower() == name.ToLower());
            if (tag == null)
            {
                tag = new Tag(name);
                tag.FromAssetStore = fromAssetStore;
                DBAdapter.DB.Insert(tag);

                OnTagsChanged?.Invoke();
            }
            else if (!tag.FromAssetStore && fromAssetStore)
            {
                tag.FromAssetStore = true;
                DBAdapter.DB.Update(tag); // don't trigger changed event in such cases, this is just for book-keeping
            }

            return tag;
        }

        public static void RenameTag(Tag tag, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return;

            tag.Name = newName;
            DBAdapter.DB.Update(tag);
            LoadTagAssignments();
        }

        public static void DeleteTag(Tag tag)
        {
            DBAdapter.DB.Execute("delete from TagAssignment where TagId=?", tag.Id);
            DBAdapter.DB.Delete<Tag>(tag.Id);
            LoadTagAssignments();
        }

        public static List<Tag> LoadTags()
        {
            return DBAdapter.DB.Table<Tag>().OrderBy(t => t.Name).ToList();
        }

        public static void ScheduleRecreatePreviews(Asset asset)
        {
            DBAdapter.DB.Execute("update AssetFile set PreviewState=2 where AssetId=?", asset.Id);
        }

        public static async void RecreatePreviews()
        {
            string query = "select *, AssetFile.Id as Id from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where AssetFile.PreviewState == ?";
            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>(query, AssetFile.PreviewOptions.Redo).ToList();
            PreviewGenerator.Init(files.Count);
            string previewPath = GetPreviewFolder();
            foreach (AssetInfo af in files)
            {
                // do not recreate for files with dependencies as that will throw lots of errors, need a full import for that (TODO)
                if (ScanDependencies.Contains(af.Type))
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Custom, af.Id);
                    continue;
                }

                await Task.Yield(); // let editor breath

                string previewFile = Path.Combine(previewPath, "af-" + af.Id + ".png");
                string sourcePath = await EnsureMaterializedAsset(af);
                if (sourcePath == null)
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Custom, af.Id);
                    continue;
                }

                PreviewGenerator.RegisterPreviewRequest(af.Id, sourcePath, previewFile, req =>
                {
                    AssetFile paf = DBAdapter.DB.Find<AssetFile>(req.ID);
                    if (paf == null) return;

                    // TODO: deduplicate
                    if (req.Obj is Texture2D tex)
                    {
                        paf.Width = tex.width;
                        paf.Height = tex.height;
                    }
                    if (req.Obj is AudioClip clip)
                    {
                        paf.Length = clip.length;
                    }
                    if (File.Exists(req.DestinationFile)) paf.PreviewFile = Path.GetFileName(previewFile);
                    paf.PreviewState = AssetFile.PreviewOptions.Custom;
                    DBAdapter.DB.Update(paf);
                });
                if (PreviewGenerator.ActiveRequestCount() > 100) await PreviewGenerator.ExportPreviews(10);
            }
            await PreviewGenerator.ExportPreviews();
        }
    }
}