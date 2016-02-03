using System;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;

namespace MasterMind
{
    public static class MasterMind
    {
        public static readonly TextureLoader TextureLoader = new TextureLoader();

        public static bool IsSpectatorMode { get; private set; }

        public static Menu Menu { get; private set; }

        private static readonly IComponent[] Components =
        {
            new CooldownTracker()
        };

        public static void Main(string[] args)
        {
            // Load the addon in a real match and when spectating games
            Loading.OnLoadingComplete += OnLoadingComplete;
            Loading.OnLoadingCompleteSpectatorMode += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Initialize menu
            Menu = MainMenu.AddMenu("MasterMind", "MasterMind", "MasterMind - Improve Yourself!");

            Menu.AddGroupLabel("Welcome to MasterMind, your solution for quality game assistance.");
            Menu.AddLabel("This addon offers some neat features which will improve your gameplay");
            Menu.AddLabel("without dropping FPS or gameplay fun.");
            Menu.AddSeparator();
            Menu.AddLabel("Take a look at the various sub menus this addon has to offer, have fun!");

            // Initialize properties
            IsSpectatorMode = Bootstrap.IsSpectatorMode;

            // Initialize components
            foreach (var component in Components.Where(component => component.ShouldLoad(IsSpectatorMode)))
            {
                component.InitializeComponent();
            }
        }
    }
}
