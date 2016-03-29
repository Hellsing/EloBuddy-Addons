using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using Karthus.Modes;

namespace Karthus
{
    public sealed class ModeHandler
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
                new LaneClear(instance),
                new JungleFarming(instance)
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

            // Check if Defile should be turned off
            if (ModeBase.ShouldTurnOffDefile)
            {
                Instance.SpellHandler.E.Cast();
                ModeBase.ShouldTurnOffDefile = false;
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

        protected bool IsDefileActive
        {
            get { return Instance.SpellHandler.IsDefileActive(); }
        }

        public static bool ShouldTurnOffDefile { get; set; }

        protected ModeBase(Karthus instance)
        {
            // Initialize properties
            Instance = instance;
        }

        public abstract bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes);

        public abstract bool Execute();

        protected bool CastDefilePulse()
        {
            if (E.IsReady() && !IsDefileActive)
            {
                E.OnSpellCasted += DeactivateAfterSpellCast;
                if (!E.Cast())
                {
                    E.OnSpellCasted -= DeactivateAfterSpellCast;
                    return false;
                }
                return true;
            }
            return false;
        }

        private void DeactivateAfterSpellCast(Spell.SpellBase spell, GameObjectProcessSpellCastEventArgs args)
        {
            E.OnSpellCasted -= DeactivateAfterSpellCast;
            Game.OnTick += DeactivateDefile;
        }

        private void DeactivateDefile(EventArgs args)
        {
            // Check if Defile is ready and active
            if (E.IsReady() && IsDefileActive)
            {
                // Recast E and remove the tick listener
                Game.OnTick -= DeactivateDefile;
                E.Cast();
            }
        }
    }
}
