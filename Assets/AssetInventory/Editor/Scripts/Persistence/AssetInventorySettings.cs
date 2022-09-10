using System;
using System.Collections.Generic;

namespace AssetInventory
{
    [Serializable]
    public class AssetInventorySettings
    {
        public int sortField;
        public int assetGrouping;
        public bool sortDescending;
        public int maxResults = 5;
        public int tileText;
        public bool showFilterBar;
        public bool showDetailFilters = true;
        public bool showSavedSearches = true;
        public bool indexAssetStore = true;
        public bool autoPlayAudio = true;
        public bool pingSelected = true;
        public bool groupLists = true;
        public bool showIconsForMissingPreviews = true;
        public bool gatherExtendedMetadata = true;
        public bool extractPreviews = true;
        public bool indexPackageContents = true;
        public int tileSize = 150;
        public string customStorageLocation;
        public string excludedExtensions = "asset;json;txt;cs;md;uss;asmdef;ttf;uxml";
        public List<FolderSpec> folders = new List<FolderSpec>();
        public List<SavedSearch> searches = new List<SavedSearch>();
    }
}