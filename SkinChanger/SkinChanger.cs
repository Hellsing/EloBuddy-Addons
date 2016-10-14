using System;
using System.Collections.Generic;
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
        public static string Version { get; private set; }

        private const string VersionUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
        private const string RequestFormat = "http://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion/{1}.json";

        private static readonly Dictionary<Champion, List<ChampionDataJson.Skin>> Skins = new Dictionary<Champion, List<ChampionDataJson.Skin>>();

        private static readonly Dictionary<int, int> DefaultSkins = new Dictionary<int, int>();

        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static Menu Menu { get; set; }
        private static Dictionary<int, Menu> HeroMenus { get; set; }

        public static void Main(string[] args)
        {
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
            foreach (var hero in new[] { Player.Instance }.Concat(EntityManager.Heroes.Allies.Where(o => !o.IsMe))/*.Concat(EntityManager.Heroes.Enemies)*/)
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

            // Initialize skin data download
            using (var webClient = new WebClient())
            {
                webClient.DownloadStringCompleted += DownloadVersionStringCompleted;

                try
                {
                    // Download version file
                    webClient.DownloadStringAsync(new Uri(VersionUrl, UriKind.Absolute));
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
                Version = JsonConvert.DeserializeObject<string[]>(args.Result)[0];
            }
            catch (Exception e)
            {
                Logger.Exception("[SkinChanger] Failed to convert version string to array!\nVersion string: {0}", e, args.Result);
            }

            // Validate version
            if (string.IsNullOrWhiteSpace(Version))
            {
                return;
            }

            Task.Run(async () =>
            {
                // Download data for each champion
                using (var webClient = new WebClient())
                {
                    webClient.DownloadStringCompleted += ChampionDataDownloaded;

                    foreach (var hero in EntityManager.Heroes.AllHeroes.Select(o => o.Hero).Unique())
                    {
                        while (webClient.IsBusy)
                        {
                            await Task.Delay(50);
                        }
                        webClient.DownloadStringAsync(new Uri(string.Format(RequestFormat, Version, hero), UriKind.Absolute));
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
                Skins[champion] = champData.data[champion.ToString()].skins;

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
            // Get real skin id
            skinId = Skins[hero.Hero][skinId].num;

            // Get the menu for the hero
            var menu = HeroMenus[hero.NetworkId];

            // Remove any chroma entry
            Core.DelayAction(() =>
            {
                menu.Remove("chromaLabel");
                menu.Remove("chromas");
            }, 0);

            // Check if the skin is a chroma
            if (Skins[hero.Hero].Any(skin => skin.chromas && skin.num == skinId))
            {
                Core.DelayAction(() =>
                {
                    // Create a new chromas ComboBox
                    menu.Add("chromaLabel", new Label("This skin also has several chromas, go try them out!"));
                    var chromas = menu.Add("chromas", new ComboBox("Selected chroma", new[] { "No chroma", "Variation 1", "Variation 2", "Variation 3" }));

                    // Don't save chroma for any other champ than ourself
                    if (!hero.IsMe)
                    {
                        chromas.CurrentValue = 0;
                    }

                    // Create executor
                    var applyChroma = new Action(() =>
                    {
                        // Apply chroma
                        if (hero.SkinId != skinId + chromas.CurrentValue)
                        {
                            hero.SetSkinId(skinId + chromas.CurrentValue);
                        }
                    });

                    // Apply chroma
                    applyChroma();

                    // Listen to chroma changes
                    chromas.OnValueChange += delegate { applyChroma(); };
                }, 0);

                return;
            }

            // Apply skin change
            if (hero.SkinId != skinId)
            {
                hero.SetSkinId(skinId);
            }
        }
    }
}
