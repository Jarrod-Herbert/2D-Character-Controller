using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetInventory
{
    public static class AssetUtils
    {
        private const int Timeout = 30;
        private static readonly Regex NoSpecialChars = new Regex("[^a-zA-Z0-9 -]");
        private static Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();

        public static int GetPageCount(int resultCount, int maxResults)
        {
            return (int) Math.Ceiling((double) resultCount / (maxResults > 0 ? maxResults : int.MaxValue));
        }

        public static async Task<AudioClip> LoadAudioFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            // workaround for Unity not supporting loading local files with # or + in the name
            if (filePath.Contains("#") || filePath.Contains("+"))
            {
                string newName = Path.Combine(Application.temporaryCachePath, "AIAudioPreview" + Path.GetExtension(filePath));
                File.Copy(filePath, newName, true);
                filePath = newName;
            }

            // select appropriate audio type from extension where UNKNOWN heuristic can fail, especially for AIFF
            AudioType type = AudioType.UNKNOWN;
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".aiff":
                    type = AudioType.AIFF;
                    break;
            }

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, type))
            {
                ((DownloadHandlerAudioClip) uwr.downloadHandler).streamAudio = true;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError($"Error fetching '{filePath}': {uwr.error}");
                    return null;
                }

                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip) uwr.downloadHandler;
                dlHandler.streamAudio = false; // otherwise tracker files won't work
                if (dlHandler.isDone)
                {
                    // can fail if FMOD encounters incorrect file, will return null then, error cannot be surpressed
                    return dlHandler.audioClip;
                }
            }

            return null;
        }

        public static IEnumerator LoadTextures(List<AssetInfo> assetInfos)
        {
            foreach (AssetInfo info in assetInfos)
            {
                yield return LoadTexture(info);
            }
        }

        public static IEnumerator LoadTexture(AssetInfo assetInfo)
        {
            if (string.IsNullOrEmpty(assetInfo.PreviewImage)) yield break;

            string previewFolder = AssetInventory.GetPreviewFolder();
            string previewFile = Path.Combine(previewFolder, assetInfo.PreviewImage);
            if (!File.Exists(previewFile)) yield break;

            yield return LoadTexture(previewFile, result => { assetInfo.PreviewTexture = result; }, true);
        }

        public static IEnumerator LoadTexture(string file, Action<Texture2D> callback, bool useCache = false)
        {
            if (useCache && _previewCache.ContainsKey(file))
            {
                callback?.Invoke(_previewCache[file]);
                yield break;
            }

            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + file);
            yield return www.SendWebRequest();

            Texture2D result = DownloadHandlerTexture.GetContent(www);
            if (useCache) _previewCache[file] = result;

            callback?.Invoke(result);
        }

        public static async Task<T> FetchAPIData<T>(string uri, string token, string etag = null, Action<string> eTagCallback = null)
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(uri))
            {
                uwr.SetRequestHeader("Authorization", "Bearer " + token);
                if (!string.IsNullOrEmpty(etag)) uwr.SetRequestHeader("If-None-Match", etag);
                uwr.timeout = Timeout;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError)
#else
                if (uwr.isNetworkError)
#endif
                {
                    Debug.LogError($"Could not fetch API data from {uri} due to network issues: {uwr.error}");
                }
#if UNITY_2020_1_OR_NEWER
                else if (uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                else if (uwr.isHttpError)
#endif
                {
                    if (uwr.responseCode == (int) HttpStatusCode.Unauthorized)
                    {
                        Debug.LogError($"Invalid or expired API Token when contacting {uri}");
                    }
                    else
                    {
                        Debug.LogError($"Error fetching API data from {uri}: {uwr.downloadHandler.text}");
                    }
                }
                else
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (T) Convert.ChangeType(uwr.downloadHandler.text, typeof(T));
                    }
                    string newEtag = uwr.GetResponseHeader("ETag");
                    if (!string.IsNullOrEmpty(newEtag)) eTagCallback?.Invoke(newEtag);

                    return JsonConvert.DeserializeObject<T>(uwr.downloadHandler.text);
                }
            }
            return default;
        }

        public static string GuessSafeName(string name, string replacement = "")
        {
            // remove special characters like Unity does when saving to disk
            // This will work in 99% of cases but sometimes items get renamed and
            // Unity will keep the old safe name so this needs to be synced with the 
            // download info API.
            string clean = name;

            // special characters
            clean = NoSpecialChars.Replace(clean, replacement);

            // duplicate spaces
            clean = Regex.Replace(clean, @"\s+", " ");

            return clean.Trim();
        }
    }
}