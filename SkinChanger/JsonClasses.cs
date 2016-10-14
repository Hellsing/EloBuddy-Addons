using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace SkinChanger
{
    public class ChampionDataJson
    {
        public Dictionary<string, Champion> data { get; set; }

        public class Champion
        {
            public List<Skin> skins { get; set; }
        }

        public class Skin
        {
            public string id { get; set; }
            public int num { get; set; }
            public string name { get; set; }
            public bool chromas { get; set; }
        }
    }
}
