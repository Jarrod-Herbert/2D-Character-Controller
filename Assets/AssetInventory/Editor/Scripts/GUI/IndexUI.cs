// reference for built-in icons: https://github.com/halak/unity-editor-icons

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JD.EditorAudioUtils;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public class IndexUI : BasicEditorUI
    {
        private const float SearchDelay = 0.3f;
        private readonly bool _debugMode = false;

        private readonly Dictionary<string, string> _staticPreviews = new Dictionary<string, string>
        {
            {"cs", "cs Script Icon"},
            {"php", "TextAsset Icon"},
            {"cg", "TextAsset Icon"},
            {"cginc", "TextAsset Icon"},
            {"js", "d_Js Script Icon"},
            {"prefab", "d_Prefab Icon"},
            {"fbx", "d_PrefabModel Icon"},
            {"dll", "dll Script Icon"},
            {"meta", "MetaFile Icon"},
            {"unity", "d_SceneAsset Icon"},
            {"asset", "EditorSettings Icon"},
            {"txt", "TextScriptImporter Icon"},
            {"md", "TextScriptImporter Icon"},
            {"doc", "TextScriptImporter Icon"},
            {"docx", "TextScriptImporter Icon"},
            {"pdf", "TextScriptImporter Icon"},
            {"rtf", "TextScriptImporter Icon"},
            {"readme", "TextScriptImporter Icon"},
            {"chm", "TextScriptImporter Icon"},
            {"compute", "ComputeShader Icon"},
            {"shader", "Shader Icon"},
            {"shadergraph", "Shader Icon"},
            {"shadersubgraph", "Shader Icon"},
            {"mat", "d_Material Icon"},
            {"wav", "AudioImporter Icon"},
            {"mp3", "AudioImporter Icon"},
            {"ogg", "AudioImporter Icon"},
            {"xml", "UxmlScript Icon"},
            {"html", "UxmlScript Icon"},
            {"uss", "UssScript Icon"},
            {"css", "StyleSheet Icon"},
            {"json", "StyleSheet Icon"},
            {"exr", "d_ReflectionProbe Icon"}
        };

        private List<AssetInfo> _files;
        private List<Tag> _tags;
        private GUIContent[] _contents;
        private string[] _assetNames;
        private string[] _tagNames;
        private string[] _publisherNames;
        private string[] _categoryNames;
        private string[] _types;
        private string[] _resultSizes;
        private string[] _sortFields;
        private string[] _tileTitle;
        private string[] _groupByOptions;

        private int _gridSelection;
        private string _searchPhrase;
        private string _searchWidth;
        private string _searchHeight;
        private string _searchLength;
        private string _searchSize;
        private string _assetSearchPhrase;
        private bool _checkMaxWidth;
        private bool _checkMaxHeight;
        private bool _checkMaxLength;
        private bool _checkMaxSize;
        private int _selectedPublisher;
        private int _selectedCategory;
        private int _selectedType;
        private int _selectedAsset;
        private int _selectedTag;
        private bool _showSettings;
        private int _tab;
        private string _newTag;
        private int _lastMainProgress;

        private Vector2 _searchScrollPos;
        private Vector2 _folderScrollPos;
        private Vector2 _assetScrollPos;
        private Vector2 _inspectorScrollPos;
        private Vector2 _usedAssetsScrollPos;
        private Vector2 _assetsScrollPos;
        private Vector2 _statsScrollPos;
        private Vector2 _settingsScrollPos;

        private int _curPage = 1;
        private int _pageCount;

        private bool _previewInProgress;
        private EditorCoroutine _textureLoading;

        private string _token;

        private string[] _pvSelection;
        private string _pvSelectedPath;
        private string _pvSelectedFolder;
        private bool _pvSelectionChanged;
        private List<AssetInfo> _pvSelectedAssets;
        private int _selectedFolderIndex = -1;
        private AssetInfo _selectedEntry;
        private bool _showMaintenance;
        private bool _packageAvailable;
        private long _dbSize;
        private long _cacheSize;
        private int _resultCount;
        private int _packageCount;
        private int _deprecatedAssetsCount;
        private int _packageFileCount;

        private AssetPurchases _purchasedAssets = new AssetPurchases();
        private int _purchasedAssetsCount;
        private List<AssetInfo> _assets;
        private int _indexedPackageCount;

        private List<AssetInfo> _assetUsage;
        private List<string> _usedAssets;
        private List<AssetInfo> _identifiedPackages;

        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;
        private SearchField AssetSearchField => _assetSearchField = _assetSearchField ?? new SearchField();
        private SearchField _assetSearchField;

        private float _nextSearchTime;
        [SerializeField] MultiColumnHeaderState _assetMCHState;

        private Rect AssetTreeRect => new Rect(20, 0, position.width - 40, position.height - 60);

        private TreeViewWithTreeModel<AssetInfo> AssetTreeView
        {
            get
            {
                if (_assetTreeViewState == null) _assetTreeViewState = new TreeViewState();

                MultiColumnHeaderState headerState = AssetTreeViewControl.CreateDefaultMultiColumnHeaderState(AssetTreeRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_assetMCHState, headerState)) MultiColumnHeaderState.OverwriteSerializedFields(_assetMCHState, headerState);
                _assetMCHState = headerState;

                if (_assetTreeView == null)
                {
                    MultiColumnHeader mch = new MultiColumnHeader(headerState);
                    mch.canSort = false;
                    mch.height = MultiColumnHeader.DefaultGUI.minimumHeight;
                    mch.ResizeToFit();

                    _assetTreeView = new AssetTreeViewControl(_assetTreeViewState, mch, AssetTreeModel);
                    _assetTreeView.OnSelectionChanged += OnAssetTreeSelectionChanged;
                    _assetTreeView.OnDoubleClickedItem += OnAssetTreeDoubleClicked;
                    _assetTreeView.Reload();
                }
                return _assetTreeView;
            }
        }

        private TreeViewWithTreeModel<AssetInfo> _assetTreeView;
        private TreeViewState _assetTreeViewState;

        private TreeModel<AssetInfo> AssetTreeModel
        {
            get
            {
                if (_assetTreeModel == null) _assetTreeModel = new TreeModel<AssetInfo>(new List<AssetInfo> {new AssetInfo().WithTreeData("Root", depth: -1)});
                return _assetTreeModel;
            }
        }

        private TreeModel<AssetInfo> _assetTreeModel;
        private AssetInfo _selectedTreeAsset;
        private List<AssetInfo> _selectedTreeAssets;
        private readonly Dictionary<string, Tuple<int, Color>> _bulkTags = new Dictionary<string, Tuple<int, Color>>();

        private class AdditionalFoldersWrapper : ScriptableObject
        {
            public List<FolderSpec> folders = new List<FolderSpec>();
        }

        private ReorderableList FolderListControl
        {
            get
            {
                if (_folderListControl == null) InitFolderControl();
                return _folderListControl;
            }
        }

        private ReorderableList _folderListControl;

        private SerializedObject SerializedFoldersObject
        {
            get
            {
                // reference can become null on reload
                if (_serializedFoldersObject == null || _serializedFoldersObject.targetObjects.FirstOrDefault() == null) InitFolderControl();
                return _serializedFoldersObject;
            }
        }

        private SerializedObject _serializedFoldersObject;
        private SerializedProperty _foldersProperty;

        private static bool _requireAssetTreeRebuild;
        private static bool _requireLookupUpdate;
        private static bool _requireSearchUpdate;
        private int _awakeTime;
        private Rect _buttonRect;

        [MenuItem("Assets/Asset Inventory", priority = 9000)]
        public static void ShowWindow()
        {
            IndexUI window = GetWindow<IndexUI>("Asset Inventory");
            window.minSize = new Vector2(550, 200);
        }

        private void Awake()
        {
            _awakeTime = Time.frameCount;
            AssetInventory.Init();
            InitFolderControl();

            _requireLookupUpdate = true;
            _requireSearchUpdate = true;
        }

        private void OnEnable()
        {
            AssetInventory.OnIndexingDone += OnTagsChanged;
            AssetInventory.OnTagsChanged += OnTagsChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            AssetInventory.OnIndexingDone -= OnTagsChanged;
            AssetInventory.OnTagsChanged -= OnTagsChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorAudioUtility.StopAllPreviewClips();
        }

        private void OnTagsChanged()
        {
            _requireLookupUpdate = true;
            _requireAssetTreeRebuild = true;
        }

        private void InitFolderControl()
        {
            AdditionalFoldersWrapper obj = CreateInstance<AdditionalFoldersWrapper>();
            obj.folders = AssetInventory.Config.folders;

            _serializedFoldersObject = new SerializedObject(obj);
            _foldersProperty = _serializedFoldersObject.FindProperty("folders");
            _folderListControl = new ReorderableList(_serializedFoldersObject, _foldersProperty, true, true, true, true);
            _folderListControl.drawElementCallback = DrawFoldersListItems;
            _folderListControl.drawHeaderCallback = DrawFoldersHeader;
            _folderListControl.onAddCallback = OnAddCallback;
            _folderListControl.onRemoveCallback = OnRemoveCallback;
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            if (_selectedFolderIndex < 0 || _selectedFolderIndex >= AssetInventory.Config.folders.Count) return;
            AssetInventory.Config.folders.RemoveAt(_selectedFolderIndex);
            AssetInventory.SaveConfig();
        }

        private void OnAddCallback(ReorderableList list)
        {
            string folder = EditorUtility.OpenFolderPanel("Select folder to index", "", "");
            if (string.IsNullOrEmpty(folder)) return;

            FolderSpec spec = new FolderSpec();
            spec.location = Path.GetFullPath(folder);
            AssetInventory.Config.folders.Add(spec);
            AssetInventory.SaveConfig();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // will crash editor otherwise
                if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
            }

