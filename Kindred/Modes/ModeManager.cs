using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Utils;

namespace Kindred.Modes
{
    public abstract class ModeBase
    {
        public static AIHeroClient Player
        {
            get { return EloBuddy.Player.Instance; }
        }

        public static Spell.Targeted Q
        {
            get { return SpellManager.Q; }
        }
        public static Spell.Active W
        {
            get { return SpellManager.W; }
        }
        public static Spell.Targeted E
        {
            get { return SpellManager.E; }
        }
        public static Spell.Targeted R
        {
            get { return SpellManager.R; }
        }

        public abstract bool ShouldBeExecuted();

        public abstract void Execute();
    }

    public static class ModeManager
    {
        private static readonly List<ModeBase> AvailableModes = new List<ModeBase>();

        static ModeManager()
        {
            // Add all modes
            AvailableModes.AddRange(new ModeBase[]
            {
                new PermaActive(),
                new Combo(),
                new Harass(),
                new LaneClear(),
                new JungleClear(),
                new LastHit(),
                new Flee()
            });

            // Listen to required events
            Game.OnTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
            AvailableModes.ForEach(mode =>
            {
                try
                {
                    if (mode.ShouldBeExecuted())
                    {
                        mode.Execute();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("There was an error executing mode {0}!\n{1}", mode.GetType().Name, e);
                }
            });
        }

        public static void Initialize()
        {
        }
    }
}
