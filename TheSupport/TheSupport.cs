using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Notifications;
using TheSupport.Champions;

namespace TheSupport
{
    public class TheSupport
    {
        public static Dictionary<Champion, Func<TheSupport, ChampionBase>> SupportedChampions = new Dictionary<Champion, Func<TheSupport, ChampionBase>>
        {
            //{ Champion.Alistar, support => new Alistar(support) },
            //{ Champion.Annie, support => new Annie(support) },
            //{ Champion.Bard, support => new Bard(support) },
            { Champion.Blitzcrank, support => new Blitzcrank(support) },
            //{ Champion.Brand, support => new Brand(support) },
            //{ Champion.Braum, support => new Braum(support) },
            //{ Champion.Janna, support => new Janna(support) },
            //{ Champion.Karma, support => new Karma(support) },
            //{ Champion.Leona, support => new Leona(support) },
            //{ Champion.Lulu, support => new Lulu(support) },
            //{ Champion.Morgana, support => new Morgana(support) },
            //{ Champion.Nami, support => new Nami(support) },
            //{ Champion.Sona, support => new Sona(support) },
            //{ Champion.Soraka, support => new Soraka(support) },
            //{ Champion.TahmKench, support => new TahmKench(support) },
            //{ Champion.Taric, support => new Taric(support) },
            //{ Champion.Thresh, support => new Thresh(support) },
            //{ Champion.Velkoz, support => new Velkoz(support) },
            //{ Champion.Zilean, support => new Zilean(support) },
            //{ Champion.Zyra, support => new Zyra(support) }
        };

        private static TheSupport _instance;
        public static TheSupport Instance
        {
            get { return _instance ?? (_instance = new TheSupport()); }
        }

        internal static void Main(string[] args)
        {
            // Wait for the game to fully load
            Loading.OnLoadingComplete += delegate { Instance.OnLoadingComplete(); };
        }

        public Menu Menu { get; private set; }

        public ModeHandler ModeHandler { get; private set; }
        public ChampionBase PlayedChampion { get; private set; }

        public bool FinishedLoading { get; private set; }

        private TheSupport()
        {
            // Do not allow the creation of another instance of this class
        }

        public void OnLoadingComplete()
        {
            // Do not allow multiple calling of this method
            if (FinishedLoading)
            {
                return;
            }
            FinishedLoading = true;

            // Check if this addon supports the current champion
            if (!SupportedChampions.ContainsKey(Player.Instance.Hero))
            {
                // Do not load further as we don't support the champion
                return;
            }

            // Initialize properties
            Menu = MainMenu.AddMenu("The Support", "leSupport" + Player.Instance.ChampionName, "The Support - " + Player.Instance.ChampionName);
            ModeHandler = new ModeHandler(this);

            // Create a new instance of the played champion
            PlayedChampion = SupportedChampions[Player.Instance.Hero](this);

            // Register spells for the champion
            PlayedChampion.RegisterSpells(ModeHandler);

            // Finalize champion init
            PlayedChampion.OnPostInit();

            // Listen to required events
            Game.OnTick += OnTick;

            // Notify successfull loading
            Notifications.Show(new SimpleNotification(
                "TheSupport by Hellsing",
                string.Format("TheSupport has successfully loaded the champion '{0}' for you! Have fun playing!", Player.Instance.ChampionName)),
                15000);
        }

        private void OnTick(EventArgs args)
        {
            // Trigger mode handler
            ModeHandler.OnTick();
        }
    }
}
