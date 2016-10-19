using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable UseStringInterpolation

namespace SkinChanger
{
    public static class SkinChanger
    {
        public static string DataVersion { get; private set; }
        private const string DataVersionUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
        private const string RequestFormat = "http://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion/{1}.json";

        public static string AirClientVersion { get; private set; }
        public static string AirClientPath { get; private set; }
        private static string AirClientChromaSwatchesPath { get; set; }
        private static readonly Dictionary<int, List<int>> Chromas = new Dictionary<int, List<int>>();

        private static readonly Dictionary<Champion, List<ChampionDataJson.Skin>> Skins = new Dictionary<Champion, List<ChampionDataJson.Skin>>();

        private static readonly Dictionary<int, int> DefaultSkins = new Dictionary<int, int>();

        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static Menu Menu { get; set; }
        private static Dictionary<int, Menu> HeroMenus { get; set; }

        public static void Main(string[] args)
        {
            // Get air client path
            AirClientPath = Path.Combine("..", "..", "..", "..", "..", "projects", "lol_air_client", "releases");
            if (Directory.Exists(AirClientPath))
            {
                // Get the most recent version
                AirClientVersion = Directory.GetDirectories(AirClientPath).Select(Path.GetFileName).Max(name => new System.Version(name)).ToString();

                // Create chroma swatches path
                AirClientChromaSwatchesPath = Path.Combine(AirClientPath, AirClientVersion, "deploy", "assets", "storeImages", "content", "svu", "chroma_swatches");
                if (Directory.Exists(AirClientChromaSwatchesPath))
                {
                    // Get all chroma swatches
                    var files = Directory.GetFiles(AirClientChromaSwatchesPath);

                    // Parse files into ids
                    var ids = new List<int>();
                    foreach (var fileName in files.Select(Path.GetFileNameWithoutExtension))
                    {
                        int id;
                        if (int.TryParse(fileName, out id))
                        {
                            ids.Add(id);
                        }
                    }

                    // Parse chromas
                    foreach (var id in ids.OrderBy(id => id))
                    {
                        var champId = id / 1000;
                        var skinId = id % 1000;

                        // Add chroma to list
                        if (!Chromas.ContainsKey(champId))
                        {
                            Chromas.Add(champId, new List<int>());
                        }
                        Chromas[champId].Add(skinId);
                    }
                }
                else
                {
                    Logger.Warn("[SkinChanger] Could not find air client path!\nValue: {0}", AirClientChromaSwatchesPath);
                }
            }
            else
            {
                Logger.Warn("[SkinChanger] Could not find air client path!\nValue: {0}", AirClientPath);
            }

            // Listen to loading complete event
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Get the default skins
            foreach (var hero in EntityManager.Heroes.AllHeroes)
            {
                DefaultSkins.Add(hero.NetworkId, hero.SkinId);
            }

            // Create a menu
            Menu = MainMenu.AddMenu("SkinChanger", "hellsingSkinChanger", "SkinChanger - by Hellsing");
            Menu.AddGroupLabel("Intro");
            Menu.AddLabel("Change the skins of any hero on the map!");
            Menu.AddLabel("Select a hero from one of the submenus and choose their new skin.");
            Menu.AddLabel("You can also reset the skins of the heroes by clicking reset below.");
            Menu.AddLabel("(Does not work after reloading/restarting the game)");
            Menu.AddSeparator();

            Menu.AddGroupLabel("General");
            Menu.Add("random", new CheckBox("Apply random skin for everyone", false)).OnValueChange += OnRandomSkinsPress;
            Menu.Add("reset", new CheckBox("Reset all to default skins", false)).OnValueChange += OnResetPress;

            // Add a submenu for each hero
            HeroMenus = new Dictionary<int, Menu>();
            foreach (var hero in new[] { Player.Instance }.Concat(EntityManager.Heroes.Allies.Where(o => !o.IsMe)) /*.Concat(EntityManager.Heroes.Enemies)*/)
            {
                var menuName = string.Format("{0} - {1}", hero.IsMe ? "Me" : hero.IsAlly ? "A" : "E", hero.ChampionName);
                var menu = Menu.AddSubMenu(menuName, menuName, string.Format("{0} - {1}", menuName, hero.Name));
                HeroMenus.Add(hero.NetworkId, menu);

                menu.AddGroupLabel("Info");
                menu.AddLabel("Below you can select between several skins for this champion.");
                menu.AddLabel("Skins marked with a [c] also have different chromas to select from.");
                menu.AddSeparator();

                menu.AddGroupLabel("Select a skin");
                menu.Add("none", new Label("No skins available, check debug console!"));
            }

            // Update child objects to the same skin as each hero
            Obj_AI_Base.OnBuffGain += delegate(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs eventArgs)
            {
                if (sender.IsAlly && sender.Type != GameObjectType.AIHeroClient && !sender.IsMinion() && !sender.IsWard())
                {
                    // Get the caster
                    var caster = eventArgs.Buff.Caster as AIHeroClient;
                    if (caster != null && caster.NetworkId != sender.NetworkId)
                    {
                        // Compare skins
                        if (sender.SkinId != caster.SkinId)
                        {
                            sender.SetSkinId(caster.SkinId);
                        }
                    }
                }
            };

            // Download ddragon version file
            using (var webClient = new WebClient())
            {
                webClient.DownloadStringCompleted += DownloadVersionStringCompleted;

                try
                {
                    // Download version file
                    webClient.DownloadStringAsync(new Uri(DataVersionUrl, UriKind.Absolute));
                }
                catch (Exception)
                {
                    Logger.Info("[SkinChanger] Failed to download ddragon version file.");
                }
            }
        }

