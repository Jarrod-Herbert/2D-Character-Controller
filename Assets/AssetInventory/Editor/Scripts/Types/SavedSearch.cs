using System;

namespace AssetInventory
{
    [Serializable]
    public class SavedSearch
    {
        public string name;
        public string color;
        public string searchPhrase;
        public string type;
        public string package;
        public string tag;
        public string publisher;
        public string category;
        public string width;
        public string height;
        public string length;
        public string size;
        public bool checkMaxWidth;
        public bool checkMaxHeight;
        public bool checkMaxLength;
        public bool checkMaxSize;
    }
}