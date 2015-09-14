using EloBuddy;
using EloBuddy.SDK;

namespace Hellsing.Kalista.Modes
{
    public abstract class ModeBase
    {
        protected static readonly AIHeroClient Player = EloBuddy.Player.Instance;

        protected Spell.Skillshot Q { get { return SpellManager.Q; } }
        protected Spell.Targeted W { get { return SpellManager.W; } }
        protected Spell.Active E { get { return SpellManager.E; } }
        protected Spell.Active R { get { return SpellManager.R; } }

        public abstract bool ShouldBeExecuted();
        public abstract void Execute();
    }
}
