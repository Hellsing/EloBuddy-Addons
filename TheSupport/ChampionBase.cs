using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace TheSupport
{
    public abstract class ChampionBase<TQ, TW, TE, TR> : ChampionBase
        where TQ : Spell.SpellBase
        where TW : Spell.SpellBase
        where TE : Spell.SpellBase
        where TR : Spell.SpellBase
    {
        protected TQ Q { get; set; }
        protected TW W { get; set; }
        protected TE E { get; set; }
        protected TR R { get; set; }

        protected HashSet<Spell.SpellBase> SpellRangeDrawings { get; set; }  

        protected ChampionBase(TheSupport support, TQ spellQ = null, TW spellW = null, TE spellE = null, TR spellR = null) : base(support)
        {
            // Initialize properties
            Q = spellQ;
            W = spellW;
            E = spellE;
            R = spellR;
            SpellRangeDrawings = new HashSet<Spell.SpellBase>();
        }

        public sealed override void OnPostInit()
        {
            // Register spell range drawings
            if (SpellRangeDrawings.Count > 0)
            {
                Drawing.OnDraw += OnSpellRangeDraw;
            }
        }

        private void OnSpellRangeDraw(EventArgs args)
        {
            foreach (var spell in SpellRangeDrawings.Where(o => o.Range < uint.MaxValue))
            {
                Circle.Draw(Color.GreenYellow, spell.Range, Player.Instance.Position);
            }
        }
    }

    public abstract class ChampionBase
    {
        public Menu Menu { get; private set; }
        protected TheSupport Support { get; private set; }

        protected ChampionBase(TheSupport support)
        {
            // Initialize properties
            Menu = support.Menu.AddSubMenu("Champion");
            Support = support;
        }

        public abstract void RegisterSpells(ModeHandler handler);

        public abstract void OnPostInit();
    }
}
