using System.Collections.Generic;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using Karthus.Modes;

namespace Karthus
{
    public class ModeHandler
    {
        private Karthus Instance { get; set; }
        public List<ModeBase> Modes { get; private set; }

        public ModeHandler(Karthus instance)
        {
            // Initialize properties
            Instance = instance;
            Modes = new List<ModeBase>
            {
                new PermaActive(instance),
                new Combo(instance),
                new Harass(instance),
                //new LaneClear(instance) // TODO: Enable
            };
        }

        public void OnTick()
        {
            // Execute all modes
            var activeModes = Orbwalker.ActiveModesFlags;
            foreach (var mode in Modes)
            {
                if (mode.ShouldBeExecuted(activeModes) && mode.Execute())
                {
                    break;
                }
            }
        }
    }

    public abstract class ModeBase
    {
        private Menu _menu;
        public Menu Menu
        {
            get { return _menu ?? (_menu = Instance.Menu.AddSubMenu(GetType().Name)); }
        }
        protected Karthus Instance { get; private set; }

        protected Spell.Skillshot Q
        {
            get { return Instance.SpellHandler.Q; }
        }
        protected Spell.Skillshot W
        {
            get { return Instance.SpellHandler.W; }
        }
        protected Spell.Active E
        {
            get { return Instance.SpellHandler.E; }
        }
        protected Spell.Active R
        {
            get { return Instance.SpellHandler.R; }
        }

        protected ModeBase(Karthus instance)
        {
            // Initialize properties
            Instance = instance;
        }

        public abstract bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes);

        public abstract bool Execute();
    }
}
