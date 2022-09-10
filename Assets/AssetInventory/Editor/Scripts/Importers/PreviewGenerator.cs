using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetInventory
{
    public static class PreviewGenerator
    {
        private const string PreviewFolder = "_AssetInventoryPreviewsTemp";
        private const int MinPreviewCacheSize = 200;
        private static readonly List<PreviewRequest> _requests = new List<PreviewRequest>();

        public static void Init(int expectedFileCount)
        {
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(MinPreviewCacheSize, expectedFileCount + 100));
        }

        public static int ActiveRequestCount() => _requests.Count;

        public static void RegisterPreviewRequest(int id, string sourceFile, string previewDestination, Action<PreviewRequest> onSuccess)
        {
            PreviewRequest request = new PreviewRequest
            {
                ID = id, SourceFile = sourceFile, DestinationFile = previewDestination, OnSuccess = onSuccess
            };

            string targetDir = Path.Combine(Application.dataPath, PreviewFolder);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            request.TempFile = Path.Combine(targetDir, id + Path.GetExtension(sourceFile));
            File.Copy(sourceFile, request.TempFile);

            request.TempFileRel = request.TempFile;
            if (request.TempFileRel.StartsWith(Application.dataPath))
            {
                request.TempFileRel = "Assets" + request.TempFileRel.Substring(Application.dataPath.Length);
            }
            if (!File.Exists(request.TempFileRel)) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); // can happen in very rare cases, not yet clear why
            AssetDatabase.ImportAsset(request.TempFileRel);

            // trigger creation, fetch later as it takes a while
            request.Obj = AssetDatabase.LoadAssetAtPath<Object>(request.TempFileRel);
            if (request.Obj != null)
            {
                request.TimeStarted = Time.realtimeSinceStartup;
                AssetPreview.GetAssetPreview(request.Obj);
            }

            _requests.Add(request);
        }

        public static async Task ExportPreviews(int limit = 0)
        {
            while (_requests.Count > limit)
            {
                for (int i = _requests.Count - 1; i >= 0; i--)
                {
                    PreviewRequest req = _requests[i];
                    Texture2D icon = AssetPreview.GetAssetPreview(req.Obj);
                    if (icon == null && AssetPreview.IsLoadingAssetPreview(req.Obj.GetInstanceID()))
                    {
                        AssetPreview.GetAssetPreview(req.Obj);
                        continue;
                    }
                    icon = AssetPreview.GetAssetPreview(req.Obj);

                    // still will not return something for all assets
                    if (icon != null && icon.isReadable)
                    {
                        byte[] bytes = icon.EncodeToPNG();
                        if (bytes != null) File.WriteAllBytes(req.DestinationFile, bytes);
                    }
                    File.Delete(req.TempFile);
                    File.Delete(req.TempFile + ".meta");
                    req.OnSuccess?.Invoke(req);
                    _requests.RemoveAt(i);
                    await Task.Yield();
                }
            }
        }

        public static void Clear()
        {
            _requests.Clear();

            string targetDir = Path.Combine(Application.dataPath, PreviewFolder);
            if (!Directory.Exists(targetDir)) return;

            Directory.Delete(targetDir, true);
            File.Delete(targetDir + ".meta");

            AssetDatabase.Refresh();
        }
    }

    public class PreviewRequest
    {
        public int ID;
        public string SourceFile;
        public string TempFile;
        public string TempFileRel;
        public string DestinationFile;
        public Object Obj;
        public Action<PreviewRequest> OnSuccess;

        public float TimeStarted;
    }
}