        private static void OnResetPress(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (args.NewValue)
            {
                // Reset skins of all champs
                foreach (var hero in EntityManager.Heroes.AllHeroes.Where(hero => HeroMenus.ContainsKey(hero.NetworkId) && hero.SkinId != DefaultSkins[hero.NetworkId]))
                {
                    // Get the menu for the hero
                    var menu = HeroMenus[hero.NetworkId];

                    try
                    {
                        // Set the menu value for the hero
                        var skins = menu.Get<ComboBox>("skins");
                        skins.CurrentValue = DefaultSkins[hero.NetworkId];
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                // Reset CheckBox
                sender.CurrentValue = !args.NewValue;
            }
        }

        private static void OnRandomSkinsPress(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (args.NewValue)
            {
                // Apply random skin for each champ
                foreach (var menu in EntityManager.Heroes.AllHeroes.Where(hero => HeroMenus.ContainsKey(hero.NetworkId)).Select(hero => HeroMenus[hero.NetworkId]))
                {
                    try
                    {
                        // Set the menu value for the hero
                        var skins = menu.Get<ComboBox>("skins");

                        // Get a new unique random skin
                        int skin;
                        do
                        {
                            skin = Random.Next(skins.Overlay.Children.Count);
                        } while (skin == skins.CurrentValue);

                        // Apply random skin
                        skins.CurrentValue = skin;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                // Reset CheckBox
                sender.CurrentValue = !args.NewValue;
            }
        }

        private static void DownloadVersionStringCompleted(object sender, DownloadStringCompletedEventArgs args)
        {
            try
            {
                // Convert version string
                DataVersion = JsonConvert.DeserializeObject<string[]>(args.Result)[0];
            }
            catch (Exception e)
            {
                Logger.Exception("[SkinChanger] Failed to convert version string to array!\nVersion string: {0}", e, args.Result);
            }

            // Validate version
            if (string.IsNullOrWhiteSpace(DataVersion))
            {
                return;
            }

            Task.Run(async () =>
            {
                // Download data for each champion
                using (var webClient = new WebClient())
                {
                    webClient.DownloadStringCompleted += ChampionDataDownloaded;

                    foreach (var hero in EntityManager.Heroes.AllHeroes.Where(o => HeroMenus.ContainsKey(o.NetworkId)).Select(o => o.Hero).Unique())
                    {
                        while (webClient.IsBusy)
                        {
                            await Task.Delay(50);
                        }
                        webClient.DownloadStringAsync(new Uri(string.Format(RequestFormat, DataVersion, hero), UriKind.Absolute));
                    }
                }
            });
        }

        private static void ChampionDataDownloaded(object sender, DownloadStringCompletedEventArgs args)
        {
            // Check for invalid json result
            if (string.IsNullOrWhiteSpace(args.Result))
            {
                return;
            }

            // Convert json into an object
            var champData = JsonConvert.DeserializeObject<ChampionDataJson>(args.Result);

            // Get the champion
            var champion = (Champion) Enum.Parse(typeof (Champion), champData.data.Keys.First());

            // Synchronize further actions
            Core.DelayAction(() =>
            {
                // Get the skins
                var champId = champData.data[champion.ToString()].key;
                Skins[champion] = champData.data[champion.ToString()].skins;

                // Add chromas
                if (Chromas.ContainsKey(champId))
                {
                    // Get all chroma skins
                    var i = 0;
                    foreach (var skin in Skins[champion].Where(skin => skin.chromas))
                    {
                        // Apply chromas
                        skin.ChromaIds = Chromas[champId];

                        // Check for multiple chromas on same champ (Rito pls :money:)
                        if (i > 0)
                        {
                            if (i == 1)
                            {
                                // Print debug message
                                Logger.Info("[SkinChanger] Found multiple chromas on champion '{0}'!", champion.ToString());
                            }
                        }

                        // Increase index
                        i++;
                    }
                }

                // Add the skins for each champ to the menu
                foreach (var hero in EntityManager.Heroes.AllHeroes.Where(hero => hero.Hero == champion && HeroMenus.ContainsKey(hero.NetworkId)))
                {
                    // Get the menu for the hero
                    var menu = HeroMenus[hero.NetworkId];

                    // Remove no-skin notifier
                    menu.Remove("none");

                    // Add ComboBox containing all skins
                    menu.AddLabel("Please select the skin you want to see for that chamion!");
                    var comboBox = menu.Add("skins",
                        new ComboBox("Selected skin",
                            Skins[champion].Any(skin => skin.num == hero.SkinId)
                                ? Skins[champion].FindIndex(skin => skin.num == hero.SkinId)
                                : Skins[champion].Select(skin => skin.num).FirstOrDefault(skin => hero.SkinId - skin < 3),
                            Skins[champion].Select(o => (o.chromas ? "[c] " : "") + o.name).ToArray()));

                    // Add a blank line for possible chromas
                    menu.AddSeparator();

                    if (hero.IsMe)
                    {
                        // Apply the saved skin
                        Core.DelayAction(() => { hero.SetSkinId(comboBox.CurrentValue); }, 5000);
                    }
                    else
                    {
                        // Don't load saved skins from other champs
                        comboBox.CurrentValue = hero.SkinId;
                    }

                    // Trigger a fake change
                    OnSkinChange(hero, comboBox.CurrentValue);

                    // Handle value changes
                    comboBox.OnValueChange += (s, a) => OnSkinChange(hero, a.NewValue, s);
                }
            }, 0);
        }

        private static void OnSkinChange(AIHeroClient hero, int skinId, ValueBase<int> sender = null)
        {
            Core.DelayAction(() =>
            {
                // Get real skin id
                skinId = Skins[hero.Hero][skinId].num;

                // Get the menu for the hero
                var menu = HeroMenus[hero.NetworkId];

                // Remove any chroma entry
                menu.Remove("chromaLabel");
                menu.Remove("chromas");

                // Check if the skin is a chroma
                var chroma = Skins[hero.Hero].FirstOrDefault(skin => skin.chromas && skin.num == skinId);
                if (chroma != null)
                {
                    // Create combo box values
                    var values = new List<string> { "No chroma" };
                    if (chroma.ChromaIds != null)
                    {
                        for (var i = 0; i < chroma.ChromaIds.Count; i++)
                        {
                            values.Add("Variation " + (i + 1));
                        }
                    }

                    // Create a new chromas ComboBox
                    menu.Add("chromaLabel", new Label("This skin also has several chromas, go try them out!"));
                    var chromas = menu.Add("chromas", new ComboBox("Selected chroma", values));

                    // Don't save chroma for any other champ than ourself
                    if (!hero.IsMe || hero.SkinId != 0)
                    {
                        chromas.CurrentValue = hero.SkinId;
                    }

                    // Create executor
                    var applyChroma = new Action(() =>
                    {
                        // Apply chroma
                        if (chromas.CurrentValue > 0)
                        {
                            // Set chroma variation
                            SetSkin(hero, chroma.ChromaIds[chromas.CurrentValue - 1]);
                        }
                        else if (hero.SkinId != skinId)
                        {
                            // Set regular skin
                            SetSkin(hero, skinId);
                        }
                    });

                    // Apply chroma
                    applyChroma();

                    // Listen to chroma changes
                    chromas.OnValueChange += delegate { applyChroma(); };

                    return;
                }

                // Apply skin change
                if (hero.SkinId != skinId)
                {
                    SetSkin(hero, skinId);
                }
            }, 0);
        }

        private static void SetSkin(Obj_AI_Base target, int id)
        {
            // Apply the skin
            target.SetSkinId(id);

            // Check if the target is a hero
            var hero = target as AIHeroClient;
            if (hero != null)
            {
                // Update all child objects
                foreach (
                    var obj in
                        ObjectManager.Get<Obj_AI_Base>()
                            .Where(
                                o =>
                                    o.NetworkId != hero.NetworkId && o.Type != GameObjectType.AIHeroClient && !o.IsMinion() && !o.IsWard() && o.Buffs.Any(b => b.Caster.NetworkId == hero.NetworkId) &&
                                    o.SkinId != hero.SkinId))
                {
                    // Apply same skin
                    obj.SetSkinId(hero.SkinId);
                }
            }
        }
    }
}