#if UNITY_2020_1_OR_NEWER
            // ensure not to call perform search twice
            if (docked && Time.frameCount <= _awakeTime + 1) return; // will be called 1 frame after returning from maximized game view
#endif
            // UI will have lost all preview textures during play mode
            if (state == PlayModeStateChange.EnteredEditMode) _requireSearchUpdate = true;
        }

        private void ReloadLookups()
        {
            if (_debugMode) Debug.LogWarning("Reload Lookups");

            _requireLookupUpdate = false;
            _resultSizes = new[] {"-All-", string.Empty, "10", "25", "50", "100", "250", "500", "1000"};
            _sortFields = new[] {"Asset", "File Name", "Size", "Type", "Length", "Width", "Height", "Category", "Last Updated", "Rating", "#Ratings"};
            _groupByOptions = new[] {"-None-", string.Empty, "Category", "Tags"};
            _tileTitle = new[] {"Asset Path", "File Name", "File Name without Extension", "None"};

            UpdateStatistics();

            _assetNames = AssetInventory.ExtractAssetNames(_assets);
            _publisherNames = AssetInventory.ExtractPublisherNames(_assets);
            _categoryNames = AssetInventory.ExtractCategoryNames(_assets);
            _types = AssetInventory.LoadTypes();
            _tagNames = AssetInventory.ExtractTagNames(_tags);
            _token = CloudProjectSettings.accessToken;
            _purchasedAssetsCount = AssetInventory.CountPurchasedAssets(_assets);
        }

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            _requireAssetTreeRebuild = true;
            _requireSearchUpdate = true;
            PreviewGenerator.Clear();
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("The Asset Inventory is not available during play mode.", MessageType.Info);
                return;
            }

            if (_requireLookupUpdate) ReloadLookups();

            UIStyles.tile.fixedHeight = AssetInventory.Config.tileSize;
            UIStyles.tile.fixedWidth = AssetInventory.Config.tileSize;

            bool isNewTab = DrawToolbar();
            EditorGUILayout.Space();

            // centrally handle project view selections since used in multiple views
            CheckProjectViewSelection();

            switch (_tab)
            {
                case 0:
                    if (_requireSearchUpdate) PerformSearch(true);
                    DrawSearchTab();
                    break;

                case 1:
                    // will have lost asset tree on reload due to missing serialization
                    if (_requireAssetTreeRebuild) CreateAssetTree();
                    DrawPackagesTab();
                    break;

                case 2:
                    DrawReportingTab();
                    break;

                case 3:
                    if (isNewTab) UpdateStatistics();
                    DrawIndexTab();
                    break;

                case 4:
                    DrawAboutTab();
                    break;
            }

            // reload if there is new data
            if (_lastMainProgress != AssertImporter.MainProgress)
            {
                _lastMainProgress = AssertImporter.MainProgress;
                _requireLookupUpdate = true;
                _requireSearchUpdate = true;
            }
        }

        private void CheckProjectViewSelection()
        {
            _pvSelection = Selection.assetGUIDs;
            string oldPvSelectedPath = _pvSelectedPath;
            _pvSelectedPath = null;
            if (_pvSelection != null && _pvSelection.Length > 0)
            {
                _pvSelectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                _pvSelectedFolder = Directory.Exists(_pvSelectedPath) ? _pvSelectedPath : Path.GetDirectoryName(_pvSelectedPath);
                if (!string.IsNullOrWhiteSpace(_pvSelectedFolder)) _pvSelectedFolder = _pvSelectedFolder.Replace('/', Path.DirectorySeparatorChar);
            }
            _pvSelectionChanged = oldPvSelectedPath != _pvSelectedPath;
            if (_pvSelectionChanged) _pvSelectedAssets = null;
        }

        private bool DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            List<string> strings = new List<string>
            {
                "Search",
                "Packages",
                "Reporting",
                "Settings" + (AssetInventory.CurrentMain != null || AssetInventory.IndexingInProgress ? " (indexing)" : "")
            };
            _tab = GUILayout.Toolbar(_tab, strings.ToArray(), GUILayout.Height(32), GUILayout.MinWidth(500));
            bool newTab = EditorGUI.EndChangeCheck();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Help", "About"), EditorStyles.label)) _tab = 4;
            GUILayout.EndHorizontal();

            return newTab;
        }

        private void DrawSearchTab()
        {
            if (_packageFileCount == 0)
            {
                bool canStillSearch = AssetInventory.IndexingInProgress || _packageCount == 0 || AssetInventory.Config.indexPackageContents;
                if (canStillSearch)
                {
                    EditorGUILayout.HelpBox("The search index needs to be initialized. Start it right from here or go to the Index tab to configure the details.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The search is only available if package contents was indexed.", MessageType.Info);
                }

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (canStillSearch)
                {
                    EditorGUILayout.Space(30);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(AssetInventory.IndexingInProgress);
                    if (GUILayout.Button("Start Indexing", GUILayout.Height(50), GUILayout.MaxWidth(400))) StartInitialIndexing();
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                bool dirty = false;
                EditorGUIUtility.labelWidth = 60;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(EditorGUIUtility.IconContent("Preset.Context", "Search Details")))
                {
                    AssetInventory.Config.showFilterBar = !AssetInventory.Config.showFilterBar;
                    AssetInventory.SaveConfig();
                    dirty = true;
                }
                EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
                EditorGUI.BeginChangeCheck();
                _searchPhrase = SearchField.OnGUI(_searchPhrase, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    // delay search to allow fast typing
                    _nextSearchTime = Time.realtimeSinceStartup + SearchDelay;
                }
                else if (_nextSearchTime > 0 && Time.realtimeSinceStartup > _nextSearchTime)
                {
                    _nextSearchTime = 0;
                    dirty = true;
                }
                EditorGUI.BeginChangeCheck();
                GUILayout.Space(2);
                _selectedType = EditorGUILayout.Popup(_selectedType, _types, GUILayout.ExpandWidth(false), GUILayout.MinWidth(85));
                if (EditorGUI.EndChangeCheck()) dirty = true;
                GUILayout.Space(2);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                if (AssetInventory.Config.showFilterBar)
                {
                    GUILayout.BeginVertical("Filter Bar", "window", GUILayout.Width(UIStyles.InspectorWidth));
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showDetailFilters = EditorGUILayout.Foldout(AssetInventory.Config.showDetailFilters, "Additional Filters");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showDetailFilters)
                    {
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Tag", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedTag = EditorGUILayout.Popup(_selectedTag, _tagNames, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Package", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedAsset = EditorGUILayout.Popup(_selectedAsset, _assetNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Publisher", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPublisher = EditorGUILayout.Popup(_selectedPublisher, _publisherNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedCategory = EditorGUILayout.Popup(_selectedCategory, _categoryNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Width", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxWidth ? "<=" : ">=", GUILayout.Width(25))) _checkMaxWidth = !_checkMaxWidth;
                        _searchWidth = EditorGUILayout.TextField(_searchWidth, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Height", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxHeight ? "<=" : ">=", GUILayout.Width(25))) _checkMaxHeight = !_checkMaxHeight;
                        _searchHeight = EditorGUILayout.TextField(_searchHeight, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Length", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxLength ? "<=" : ">=", GUILayout.Width(25))) _checkMaxLength = !_checkMaxLength;
                        _searchLength = EditorGUILayout.TextField(_searchLength, GUILayout.Width(58));
                        EditorGUILayout.LabelField("sec", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("File Size", "File size in kilobytes"), EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxSize ? "<=" : ">=", GUILayout.Width(25))) _checkMaxSize = !_checkMaxSize;
                        _searchSize = EditorGUILayout.TextField(_searchSize, GUILayout.Width(58));
                        EditorGUILayout.LabelField("kb", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck()) dirty = true;

                        if (GUILayout.Button("Reset Filters"))
                        {
                            ResetSearch(true);
                            _requireSearchUpdate = true;
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showSavedSearches = EditorGUILayout.Foldout(AssetInventory.Config.showSavedSearches, "Saved Searches");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showSavedSearches)
                    {
                        if (AssetInventory.Config.searches.Count == 0)
                        {
                            EditorGUILayout.HelpBox("Save different search settings to quickly pull up the results later again.", MessageType.Info);
                        }
                        if (GUILayout.Button("Save current search..."))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(string.IsNullOrEmpty(_searchPhrase) ? "My Search" : _searchPhrase, SaveSearch);
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }

                        EditorGUILayout.Space();
                        Color oldCol = GUI.backgroundColor;
                        for (int i = 0; i < AssetInventory.Config.searches.Count; i++)
                        {
                            SavedSearch search = AssetInventory.Config.searches[i];
                            GUILayout.BeginHorizontal();

                            if (ColorUtility.TryParseHtmlString($"#{search.color}", out Color color)) GUI.backgroundColor = color;
                            if (GUILayout.Button(search.name)) LoadSearch(search);
                            GUI.backgroundColor = oldCol;

                            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "Delete saved search").image, GUILayout.Width(30)))
                            {
                                AssetInventory.Config.searches.RemoveAt(i);
                                AssetInventory.SaveConfig();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (_debugMode && GUILayout.Button("Reload Lookups")) ReloadLookups();

                    GUILayout.EndVertical();
                }

                // result
                GUILayout.BeginVertical();
                if (_contents.Length > 0)
                {
                    if (_files == null) PerformSearch(); // happens during recompilation

                    GUILayout.BeginHorizontal();

                    // assets
                    GUILayout.BeginVertical();
                    EditorGUI.BeginChangeCheck();
                    _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, false, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO: implement paged endless scrolling, needs some pixel calculations though
                        // if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
                        // _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this);
                    }

                    int cells = Mathf.RoundToInt(Mathf.Clamp(Mathf.Floor((position.width - UIStyles.InspectorWidth * (AssetInventory.Config.showFilterBar ? 2 : 1) - UIStyles.BorderWidth - 25) / AssetInventory.Config.tileSize), 1, 99));
                    if (cells < 2) cells = 2;

                    EditorGUI.BeginChangeCheck();
                    _gridSelection = GUILayout.SelectionGrid(_gridSelection, _contents, cells, UIStyles.tile);
                    bool isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                    if (EditorGUI.EndChangeCheck())
                    {
                        _showSettings = false;
                        _selectedEntry = _files[_gridSelection];
                        EditorAudioUtility.StopAllPreviewClips();
                        isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                        if (AssetInventory.Config.autoPlayAudio && isAudio && !_previewInProgress) PlayAudio(_selectedEntry);
                        if (_selectedEntry != null)
                        {
                            _selectedEntry.ProjectPath = AssetDatabase.GUIDToAssetPath(_selectedEntry.Guid);
                            _selectedEntry.IsMaterialized = AssetInventory.IsMaterialized(_selectedEntry.ToAsset(), _selectedEntry);
                            EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTexture(_selectedEntry), this);

                            // if entry is already materialized calculate dependencies immediately
                            if (!_previewInProgress && _selectedEntry.DependencyState == AssetInfo.DependencyStateOptions.Unknown && _selectedEntry.IsMaterialized) CalculateDependencies(_selectedEntry);

                            _packageAvailable = _selectedEntry.AssetSource == Asset.Source.Directory || File.Exists(_selectedEntry.Location);
                            if (AssetInventory.Config.pingSelected && _selectedEntry.InProject) PingAsset(_selectedEntry);
                        }
                        else
                        {
                            _packageAvailable = false;
                        }
                    }

                    GUILayout.EndScrollView();

                    // navigation
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (_pageCount > 1)
                    {
                        EditorGUI.BeginDisabledGroup(_curPage <= 1);
                        if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) SetPage(_curPage - 1);
                        EditorGUI.EndDisabledGroup();

                        EditorGUIUtility.labelWidth = 0;
                        EditorGUILayout.LabelField(UIStyles.Content($"Page {_curPage}/{_pageCount}", $"{_resultCount} results in total"), UIStyles.centerLabel, GUILayout.Width(150));
                        EditorGUIUtility.labelWidth = 60;

                        EditorGUI.BeginDisabledGroup(_curPage >= _pageCount);
                        if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) SetPage(_curPage + 1);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{_resultCount} results", UIStyles.centerLabel, GUILayout.ExpandWidth(true));
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "Show/Hide Settings Tab"))) _showSettings = !_showSettings;
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();

                    GUILayout.EndVertical();

                    // inspector
                    GUILayout.BeginVertical();
                    GUILayout.BeginVertical("Details Inspector", "window", GUILayout.Width(UIStyles.InspectorWidth));
                    EditorGUILayout.Space();
                    _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                    if (_selectedEntry == null || string.IsNullOrEmpty(_selectedEntry.SafeName))
                    {
                        // will happen after script reload
                        EditorGUILayout.HelpBox("Select an asset for details", MessageType.Info);
                    }
                    else
                    {
                        if (!_packageAvailable)
                        {
                            EditorGUILayout.HelpBox("Not cached anymore. Download the asset in the Package Manager again.", MessageType.Info);
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("File", EditorStyles.largeLabel);
                        GUILabelWithText("Name", Path.GetFileName(_selectedEntry.Path));
                        if (_selectedEntry.AssetSource == Asset.Source.Directory) GUILabelWithText("Location", $"{Path.GetDirectoryName(_selectedEntry.Path)}");
                        GUILabelWithText("Size", EditorUtility.FormatBytes(_selectedEntry.Size));
                        if (_selectedEntry.Width > 0) GUILabelWithText("Dimensions", $"{_selectedEntry.Width}x{_selectedEntry.Height} px");
                        if (_selectedEntry.Length > 0) GUILabelWithText("Length", $"{_selectedEntry.Length:0.##} seconds");
                        GUILabelWithText("In Project", _selectedEntry.InProject ? "Yes" : "No");
                        if (_packageAvailable)
                        {
                            if (AssetInventory.ScanDependencies.Contains(_selectedEntry.Type))
                            {
                                switch (_selectedEntry.DependencyState)
                                {
                                    case AssetInfo.DependencyStateOptions.Unknown:
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel, GUILayout.Width(85));
                                        EditorGUI.BeginDisabledGroup(_previewInProgress);
                                        if (GUILayout.Button("Calculate", GUILayout.ExpandWidth(false))) CalculateDependencies(_selectedEntry);
                                        EditorGUI.EndDisabledGroup();
                                        GUILayout.EndHorizontal();
                                        break;

                                    case AssetInfo.DependencyStateOptions.Calculating:
                                        GUILabelWithText("Dependencies", "Calculating...");
                                        break;

                                    case AssetInfo.DependencyStateOptions.NotPossible:
                                        GUILabelWithText("Dependencies", "Cannot determine (binary)");
                                        break;

                                    case AssetInfo.DependencyStateOptions.Done:
                                        GUILayout.BeginHorizontal();
                                        GUILabelWithText("Dependencies", $"{_selectedEntry.MediaDependencies?.Count} + {_selectedEntry.ScriptDependencies?.Count} ({EditorUtility.FormatBytes(_selectedEntry.DependencySize)})");
                                        if (_selectedEntry.Dependencies.Count > 0 && GUILayout.Button("Show..."))
                                        {
                                            string list = string.Join("\n", _selectedEntry.Dependencies.Select(af => af.Path.Replace("Assets/", string.Empty).Replace(" ", "\u00A0")));
                                            EditorUtility.DisplayDialog("Asset Dependencies", list, "Close");
                                        }

                                        GUILayout.EndHorizontal();
                                        break;
                                }
                            }

                            if (!_selectedEntry.InProject && _pvSelectedFolder == null)
                            {
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField("Select a folder in Project View for import options", EditorStyles.centeredGreyMiniLabel);
                            }
                            EditorGUI.BeginDisabledGroup(_previewInProgress);
                            if (!_selectedEntry.InProject && !string.IsNullOrEmpty(_pvSelectedFolder))
                            {
                                GUILabelWithText("Import To", _pvSelectedFolder);
                                EditorGUILayout.Space();
                                if (GUILayout.Button("Import File" + (_selectedEntry.DependencySize > 0 ? " Only" : ""))) CopyTo(_selectedEntry, _pvSelectedFolder);
                                if (_selectedEntry.DependencySize > 0 && AssetInventory.ScanDependencies.Contains(_selectedEntry.Type))
                                {
                                    if (GUILayout.Button("Import With Dependencies")) CopyTo(_selectedEntry, _pvSelectedFolder, true);
                                    if (_selectedEntry.ScriptDependencies.Count > 0)
                                    {
                                        if (GUILayout.Button("Import With Dependencies + Scripts")) CopyTo(_selectedEntry, _pvSelectedFolder, true, true);
                                    }

                                    EditorGUILayout.Space();
                                }
                            }
                            else
                            {
                                EditorGUILayout.Space();
                            }

                            if (isAudio)
                            {
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_PlayButton", "Play"), GUILayout.Width(40))) PlayAudio(_selectedEntry);
                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreMatQuad", "Stop"), GUILayout.Width(40))) EditorAudioUtility.StopAllPreviewClips();
                                EditorGUILayout.Space();
                                AssetInventory.Config.autoPlayAudio = GUILayout.Toggle(AssetInventory.Config.autoPlayAudio, "Automatically play audio");
                                GUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }

                            if (_selectedEntry.InProject)
                            {
                                if (GUILayout.Button("Ping")) PingAsset(_selectedEntry);
                            }

                            if (GUILayout.Button("Open")) Open(_selectedEntry);
                            if (GUILayout.Button("Open in Explorer")) OpenExplorer(_selectedEntry);

                            if (!_selectedEntry.IsMaterialized && !_previewInProgress)
                            {
                                EditorGUILayout.LabelField("Asset will be extracted before actions are performed", EditorStyles.centeredGreyMiniLabel);
                            }
                        }

                        if (_previewInProgress)
                        {
                            EditorGUILayout.LabelField("Extracting...", EditorStyles.centeredGreyMiniLabel);
                        }

                        EditorGUILayout.Space();
                        DrawPackageDetails(_selectedEntry, false, true, false);
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    if (_showSettings)
                    {
                        EditorGUILayout.Space();
                        GUILayout.BeginVertical("View Settings", "window", GUILayout.Width(UIStyles.InspectorWidth));
                        EditorGUILayout.Space();

                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Sort by", "Specify sort order"), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.sortField = EditorGUILayout.Popup(AssetInventory.Config.sortField, _sortFields);
                        if (GUILayout.Button(AssetInventory.Config.sortDescending ? UIStyles.Content("˅", "Descending") : UIStyles.Content("˄", "Ascending"), GUILayout.Width(15)))
                        {
                            AssetInventory.Config.sortDescending = !AssetInventory.Config.sortDescending;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Results", "Maximum number of results to show"), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.maxResults = EditorGUILayout.Popup(AssetInventory.Config.maxResults, _resultSizes);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Hide Extensions", "File extensions to hide from search results, e.g. asset;json;txt. These will still be indexed."), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.excludedExtensions = EditorGUILayout.DelayedTextField(AssetInventory.Config.excludedExtensions);
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck())
                        {
                            dirty = true;
                            _curPage = 1;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUILayout.Space();
                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Tile Size", "Dimensions of search result previews. Preview images will still be 128x128 max."), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.tileSize = EditorGUILayout.IntSlider(AssetInventory.Config.tileSize, 50, 300);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Tile Text", "Text to be shown on the tile"), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.tileText = EditorGUILayout.Popup(AssetInventory.Config.tileText, _tileTitle);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            dirty = true;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUILayout.Space();
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Auto-Play Audio", "Will automatically extract unity packages to play the sound file if they were not extracted yet. This is the most convenient option but will require sufficient hard disk space."), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.autoPlayAudio = EditorGUILayout.Toggle(AssetInventory.Config.autoPlayAudio);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Ping Selected", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.pingSelected = EditorGUILayout.Toggle(AssetInventory.Config.pingSelected);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Group List", "Add a second level hierarchy to dropdowns if they become too long to scroll."), EditorStyles.boldLabel, GUILayout.Width(110));
                        AssetInventory.Config.groupLists = EditorGUILayout.Toggle(AssetInventory.Config.groupLists);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            AssetInventory.SaveConfig();
                            ReloadLookups();
                        }

                        EditorGUILayout.Space();
                        GUILayout.EndVertical();
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No matching results", UIStyles.whiteCenter, GUILayout.MinHeight(AssetInventory.Config.tileSize));
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (dirty) PerformSearch();
                EditorGUIUtility.labelWidth = 0;
            }
        }

        private void LoadSearch(SavedSearch search)
        {
            _searchPhrase = search.searchPhrase;
            _searchWidth = search.width;
            _searchHeight = search.height;
            _searchLength = search.length;
            _searchSize = search.size;
            _checkMaxWidth = search.checkMaxWidth;
            _checkMaxHeight = search.checkMaxHeight;
            _checkMaxLength = search.checkMaxLength;
            _checkMaxSize = search.checkMaxSize;

            _selectedType = Mathf.Max(0, Array.FindIndex(_types, s => s == search.type || s.EndsWith($"/{search.type}")));
            _selectedPublisher = Mathf.Max(0, Array.FindIndex(_publisherNames, s => s == search.publisher || s.EndsWith($"/{search.publisher}")));
            _selectedAsset = Mathf.Max(0, Array.FindIndex(_assetNames, s => s == search.package || s.EndsWith($"/{search.package}")));
            _selectedCategory = Mathf.Max(0, Array.FindIndex(_categoryNames, s => s == search.category || s.EndsWith($"/{search.category}")));
            _selectedTag = Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.tag || s.EndsWith($"/{search.tag}")));

            _requireSearchUpdate = true;
        }

        private void SaveSearch(string value)
        {
            SavedSearch spec = new SavedSearch();
            spec.name = value;
            spec.searchPhrase = _searchPhrase;
            spec.width = _searchWidth;
            spec.height = _searchHeight;
            spec.length = _searchLength;
            spec.size = _searchSize;
            spec.checkMaxWidth = _checkMaxWidth;
            spec.checkMaxHeight = _checkMaxHeight;
            spec.checkMaxLength = _checkMaxLength;
            spec.checkMaxSize = _checkMaxSize;
            spec.color = ColorUtility.ToHtmlStringRGB(Random.ColorHSV());

            if (_selectedType > 0 && _types.Length > _selectedType)
            {
                spec.type = _types[_selectedType].Split('/').LastOrDefault();
            }

            if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
            {
                spec.publisher = _publisherNames[_selectedPublisher].Split('/').LastOrDefault();
            }

            if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
            {
                spec.package = _assetNames[_selectedAsset].Split('/').LastOrDefault();
            }

            if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
            {
                spec.category = _categoryNames[_selectedCategory].Split('/').LastOrDefault();
            }

            if (_selectedTag > 0 && _tagNames.Length > _selectedTag)
            {
                spec.tag = _tagNames[_selectedTag].Split('/').LastOrDefault();
            }

            AssetInventory.Config.searches.Add(spec);
            AssetInventory.SaveConfig();
        }

        private void DrawPackageDetails(AssetInfo info, bool showMaintenance = false, bool showActions = true, bool startNewSection = true)
        {
            if (info.Id == 0) return;

            if (startNewSection)
            {
                GUILayout.BeginVertical("Package Details", "window", GUILayout.Width(UIStyles.InspectorWidth));
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.LabelField("Package", EditorStyles.largeLabel);
            }
            GUILabelWithText("Name", info.GetDisplayName);
            if (!string.IsNullOrWhiteSpace(info.GetDisplayPublisher)) GUILabelWithText("Publisher", $"{info.GetDisplayPublisher}");
            if (!string.IsNullOrWhiteSpace(info.GetDisplayCategory)) GUILabelWithText("Category", $"{info.GetDisplayCategory}");
            if (info.PackageSize > 0) GUILabelWithText("Size", EditorUtility.FormatBytes(info.PackageSize));
            if (!string.IsNullOrWhiteSpace(info.AssetRating))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Rating", "Rating given by Asset Store users"), EditorStyles.boldLabel, GUILayout.Width(85));
                if (int.TryParse(info.AssetRating, out int rating))
                {
                    Color oldCC = GUI.contentColor;
#if UNITY_2021_1_OR_NEWER
                    // favicon is not gold anymore                    
                    GUI.contentColor = new Color(0.992f, 0.694f, 0.004f);
#endif
                    for (int i = 0; i < rating; i++)
                    {
                        GUILayout.Button(EditorGUIUtility.IconContent("Favorite Icon"), EditorStyles.label, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    GUI.contentColor = oldCC;
                    for (int i = rating; i < 5; i++)
                    {
                        GUILayout.Button(EditorGUIUtility.IconContent("Favorite"), EditorStyles.label, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"{info.AssetRating} ");
                }
                EditorGUILayout.LabelField($"({info.RatingCount} ratings)", GUILayout.MaxWidth(90));
                GUILayout.EndHorizontal();
            }
            if (info.AssetSource == Asset.Source.AssetStorePackage && info.LastRelease.Year > 1)
            {
                GUILabelWithText("Last Update", info.LastRelease.ToString("ddd, MMM d yyyy") + (!string.IsNullOrEmpty(info.Version) ? $" ({info.Version})" : string.Empty));
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel, GUILayout.Width(85));
            switch (info.AssetSource)
            {
                case Asset.Source.AssetStorePackage:
                    if (GUILayout.Button("Asset Store", EditorStyles.linkLabel)) Application.OpenURL(info.GetItemLink());
                    break;

                default:
                    EditorGUILayout.LabelField(info.AssetSource.ToString());
                    break;
            }
            GUILayout.EndHorizontal();

            if (showMaintenance)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Exclude", "Will not index the asset and not show existing index results in the search."), EditorStyles.boldLabel, GUILayout.Width(85));
                EditorGUI.BeginChangeCheck();
                info.Exclude = EditorGUILayout.Toggle(info.Exclude);
                if (EditorGUI.EndChangeCheck())
                {
                    AssetInventory.SetAssetExclusion(info.Id, info.Exclude);
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                }
                GUILayout.EndHorizontal();
            }
            if (info.OfficialState == "deprecated") EditorGUILayout.HelpBox("This asset is deprecated.", MessageType.Warning);

            if (showActions)
            {
                if (info.AssetSource == Asset.Source.AssetStorePackage || info.AssetSource == Asset.Source.CustomPackage)
                {
                    EditorGUILayout.Space();
                    if (info.Downloaded)
                    {
                        if (GUILayout.Button("Import Package..."))
                        {
                            ImportUI importUI = ImportUI.ShowWindow();
                            importUI.Init(new List<AssetInfo> {info});
                        }
                        if (showMaintenance)
                        {
                            if (GUILayout.Button("Open in Search")) OpenInSearch(info);
                            if (GUILayout.Button("Reindex Package on Next Run"))
                            {
                                AssetInventory.ForgetAsset(info.AssetId);
                                _requireLookupUpdate = true;
                                _requireSearchUpdate = true;
                                _requireAssetTreeRebuild = true;
                            }
                            if (_debugMode && GUILayout.Button("Recreate Previews"))
                            {
                                AssetInventory.ScheduleRecreatePreviews(info.ToAsset());
                                AssetInventory.RecreatePreviews();
                            }
                        }
                    }
                }
                if (showMaintenance && GUILayout.Button("Delete From Database"))
                {
                    AssetInventory.RemoveAsset(info.AssetId);
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                    _requireAssetTreeRebuild = true;
                }

                DrawAddTag(new List<AssetInfo> {info});

                if (info.AssetTags != null && info.AssetTags.Count > 0)
                {
                    float x = 0f;
                    foreach (TagInfo tagInfo in info.AssetTags)
                    {
                        x = CalcTagSize(x, tagInfo.Name);
                        UIStyles.DrawTag(tagInfo, () =>
                        {
                            AssetInventory.RemoveAssetTag(info, tagInfo);
                            _requireAssetTreeRebuild = true;
                        });
                    }
                }
                GUILayout.EndHorizontal();
            }
            if (info.PreviewTexture != null)
            {
                EditorGUILayout.Space();
                GUILayout.FlexibleSpace();
                GUILayout.Box(info.PreviewTexture, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(UIStyles.InspectorWidth), GUILayout.MaxHeight(100));
                GUILayout.FlexibleSpace();
            }
            if (startNewSection) GUILayout.EndVertical();
        }

        private static float CalcTagSize(float x, string name)
        {
            x += UIStyles.tag.CalcSize(UIStyles.Content(name)).x + UIStyles.TagSizeSpacing + EditorGUIUtility.singleLineHeight + UIStyles.tag.margin.right * 2f;
            if (x > UIStyles.InspectorWidth - UIStyles.TagOuterMargin * 3)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(85 + 3);
                x = UIStyles.tag.CalcSize(UIStyles.Content(name)).x + UIStyles.TagSizeSpacing + EditorGUIUtility.singleLineHeight + UIStyles.tag.margin.right * 2f;
            }
            return x;
        }

        private void DrawAddTag(List<AssetInfo> info)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(UIStyles.Content("Add Tag..."), GUILayout.Width(70)))
                // if (GUILayout.Button(EditorGUIUtility.IconContent("FilterByLabel", "Add Tag"), GUILayout.Width(30)))
            {
                TagSelectionUI tagUI = new TagSelectionUI();
                tagUI.Init();
                tagUI.SetAsset(info);
                PopupWindow.Show(_buttonRect, tagUI);
            }
            if (Event.current.type == EventType.Repaint) _buttonRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(15);
        }

        private void DrawIndexTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            // folders
            EditorGUILayout.LabelField("Unity Asset Store downloads will be indexed automatically. Specify custom locations below to scan for unitypackages downloaded from somewhere else than the Asset Store or for any arbitrary media files like your model or sound library you want to access.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            AssetInventory.Config.indexAssetStore = GUILayout.Toggle(AssetInventory.Config.indexAssetStore, "Index Asset Store Downloads");
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();
#if UNITY_2022_1_OR_NEWER
            EditorGUILayout.LabelField("Only the default asset cache location will be scanned. If you moved it to a different location add that location as an additional folder below.", EditorStyles.miniLabel);
#endif
            EditorGUILayout.Space();
            _folderScrollPos = GUILayout.BeginScrollView(_folderScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            if (SerializedFoldersObject != null)
            {
                SerializedFoldersObject.Update();
                FolderListControl.DoLayoutList();
                SerializedFoldersObject.ApplyModifiedProperties();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.BeginVertical("Update", "window", GUILayout.Width(UIStyles.InspectorWidth), GUILayout.ExpandHeight(false));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Use the commands below to update the main index and to fetch the newest updates from the Asset Store.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            // local
            if (AssetInventory.IndexingInProgress)
            {
                EditorGUI.BeginDisabledGroup(AssertImporter.CancellationRequested);
                if (GUILayout.Button("Stop Indexing")) AssertImporter.CancellationRequested = true;
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                if (GUILayout.Button("Update Local Index")) AssetInventory.RefreshIndex();
            }

            // status
            if (AssetInventory.IndexingInProgress)
            {
                EditorGUILayout.Space();
                if (AssertImporter.MainCount > 0)
                {
                    EditorGUILayout.LabelField("Package Progress", EditorStyles.boldLabel);
                    DrawProgressBar(AssertImporter.MainProgress / (float) AssertImporter.MainCount, $"{AssertImporter.MainProgress}/{AssertImporter.MainCount}");
                    EditorGUILayout.LabelField("Package", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(!string.IsNullOrEmpty(AssertImporter.CurrentMain) ? Path.GetFileName(AssertImporter.CurrentMain) : "scanning...");
                }

                if (AssertImporter.SubCount > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("File Progress", EditorStyles.boldLabel);
                    DrawProgressBar(AssertImporter.SubProgress / (float) AssertImporter.SubCount, $"{AssertImporter.SubProgress}/{AssertImporter.SubCount} - " + Path.GetFileName(AssertImporter.CurrentSub));
                }
            }

            // asset store
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(AssetInventory.CurrentMain != null);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Asset Store Purchases")) FetchAssetPurchases();
            EditorGUI.EndDisabledGroup();
            if (AssetInventory.CurrentMain != null)
            {
                if (GUILayout.Button("Cancel")) AssetStore.CancellationRequested = true;
            }
            GUILayout.EndHorizontal();
            if (_debugMode && GUILayout.Button("Update Package Details")) FetchAssetDetails();

            if (AssetInventory.CurrentMain != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{AssetInventory.CurrentMain} {AssetInventory.MainProgress}/{AssetInventory.MainCount}", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Settings", "window", GUILayout.Width(UIStyles.InspectorWidth));
            EditorGUILayout.Space();
            _settingsScrollPos = GUILayout.BeginScrollView(_settingsScrollPos, false, false);
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Extract Previews", "Keep a folder with preview images for each asset file. Will require a moderate amount of space if there are many files."), EditorStyles.boldLabel, GUILayout.Width(120));
            AssetInventory.Config.extractPreviews = EditorGUILayout.Toggle(AssetInventory.Config.extractPreviews);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Icons as Previews", "Will show generic icons in case a file preview is missing instead of an empty tile."), EditorStyles.boldLabel, GUILayout.Width(120));
            AssetInventory.Config.showIconsForMissingPreviews = EditorGUILayout.Toggle(AssetInventory.Config.showIconsForMissingPreviews);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Full Metadata", "Will extract dimensions from images and length from audio files to make these searchable at the cost of a slower indexing process."), EditorStyles.boldLabel, GUILayout.Width(120));
            AssetInventory.Config.gatherExtendedMetadata = EditorGUILayout.Toggle(AssetInventory.Config.gatherExtendedMetadata);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Package Contents", "Will extract packages and make contents searchable. This is the foundation for the search. Deactivate only if you are solely interested in package metadata."), EditorStyles.boldLabel, GUILayout.Width(120));
            AssetInventory.Config.indexPackageContents = EditorGUILayout.Toggle(AssetInventory.Config.indexPackageContents);
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            if (EditorGUI.EndChangeCheck())
            {
                AssetInventory.SaveConfig();
                _requireLookupUpdate = true;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Statistics", "window", GUILayout.Width(UIStyles.InspectorWidth));
            EditorGUILayout.Space();
            _statsScrollPos = GUILayout.BeginScrollView(_statsScrollPos, false, false);
            GUILabelWithText("Indexed Packages", $"{_indexedPackageCount}/{_packageCount}", 120);
            GUILabelWithText("Indexed Files", $"{_packageFileCount}", 120);
            GUILabelWithText("Database Size", EditorUtility.FormatBytes(_dbSize), 120);
            GUILabelWithText("Cache Size", EditorUtility.FormatBytes(_cacheSize), 120);

            if (_indexedPackageCount < _packageCount - _deprecatedAssetsCount && !AssetInventory.IndexingInProgress)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("To index the remaining assets, download them in the Package Manager. Tip: Use Unity 2022.1 or newer to bulk download many/all assets at once.", MessageType.Info);
            }

            EditorGUILayout.Space();
            _showMaintenance = EditorGUILayout.Foldout(_showMaintenance, "Maintenance Functions");
            if (_showMaintenance)
            {
                EditorGUI.BeginDisabledGroup(AssetInventory.IndexingInProgress);
                if (GUILayout.Button("Clear Database"))
                {
                    if (DBAdapter.DeleteDB())
                    {
                        if (Directory.Exists(AssetInventory.GetPreviewFolder())) Directory.Delete(AssetInventory.GetPreviewFolder(), true);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Database seems to be in use by another program and could not be cleared.", "OK");
                    }
                    UpdateStatistics();
                    _assets = new List<AssetInfo>();
                }

                EditorGUI.BeginDisabledGroup(AssetInventory.ClearCacheInProgress);
                if (GUILayout.Button("Clear Cache")) AssetInventory.ClearCache(UpdateStatistics);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Reset Configuration")) AssetInventory.ResetConfig();
                if (DBAdapter.IsDBOpen())
                {
                    if (GUILayout.Button("Close Database (to allow copying)")) DBAdapter.Close();
                }

                EditorGUI.BeginDisabledGroup(AssetInventory.CurrentMain != null || AssetInventory.IndexingInProgress);
                if (GUILayout.Button("Change Database Location...")) SetDatabaseLocation();
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Database Location", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(AssetInventory.GetStorageFolder(), EditorStyles.wordWrappedLabel);

                EditorGUILayout.LabelField(UIStyles.Content("Config Location", "Copy the file into your project to use a project-specific configuration instead."), EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(AssetInventory.UsedConfigLocation, EditorStyles.wordWrappedLabel);

                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawProgressBar(float percentage, string text)
        {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, percentage, text);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.EndVertical();
        }

        private void DrawPackagesTab()
        {
            // asset list
            if (_assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No packages were indexed yet. Start the indexing process to fill this list.", MessageType.Info);
                GUILayout.BeginHorizontal();
            }
            else
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                EditorGUI.BeginChangeCheck();
                _assetSearchPhrase = AssetSearchField.OnGUI(_assetSearchPhrase, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck()) AssetTreeView.searchString = _assetSearchPhrase;

                EditorGUILayout.Space();
                EditorGUIUtility.labelWidth = 60;
                EditorGUI.BeginChangeCheck();
                AssetInventory.Config.assetGrouping = EditorGUILayout.Popup(UIStyles.Content("Group by:", "Select if packages should be grouped or not"), AssetInventory.Config.assetGrouping, _groupByOptions, GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck())
                {
                    CreateAssetTree();
                    AssetInventory.SaveConfig();
                }
                EditorGUIUtility.labelWidth = 0;

                if (AssetInventory.Config.assetGrouping > 0 && GUILayout.Button("Collapse All", GUILayout.ExpandWidth(false)))
                {
                    AssetTreeView.CollapseAll();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                AssetTreeView.OnGUI(new Rect(0, 83, position.width - UIStyles.InspectorWidth - 5, position.height - 83));
                GUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Space(4);
            GUILayout.BeginVertical("Overview", "window", GUILayout.Width(UIStyles.InspectorWidth), GUILayout.ExpandHeight(false));
            EditorGUILayout.Space();
            // FIXME: scrolling is broken for some reason, bar will often overlap
            // _assetsScrollPos = GUILayout.BeginScrollView(_assetsScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            GUILabelWithText("Total", _assets.Count.ToString());
            GUILabelWithText("Indexed", _indexedPackageCount.ToString());
            if (_deprecatedAssetsCount > 0) GUILabelWithText("Deprecated", $"{_deprecatedAssetsCount}");
            if (string.IsNullOrEmpty(_token))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Could not retrieve Asset Store purchases. Open the package manager once and go to the My Assets section.", MessageType.Warning);
                if (GUILayout.Button("Retry")) _token = CloudProjectSettings.accessToken;
                EditorGUILayout.Space();
            }
            else
            {
                GUILabelWithText("Asset Store", _purchasedAssetsCount.ToString());
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Package Manager")) UnityEditor.PackageManager.UI.Window.Open("");
            GUILayout.EndVertical();

            if (_selectedTreeAsset != null)
            {
                EditorGUILayout.Space();
                DrawPackageDetails(_selectedTreeAsset, true);
            }

            if (_selectedTreeAsset == null && _selectedTreeAssets != null && _selectedTreeAssets.Count > 0)
            {
                EditorGUILayout.Space();
                GUILayout.BeginVertical("Bulk Actions", "window", GUILayout.Width(UIStyles.InspectorWidth));
                GUILabelWithText("Selected", _selectedTreeAssets.Count.ToString());

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Exclude", EditorStyles.boldLabel, GUILayout.Width(85));
                if (GUILayout.Button("All", GUILayout.ExpandWidth(false)))
                {
                    _selectedTreeAssets.ForEach(info => AssetInventory.SetAssetExclusion(info.Id, true));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                }
                if (GUILayout.Button("None", GUILayout.ExpandWidth(false)))
                {
                    _selectedTreeAssets.ForEach(info => AssetInventory.SetAssetExclusion(info.Id, false));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (GUILayout.Button("Import..."))
                {
                    ImportUI importUI = ImportUI.ShowWindow();
                    importUI.Init(_selectedTreeAssets);
                }

                if (GUILayout.Button("Reindex Packages on Next Run"))
                {
                    _selectedTreeAssets.ForEach(info => AssetInventory.ForgetAsset(info.AssetId));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                    _requireAssetTreeRebuild = true;
                }
                if (GUILayout.Button("Delete From Database"))
                {
                    _selectedTreeAssets.ForEach(info => AssetInventory.RemoveAsset(info.AssetId));
                    _requireLookupUpdate = true;
                    _requireSearchUpdate = true;
                    _requireAssetTreeRebuild = true;
                }

                DrawAddTag(_selectedTreeAssets);

                float x = 0f;
                foreach (KeyValuePair<string, Tuple<int, Color>> bulkTag in _bulkTags)
                {
                    string tagName = $"{bulkTag.Key} ({bulkTag.Value.Item1})";
                    x = CalcTagSize(x, tagName);
                    UIStyles.DrawTag(tagName, bulkTag.Value.Item2, () =>
                    {
                        AssetInventory.RemoveAssetTag(_selectedTreeAssets, bulkTag.Key);
                        _requireAssetTreeRebuild = true;
                    }, UIStyles.TagStyle.Remove);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            // GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawReportingTab()
        {
            int assetUsageCount = _assetUsage?.Count ?? 0;
            int identifiedPackagesCount = _identifiedPackages?.Count ?? 0;

            EditorGUILayout.HelpBox("This view tries to identify used packages inside the current project. It will use guids. If package authors have shared files between projects this can result in false positives. The view is a preview under development.", MessageType.Info);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILabelWithText("Project files", assetUsageCount.ToString());
            if (assetUsageCount > 0)
            {
                GUILabelWithText("Identified files", identifiedPackagesCount + " (" + Mathf.RoundToInt((float) identifiedPackagesCount / assetUsageCount * 100f) + "%)");
            }
            else
            {
                GUILabelWithText("Identified files", "None");
            }

            if (_usedAssets != null && _usedAssets.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Identified Packages", EditorStyles.largeLabel);
                _usedAssetsScrollPos = GUILayout.BeginScrollView(_usedAssetsScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                foreach (string assetName in _usedAssets)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField(assetName);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Identify Used Packages", GUILayout.Height(50))) CalculateAssetUsage();
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Selection Details", "window", GUILayout.Width(UIStyles.InspectorWidth));
            EditorGUILayout.Space();

            if (_pvSelection != null && _pvSelection.Length > 0)
            {
                if (_pvSelection.Length > 1)
                {
                    EditorGUILayout.HelpBox("Multiple files are selected. This is not supported.", MessageType.Warning);
                }
            }
            if (string.IsNullOrEmpty(_pvSelectedPath))
            {
                EditorGUILayout.HelpBox("Select any file in the Unity Project View to identify what package it belongs to.", MessageType.Info);
            }
            else
            {
                GUILabelWithText("Folder", Path.GetDirectoryName(_pvSelectedPath));
                GUILabelWithText("Selection", Path.GetFileName(_pvSelectedPath));

                if (_pvSelectionChanged || _pvSelectedAssets == null)
                {
                    _pvSelectedAssets = AssetInventory.Guid2File(Selection.assetGUIDs[0]);
                    EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTextures(_pvSelectedAssets), this);
                }
                if (_pvSelectedAssets.Count == 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Could not identify package. Guid not found in local database.", MessageType.Info);
                }
                if (_pvSelectedAssets.Count > 1)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The file was matched with multiple packages. This can happen if identical files were contained in multiple packages.", MessageType.Info);
                }
                foreach (AssetInfo info in _pvSelectedAssets)
                {
                    EditorGUILayout.Space();
                    DrawPackageDetails(info, false, true, false);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawAboutTab()
        {
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("A tool by Impossible Robert", UIStyles.whiteCenter);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Developer: Robert Wetzold", UIStyles.whiteCenter);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("www.wetzold.com/tools", UIStyles.centerLinkLabel)) Application.OpenURL("https://www.wetzold.com/tools");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Version {AssetInventory.ToolVersion}", UIStyles.whiteCenter);
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("If you like this asset please consider leaving a review on the Unity Asset Store. Thanks a million!", MessageType.Info);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (_debugMode && GUILayout.Button("Get Token")) Debug.Log(CloudProjectSettings.accessToken);
            if (_debugMode && GUILayout.Button("Reload Lookups")) ReloadLookups();
        }

        private void CalculateAssetUsage()
        {
            _assetUsage = AssetInventory.CalculateAssetUsage();
            _usedAssets = _assetUsage.Select(ai => ai.DisplayName).Distinct().Where(a => !string.IsNullOrEmpty(a)).OrderBy(s => s).ToList();
            _identifiedPackages = _assetUsage.Where(ai => ai.CurrentState != Asset.State.Unknown).ToList();
        }

        private void StartInitialIndexing()
        {
            AssetInventory.RefreshIndex();

            // start also asset download if not already done before manually
            if (string.IsNullOrEmpty(AssetInventory.CurrentMain)) FetchAssetPurchases();
        }

        private void GUILabelWithText(string label, string text, int width = 85)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(width));
            EditorGUILayout.LabelField(UIStyles.Content(text, text), GUILayout.MaxWidth(UIStyles.InspectorWidth - width - 20), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        private void SetDatabaseLocation()
        {
            string targetFolder = EditorUtility.OpenFolderPanel("Select folder for database and cache", AssetInventory.GetStorageFolder(), "");
            if (string.IsNullOrEmpty(targetFolder)) return;

            // check if same folder selected
            if (IOUtils.IsSameDirectory(targetFolder, AssetInventory.GetStorageFolder())) return;

            // check for existing database
            if (File.Exists(Path.Combine(targetFolder, DBAdapter.DB_NAME)))
            {
                if (EditorUtility.DisplayDialog("Use Existing?", "The target folder contains a database. Switch to this one? Otherwise please select an empty directory.", "Switch", "Cancel"))
                {
                    AssetInventory.SwitchDatabase(targetFolder);
                    ReloadLookups();
                    PerformSearch();
                }

                return;
            }

            // target must be empty
            if (!IOUtils.IsDirectoryEmpty(targetFolder))
            {
                EditorUtility.DisplayDialog("Error", "The target folder needs to be empty or contain an existing database.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Keep Old Database", "Should a new database be created or the current one moved?", "New", "Move"))
            {
                AssetInventory.SwitchDatabase(targetFolder);
                ReloadLookups();
                PerformSearch();
                return;
            }

            _previewInProgress = true;
            AssetInventory.MoveDatabase(targetFolder);
            _previewInProgress = false;
        }

        private async void PlayAudio(AssetInfo assetInfo)
        {
            _previewInProgress = true;

            await AssetInventory.PlayAudio(assetInfo);

            _previewInProgress = false;
        }

        private void PingAsset(AssetInfo assetInfo)
        {
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetInfo.ProjectPath);
            if (Selection.activeObject == null) assetInfo.ProjectPath = null; // probably got deleted again
        }

        private async void CalculateDependencies(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            assetInfo.DependencyState = AssetInfo.DependencyStateOptions.Calculating;
            await AssetInventory.CalculateDependencies(_selectedEntry);
            if (assetInfo.DependencyState == AssetInfo.DependencyStateOptions.Calculating) assetInfo.DependencyState = AssetInfo.DependencyStateOptions.Done; // otherwise error along the way
            _previewInProgress = false;
        }

        private async void Open(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            string targetPath;
            if (assetInfo.InProject)
            {
                targetPath = assetInfo.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(assetInfo);
            }

            if (targetPath != null) EditorUtility.OpenWithDefaultApp(targetPath);
            _previewInProgress = false;
        }

        private async void OpenExplorer(AssetInfo assetInfo)
        {
            _previewInProgress = true;
            string targetPath;
            if (assetInfo.InProject)
            {
                targetPath = assetInfo.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(assetInfo);
            }

            if (targetPath != null) EditorUtility.RevealInFinder(targetPath);
            _previewInProgress = false;
        }

        private async void CopyTo(AssetInfo assetInfo, string targetFolder, bool withDependencies = false, bool withScripts = false)
        {
            _previewInProgress = true;

            string mainFile = await AssetInventory.CopyTo(assetInfo, targetFolder, withDependencies, withScripts);
            if (mainFile != null) PingAsset(new AssetInfo().WithProjectPath(mainFile));

            _previewInProgress = false;
        }

        private async void FetchAssetPurchases()
        {
            AssetPurchases result = await AssetInventory.FetchOnlineAssets();
            if (AssetStore.CancellationRequested || result == null) return;

            _purchasedAssets = result;
            _purchasedAssetsCount = _purchasedAssets?.total ?? 0;
            ReloadLookups();
            FetchAssetDetails();
        }

        private void CreateAssetTree()
        {
            _requireAssetTreeRebuild = false;
            List<AssetInfo> data = new List<AssetInfo>();
            AssetInfo root = new AssetInfo().WithTreeData("Root", depth: -1);
            data.Add(root);

            string[] lastCats = Array.Empty<string>();
            int catIdx = 0;
            switch (AssetInventory.Config.assetGrouping)
            {
                case 0: // none
                    _assets.OrderBy(a => a.GetDisplayName, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                        .ForEach(a => data.Add(a.WithTreeData(a.GetDisplayName, a.AssetId)));
                    break;

                case 2: // category
                    IOrderedEnumerable<AssetInfo> orderedAssets = _assets.OrderBy(a => a.GetDisplayCategory, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(a => a.GetDisplayName, StringComparer.OrdinalIgnoreCase);

                    string[] noCat = {"-no category-"};
                    foreach (AssetInfo info in orderedAssets)
                    {
                        // create hierarchy
                        string[] cats = string.IsNullOrEmpty(info.GetDisplayCategory) ? noCat : info.GetDisplayCategory.Split('/');

                        lastCats = AddCategorizedItem(cats, lastCats, data, info, ref catIdx);
                    }
                    break;

                case 3: // tags
                    List<Tag> tags = AssetInventory.LoadTags();
                    foreach (Tag tag in tags)
                    {
                        IOrderedEnumerable<AssetInfo> taggedAssets = _assets
                            .Where(a => a.AssetTags != null && a.AssetTags.Any(t => t.Name == tag.Name))
                            .OrderBy(a => a.GetDisplayName, StringComparer.OrdinalIgnoreCase);

                        string[] cats = {tag.Name};
                        foreach (AssetInfo info in taggedAssets)
                        {
                            // create hierarchy
                            lastCats = AddCategorizedItem(cats, lastCats, data, info, ref catIdx);
                        }
                    }

                    IOrderedEnumerable<AssetInfo> remainingAssets = _assets
                        .Where(a => a.AssetTags == null || a.AssetTags.Count == 0)
                        .OrderBy(a => a.GetDisplayName, StringComparer.OrdinalIgnoreCase);
                    string[] untaggedCat = {"-untagged-"};
                    foreach (AssetInfo info in remainingAssets)
                    {
                        lastCats = AddCategorizedItem(untaggedCat, lastCats, data, info, ref catIdx);
                    }
                    break;
            }

            AssetTreeModel.SetData(data, true);
            AssetTreeView.Reload();
            OnAssetTreeSelectionChanged(AssetTreeView.GetSelection());

            EditorCoroutineUtility.StartCoroutine(AssetUtils.LoadTextures(data), this);
        }

        private static string[] AddCategorizedItem(string[] cats, string[] lastCats, List<AssetInfo> data, AssetInfo info, ref int catIdx)
        {
            // find first difference to previous cat
            if (!ArrayUtility.ArrayEquals(cats, lastCats))
            {
                int firstDiff = 0;
                bool diffFound = false;
                for (int i = 0; i < Mathf.Min(cats.Length, lastCats.Length); i++)
                {
                    if (cats[i] != lastCats[i])
                    {
                        firstDiff = i;
                        diffFound = true;
                        break;
                    }
                }
                if (!diffFound) firstDiff = lastCats.Length;

                for (int i = firstDiff; i < cats.Length; i++)
                {
                    catIdx--;
                    AssetInfo catItem = new AssetInfo().WithTreeData(cats[i], catIdx, i);
                    data.Add(catItem);
                }
            }

            AssetInfo item = info.WithTreeData(info.GetDisplayName, info.AssetId, cats.Length);
            data.Add(item);

            return cats;
        }

        private void FetchAssetDetails()
        {
            AssetInventory.FetchAssetsDetails();
            ReloadLookups();
        }

        private void SetPage(int newPage)
        {
            newPage = Mathf.Clamp(newPage, 1, _pageCount);
            if (newPage != _curPage)
            {
                _curPage = newPage;
                if (_curPage > 0) PerformSearch(true);
            }
        }

        private async void UpdateStatistics()
        {
            if (_debugMode) Debug.LogWarning("Update Statistics");

            _assets = AssetInventory.LoadAssets();
            _tags = AssetInventory.LoadTags();
            _packageCount = _assets.Count;
            _indexedPackageCount = _assets.Count(a => a.FileCount > 0);
            _deprecatedAssetsCount = _assets.Count(a => a.OfficialState == "deprecated");
            _packageFileCount = DBAdapter.DB.Table<AssetFile>().Count();

            // only load slow statistics on Index tab
            if (_tab == 3)
            {
                _dbSize = DBAdapter.GetDBSize();
                _cacheSize = await AssetInventory.GetCacheFolderSize();
            }

            _requireAssetTreeRebuild = true;
        }

        private void PerformSearch(bool keepPage = false)
        {
            if (_debugMode) Debug.LogWarning("Perform Search");

            _requireSearchUpdate = false;
            int lastCount = _resultCount; // a bit of a heuristic but works great and is very performant
            string selectedSize = _resultSizes[AssetInventory.Config.maxResults];
            int.TryParse(selectedSize, out int maxResults);
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            string escape = "";
            string tagJoin = "";
            if (!string.IsNullOrWhiteSpace(_searchPhrase))
            {
                string phrase = _searchPhrase;

                // check for sqlite escaping requirements
                if (phrase.Contains("_"))
                {
                    phrase = phrase.Replace("_", "\\_");
                    escape = "ESCAPE '\\'";
                }

                wheres.Add($"AssetFile.Path like ? {escape}");
                args.Add($"%{phrase}%");
            }

            if (_selectedType > 0 && _types.Length > _selectedType)
            {
                string rawType = _types[_selectedType];
                string[] type = rawType.Split('/');
                if (type.Length > 1)
                {
                    wheres.Add("AssetFile.Type = ?");
                    args.Add(type.Last());
                }
                else if (AssetInventory.TypeGroups.ContainsKey(rawType))
                {
                    // sqlite does not support binding lists, parameters must be spelled out
                    List<string> paramCount = new List<string>();
                    foreach (string t in AssetInventory.TypeGroups[rawType])
                    {
                        paramCount.Add("?");
                        args.Add(t);
                    }

                    wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");
                }
            }

            // only add detail filters if section is open to not have confusing search results
            if (AssetInventory.Config.showFilterBar)
            {
                if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
                {
                    string[] arr = _publisherNames[_selectedPublisher].Split('/');
                    string publisher = arr[arr.Length - 1];
                    wheres.Add("Asset.SafePublisher = ?");
                    args.Add($"{publisher}");
                }

                if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
                {
                    string[] arr = _assetNames[_selectedAsset].Split('/');
                    string asset = arr[arr.Length - 1];
                    wheres.Add("Asset.SafeName = ?"); // TODO: going via In would be more efficient but not available at this point
                    args.Add($"{asset}");
                }

                if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
                {
                    string[] arr = _categoryNames[_selectedCategory].Split('/');
                    string category = arr[arr.Length - 1];
                    wheres.Add("Asset.SafeCategory = ?");
                    args.Add($"{category}");
                }

                if (!string.IsNullOrWhiteSpace(_searchWidth))
                {
                    if (int.TryParse(_searchWidth, out int width) && width > 0)
                    {
                        string widthComp = _checkMaxWidth ? "<=" : ">=";
                        wheres.Add($"AssetFile.Width > 0 and AssetFile.Width {widthComp} ?");
                        args.Add(width);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchHeight))
                {
                    if (int.TryParse(_searchHeight, out int height) && height > 0)
                    {
                        string heightComp = _checkMaxHeight ? "<=" : ">=";
                        wheres.Add($"AssetFile.Height > 0 and AssetFile.Height {heightComp} ?");
                        args.Add(height);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchLength))
                {
                    if (int.TryParse(_searchLength, out int length) && length > 0)
                    {
                        string lengthComp = _checkMaxLength ? "<=" : ">=";
                        wheres.Add($"AssetFile.Length > 0 and AssetFile.Length {lengthComp} ?");
                        args.Add(length);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchSize))
                {
                    if (int.TryParse(_searchSize, out int size) && size > 0)
                    {
                        string sizeComp = _checkMaxSize ? "<=" : ">=";
                        wheres.Add($"AssetFile.Size > 0 and AssetFile.Size {sizeComp} ?");
                        args.Add(size * 1024);
                    }
                }

                if (_selectedTag > 0 && _tagNames.Length > _selectedTag)
                {
                    string[] arr = _tagNames[_selectedTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("TagAssignment.TagId = ?");
                    args.Add(_tags.First(t => t.Name == tag).Id);

                    tagJoin = "inner join TagAssignment on Asset.Id = TagAssignment.TargetId";
                }
            }

            if (!string.IsNullOrWhiteSpace(AssetInventory.Config.excludedExtensions))
            {
                string[] extensions = AssetInventory.Config.excludedExtensions.Split(';');
                List<string> paramCount = new List<string>();
                foreach (string ext in extensions)
                {
                    paramCount.Add("?");
                    args.Add(ext.Trim());
                }

                wheres.Add("AssetFile.Type not in (" + string.Join(",", paramCount) + ")");
            }

            // ordering, can only be done on DB side since post-processing results would only work on the paged results which is incorrect
            string orderBy = "order by ";
            switch (AssetInventory.Config.sortField)
            {
                case 1:
                    orderBy += "AssetFile.FileName";
                    break;

                case 2:
                    orderBy += "AssetFile.Size";
                    break;

                case 3:
                    orderBy += "AssetFile.Type";
                    break;

                case 4:
                    orderBy += "AssetFile.Length";
                    break;

                case 5:
                    orderBy += "AssetFile.Width";
                    break;

                case 6:
                    orderBy += "AssetFile.Height";
                    break;

                case 7:
                    orderBy += "Asset.DisplayCategory";
                    break;

                case 8:
                    orderBy += "Asset.LastRelease";
                    break;

                case 9:
                    orderBy += "Asset.AssetRating";
                    break;

                case 10:
                    orderBy += "Asset.RatingCount";
                    break;

                default:
                    orderBy += "AssetFile.Path";
                    break;
            }

            orderBy += " COLLATE NOCASE";
            if (AssetInventory.Config.sortDescending) orderBy += " desc";
            orderBy += ", AssetFile.Path"; // always sort by path in case of equality of first level sorting

            wheres.Add("(Asset.Exclude=0 or Asset.Exclude is null)");

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            string baseQuery = $"from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId {tagJoin} {where}";
            string countQuery = $"select count(*) {baseQuery}";
            string dataQuery = $"select *, AssetFile.Id as Id {baseQuery} {orderBy}";
            if (maxResults > 0) dataQuery += $" limit {maxResults} offset {(_curPage - 1) * maxResults}";
            _resultCount = DBAdapter.DB.ExecuteScalar<int>($"{countQuery}", args.ToArray());
            _files = DBAdapter.DB.Query<AssetInfo>($"{dataQuery}", args.ToArray());

            // preview images
            if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
            _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this); // TODO: should be true once pages endless scrolling is in

            // pagination
            _contents = _files.Select(file =>
            {
                string text = "";
                switch (AssetInventory.Config.tileText)
                {
                    case 0:
                        text = file.ShortPath;
                        break;

                    case 1:
                        text = file.FileName;
                        break;

                    case 2:
                        text = Path.GetFileNameWithoutExtension(file.FileName);
                        break;
                }
                text = text == null ? "" : text.Replace('/', Path.DirectorySeparatorChar);

                return new GUIContent(text);
            }).ToArray();
            _pageCount = AssetUtils.GetPageCount(_resultCount, maxResults);
            if (!keepPage && lastCount != _resultCount) _curPage = 1;
            SetPage(_curPage);
        }

        private IEnumerator LoadTextures(bool firstPageOnly)
        {
            string previewFolder = AssetInventory.GetPreviewFolder();
            int idx = -1;
            IEnumerable<AssetInfo> files = _files.Take(firstPageOnly ? 20 * 8 : _files.Count);
            foreach (AssetInfo file in files)
            {
                idx++;
                if (string.IsNullOrEmpty(file.PreviewFile))
                {
                    if (!AssetInventory.Config.showIconsForMissingPreviews) continue;

                    // check if well-known extension
                    if (_staticPreviews.ContainsKey(file.Type))
                    {
                        _contents[idx].image = EditorGUIUtility.IconContent(_staticPreviews[file.Type]).image;
                    }
                    else
                    {
                        _contents[idx].image = EditorGUIUtility.IconContent("d_DefaultAsset Icon").image;
                    }
                    continue;
                }
                string previewFile = Path.Combine(previewFolder, file.PreviewFile);
                if (!File.Exists(previewFile)) continue;

                yield return AssetUtils.LoadTexture(previewFile, result =>
                {
                    if (_contents.Length > idx) _contents[idx].image = result;
                });
            }
        }

        private void OpenInSearch(AssetInfo info)
        {
            if (info.Id <= 0) return;

            if (info.Exclude)
            {
                if (!EditorUtility.DisplayDialog("Package is Excluded", "The package is currently excluded from the search. Should it be included again?", "Include Again", "Cancel"))
                {
                    return;
                }
                AssetInventory.SetAssetExclusion(info.AssetId, false);
                ReloadLookups();
            }
            ResetSearch(false);

            _tab = 0;
            _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a.EndsWith(info.SafeName)));
            _requireSearchUpdate = true;
            AssetInventory.Config.showFilterBar = true;
        }

        private void ResetSearch(bool filterBarOnly)
        {
            if (!filterBarOnly)
            {
                _searchPhrase = "";
                _selectedType = 0;
            }

            _selectedAsset = 0;
            _selectedTag = 0;
            _selectedPublisher = 0;
            _selectedCategory = 0;
            _searchHeight = "";
            _searchWidth = "";
            _searchLength = "";
            _searchSize = "";
        }

        private void OnAssetTreeSelectionChanged(IList<int> ids)
        {
            _selectedTreeAsset = null;
            _selectedTreeAssets = _selectedTreeAssets ?? new List<AssetInfo>();
            _selectedTreeAssets.Clear();

            if (ids.Count == 1 && ids[0] > 0)
            {
                _selectedTreeAsset = AssetTreeModel.Find(ids[0]);
            }
            foreach (int id in ids)
            {
                GatherTreeChildren(id, _selectedTreeAssets);
            }
            _bulkTags.Clear();
            _selectedTreeAssets.ForEach(info => info.AssetTags?.ForEach(t =>
            {
                if (!_bulkTags.ContainsKey(t.Name)) _bulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _bulkTags[t.Name] = new Tuple<int, Color>(_bulkTags[t.Name].Item1 + 1, _bulkTags[t.Name].Item2);
            }));
        }

        private void GatherTreeChildren(int id, List<AssetInfo> result)
        {
            AssetInfo info = AssetTreeModel.Find(id);
            if (info == null) return;

            if (info.HasChildren)
            {
                foreach (TreeElement subInfo in info.Children)
                {
                    GatherTreeChildren(subInfo.TreeId, result);
                }
            }
            else if (info.Id > 0)
            {
                if (result.All(existing => info.Id != existing.Id)) result.Add(info);
            }
        }

        private void OnAssetTreeDoubleClicked(int id)
        {
            if (id <= 0) return;

            AssetInfo info = AssetTreeModel.Find(id);
            OpenInSearch(info);
        }

        private void DrawFoldersHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Additional Folders to Index");
        }

        private void DrawFoldersListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            FolderSpec spec = AssetInventory.Config.folders[index];

            if (isFocused) _selectedFolderIndex = index;

            EditorGUI.BeginChangeCheck();
            spec.enabled = GUI.Toggle(new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight), spec.enabled, UIStyles.Content("", "Include folder when indexing"), UIStyles.toggleStyle);
            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

            GUI.Label(new Rect(rect.x + 20, rect.y, rect.width - 250, EditorGUIUtility.singleLineHeight), spec.location, UIStyles.entryStyle);
            GUI.Label(new Rect(rect.x + rect.width - 230, rect.y, 200, EditorGUIUtility.singleLineHeight), UIStyles.FolderTypes[spec.folderType] + (spec.folderType == 1 ? " (" + UIStyles.MediaTypes[spec.scanFor] + ")" : ""), UIStyles.entryStyle);
            if (GUI.Button(new Rect(rect.x + rect.width - 30, rect.y + 1, 30, 20), EditorGUIUtility.IconContent("Settings", "Show/Hide Settings Tab")))
            {
                FolderSettingsUI folderSettingsUI = new FolderSettingsUI();
                folderSettingsUI.Init(spec);
                PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), folderSettingsUI);
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}