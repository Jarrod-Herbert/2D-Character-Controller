using System;

namespace AssetInventory
{
    [Serializable]
    public class UploadInfo
    {
        public string assetCount;
        public string downloadSize;
        public string versionNumber;
        public string[] srps;
        public string[] dependencies;

        public override string ToString()
        {
            return $"Upload Info";
        }
    }
}