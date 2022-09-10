using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeStage.PackageToFolder;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public class ImportUI : EditorWindow
    {
        private List<AssetInfo> _assets;
        private List<AssetInfo> _missingPackages;
        private List<AssetInfo> _importQueue;
        private Vector2 _scrollPos;
        private string _customFolder;
        private string _customFolderRel;
        private bool _importRunning;
        private bool _cancellationRequested;

        public static ImportUI ShowWindow()
        {
            ImportUI window = GetWindow<ImportUI>("Package Import Wizard");
            window.minSize = new Vector2(450, 150);

            return window;
        }

        public void OnEnable()
        {
            AssetDatabase.importPackageStarted += ImportStarted;
            AssetDatabase.importPackageCompleted += ImportCompleted;
            AssetDatabase.importPackageCancelled += ImportCancelled;
            AssetDatabase.importPackageFailed += ImportFailed;
        }

        public void OnDisable()
        {
            AssetDatabase.importPackageStarted += ImportStarted;
            AssetDatabase.importPackageCompleted += ImportCompleted;
            AssetDatabase.importPackageCancelled += ImportCancelled;
            AssetDatabase.importPackageFailed += ImportFailed;
        }

        private void ImportFailed(string packageName, string errorMessage)
        {
            AssetInfo info = FindAsset(packageName);
            if (info == null) return;

            info.ImportState = AssetInfo.ImportStateOptions.Failed;
            _assets.First(a => a.Id == info.Id).ImportState = info.ImportState;

            Debug.LogError($"Import of '{packageName}' failed: {errorMessage}");
        }

        private void ImportCancelled(string packageName)
        {
            AssetInfo info = FindAsset(packageName);
            if (info == null) return;

            info.ImportState = AssetInfo.ImportStateOptions.Queued;
            _assets.First(a => a.Id == info.Id).ImportState = info.ImportState;
        }

        private void ImportCompleted(string packageName)
        {
            AssetInfo info = FindAsset(packageName);
            if (info == null) return;

            info.ImportState = AssetInfo.ImportStateOptions.Imported;
            _assets.First(a => a.Id == info.Id).ImportState = info.ImportState;
        }

        private void ImportStarted(string packageName)
        {
            AssetInfo info = FindAsset(packageName);
            if (info == null) return;

            info.ImportState = AssetInfo.ImportStateOptions.Importing;
            _assets.First(a => a.Id == info.Id).ImportState = info.ImportState;
        }

        private AssetInfo FindAsset(string packageName)
        {
            return _importQueue?.Find(info => info.SafeName == packageName || info.Location == packageName + ".unitypackage");
        }

        public void Init(List<AssetInfo> assets)
        {
            _assets = assets;
            if (!string.IsNullOrWhiteSpace(_customFolder))
            {
                _customFolderRel = "Assets" + _customFolder.Substring(Application.dataPath.Length);
            }

            // check for non-existing downloads first
            _missingPackages = new List<AssetInfo>();
            _importQueue = new List<AssetInfo>();
            foreach (AssetInfo info in _assets)
            {
                if (info.SafeName == Asset.None) continue;
                if (!info.Downloaded)
                {
                    info.ImportState = AssetInfo.ImportStateOptions.Missing;
                    _missingPackages.Add(info);
                }
                else
                {
                    info.ImportState = AssetInfo.ImportStateOptions.Queued;
                    _importQueue.Add(info);
                }
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("Select packages in the Asset Inventory for importing first.", MessageType.Info);
                return;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.Width(85));
            EditorGUILayout.LabelField(_assets.Count.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Folder", EditorStyles.boldLabel, GUILayout.Width(85));
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(_customFolderRel) ? "-default-" : _customFolderRel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Select...", GUILayout.ExpandWidth(false))) SelectTargetFolder();
            if (!string.IsNullOrWhiteSpace(_customFolder) && GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                _customFolder = null;
                _customFolderRel = null;
                AssetInventory.SaveConfig();
            }
            GUILayout.EndHorizontal();

            if (_missingPackages.Count > 0)
            {
                EditorGUILayout.Space();
                if (_importQueue.Count > 0)
                {
                    EditorGUILayout.HelpBox($"{_missingPackages.Count} packages have not been downloaded yet through the Package Manager and can therefore not be imported.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("The packages have not been downloaded yet through the Package Manager and can therefore not be imported.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(10);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            foreach (AssetInfo info in _assets)
            {
                if (info.SafeName == Asset.None) continue;

                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(info.Downloaded, GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField(new GUIContent(info.GetDisplayName, info.Location));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(info.ImportState.ToString());
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_importRunning);
            if (GUILayout.Button("Import Interactive...")) BulkImportAssets(_assets);
            if (GUILayout.Button("Import Automatically")) BulkImportAssets(_assets, false);
            EditorGUI.EndDisabledGroup();
            if (_importRunning && GUILayout.Button("Cancel All")) _cancellationRequested = true;
            GUILayout.EndHorizontal();
        }

        private void SelectTargetFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select target folder in your project", _customFolder, "");
            if (string.IsNullOrEmpty(folder)) return;

            if (folder.StartsWith(Application.dataPath))
            {
                _customFolder = folder;
                _customFolderRel = "Assets" + folder.Substring(Application.dataPath.Length);
                AssetInventory.SaveConfig();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "The target folder must be inside your current Unity project.", "OK");
            }
        }

        private async void BulkImportAssets(List<AssetInfo> assetIds, bool interactive = true)
        {
            if (assetIds.Count == 0) return;

            _importRunning = true;
            _cancellationRequested = false;

            if (!string.IsNullOrWhiteSpace(_customFolder))
            {
                _customFolderRel = "Assets" + _customFolder.Substring(Application.dataPath.Length);
                if (!Directory.Exists(_customFolder)) Directory.CreateDirectory(_customFolder);
            }

            // AssetDatabase.StartAssetEditing(); // TODO: will cause progress UI to stay on top and not close anymore
            try
            {
                foreach (AssetInfo info in _importQueue.Where(info => info.ImportState == AssetInfo.ImportStateOptions.Queued))
                {
                    info.ImportState = AssetInfo.ImportStateOptions.Importing;

                    // launch directly or intercept package resolution to tweak paths
                    if (string.IsNullOrWhiteSpace(_customFolderRel))
                    {
                        AssetDatabase.ImportPackage(info.Location, interactive);
                    }
                    else
                    {
                        Package2Folder.ImportPackageToFolder(info.Location, _customFolderRel, interactive);
                    }

                    // wait until done
                    while (!_cancellationRequested && info.ImportState == AssetInfo.ImportStateOptions.Importing)
                    {
                        await Task.Delay(25);
                    }

                    if (info.ImportState == AssetInfo.ImportStateOptions.Importing) info.ImportState = AssetInfo.ImportStateOptions.Queued;
                    if (_cancellationRequested) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error importing packages: " + e.Message);
            }

            // handle potentially pending imports and put them back in the queue
            _assets.ForEach(info =>
            {
                if (info.ImportState == AssetInfo.ImportStateOptions.Importing) info.ImportState = AssetInfo.ImportStateOptions.Queued;
            });

            // AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            _importRunning = false;
        }
    }
}