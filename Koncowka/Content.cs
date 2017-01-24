using System;
using System.Collections.Generic;


namespace Koncowka
{
    [Serializable]
    public class DataCampaign
    {
        public Data data { get; set; }
    }

    [Serializable]
    public class Data
    {
        public bool found { get; set; }
        public Campaign campaign { get; set; }
        public PlayList playlist { get; set; }
    }

    [Serializable]
    public class Campaign
    {
        public string name { get; set; }
        public string start { get; set; }
        public string end { get; set; }
    }

    [Serializable]
    public class PlayList
    {
        public string name { get; set; }
        public IList<Items> items { get; set; }
    }

    [Serializable]
    public class Items
    {
        public string type { get; set; }
        public int duration { get; set; }
        public string url { get; set; }
    }
}
