using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        // From one of my banned accounts ofc Kappa
        private const string ApiKey = "RGAPI-ccda7e3e-24cc-4348-969a-412c41d00c5c";

        private static readonly string RequestUrl = string.Format("https://global.api.pvp.net/api/lol/static-data/euw/v1.2/champion?champData=skins&api_key={0}", ApiKey);

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
            foreach (var hero in new[] { Player.Instance }.Concat(EntityManager.Heroes.Allies.Where(o => !o.IsMe)).Concat(EntityManager.Heroes.Enemies))
            {
                var menuName = string.Format("{0} - {1}", hero.IsMe ? "Me" : hero.IsAlly ? "A" : "E", hero.ChampionName);
                HeroMenus.Add(hero.NetworkId, Menu.AddSubMenu(menuName, menuName, string.Format("{0} - {1}", menuName, hero.Name)));
                HeroMenus[hero.NetworkId].AddGroupLabel("Select a skin");
                HeroMenus[hero.NetworkId].Add("none", new Label("No skins available, check debug console!"));
            }

            // Listen to game tick event
            Core.DelayAction(() =>
            {
                Game.OnTick += OnTick;
            }, 5000);

            // Initialize skin data download
            var WebClient = new WebClient();
            WebClient.DownloadStringCompleted += DownloadSkinDataCompleted;

            try
            {
                // Download the version from Rito
                WebClient.DownloadStringAsync(new Uri(RequestUrl, UriKind.Absolute));
            }
            catch (Exception)
            {
                Logger.Info("[SkinChanger] Failed to download skin data.");
            }
        }

        private static void OnTick(EventArgs args)
        {
            // Validate all skins
            foreach (var hero in EntityManager.Heroes.AllHeroes.Where(hero => hero.IsHPBarRendered))
            {
                // Get the menu for the hero
                var menu = HeroMenus[hero.NetworkId];

                try
                {
                    // Set the menu value for the hero
                    var skins = menu.Get<ComboBox>("skins");
                    if (hero.SkinId != skins.CurrentValue)
                    {
                        // Re-apply glitched skin
                        hero.SetSkinId(skins.CurrentValue);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private static void OnResetPress(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            if (args.NewValue)
            {
                // Reset skins of all champs
                foreach (var hero in EntityManager.Heroes.AllHeroes.Where(hero => hero.SkinId != DefaultSkins[hero.NetworkId]))
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
                foreach (var menu in EntityManager.Heroes.AllHeroes.Select(hero => HeroMenus[hero.NetworkId]))
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

        private static void DownloadSkinDataCompleted(object sender, DownloadStringCompletedEventArgs args)
        {
            // Convert the json data to an object
            var champData = JsonConvert.DeserializeObject<ChampionStaticData>(args.Result);

            // Add the skins for each champ to the menu
            foreach (var hero in EntityManager.Heroes.AllHeroes)
            {
                var skins = champData.GetSkinData(hero.Hero);
                if (skins.Count > 0)
                {
                    // Get the menu for the hero
                    var menu = HeroMenus[hero.NetworkId];

                    // Remove no-skin notifier
                    menu.Remove("none");

                    // Order skins
                    skins = skins.OrderBy(o => o.id).ToList();

                    // Add ComboBox containing all skins
                    menu.AddLabel("Please select the skin you want to see for that chamion!");
                    var comboBox = menu.Add("skins", new ComboBox("Selected skin", hero.SkinId, skins.Select(o => o.name).ToArray()));

                    if (hero.IsMe)
                    {
                        // Apply the saved skin
                        Core.DelayAction(() =>
                        {
                            hero.SetSkinId(comboBox.CurrentValue);
                        }, 5000);
                    }

                    // Handle value changes
                    comboBox.OnValueChange += delegate(ValueBase<int> box, ValueBase<int>.ValueChangeArgs changeArgs)
                    {
                        // Apply skin change
                        hero.SetSkinId(changeArgs.NewValue);
                    };
                }
            }
        }

        public class ChampionStaticData
        {
            public Dictionary<string, ChampionDto> data { get; set; }

            public List<SkinDto> GetSkinData(Champion champion)
            {
                try
                {
                    return data[champion.ToString()].skins;
                }
                catch (Exception)
                {
                    Logger.Warn("[SkinChanger] Data did not contain champion '{0}'!", champion);
                    return new List<SkinDto>();
                }
            }
        }

        public class ChampionDto
        {
            public int id { get; set; }
            public string title { get; set; }
            public string name { get; set; }
            public List<SkinDto> skins { get; set; }
        }

        public class SkinDto
        {
            public int id { get; set; }
            public string name { get; set; }
            public int num { get; set; }
        }
    }
}
