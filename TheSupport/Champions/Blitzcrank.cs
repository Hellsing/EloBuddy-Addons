using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Modes = EloBuddy.SDK.Orbwalker.ActiveModes;

namespace TheSupport.Champions
{
    public class Blitzcrank : ChampionBase<Spell.Skillshot, Spell.Active, Spell.Active, Spell.Active>
    {
        public Blitzcrank(TheSupport support) : base(support)
        {
        }

        public override void RegisterSpells(ModeHandler handler)
        {
            // Create the spells
            Q = new Spell.Skillshot(SpellSlot.Q, 925, SkillShotType.Linear, 250, 1800, 70)
            {
                MinimumHitChance = HitChance.High
            };
            W = new Spell.Active(SpellSlot.W, (uint) Player.Instance.GetAutoAttackRange());
            E = new Spell.Active(SpellSlot.E, (uint) Player.Instance.GetAutoAttackRange());
            R = new Spell.Active(SpellSlot.R, 600);

            // Enable spell range drawing
            SpellRangeDrawings.Add(Q);

            // Set modes which should respect mana usage
            handler.SetManaModes(Modes.Harass | Modes.LaneClear, 40);

            // Register always active spell usages
            handler.RegisterSpellUsage(R, Modes.None, DamageType.Magical, () => CheckKillsteal(R.Slot), customName: "Killsteal with R", checkTarget: false);

            // Q in Combo with the overriden hitchance high
            handler.RegisterSpellUsage(Q,Modes.Combo, DamageType.Magical, heroCondition: target => !target.HasBuffOfType(BuffType.SpellShield), hitChance: HitChance.High);
            // Q in JungleClear and Harass
            handler.RegisterSpellUsage(Q, Modes.JungleClear | Modes.Harass, DamageType.Magical, heroCondition: target => !target.HasBuffOfType(BuffType.SpellShield));
            // W in Flee
            handler.RegisterSpellUsage(W, Modes.Flee, checkTarget: false);
            // E in Combo, JungleClear and Harass, casting only when target is in auto attack range
            handler.RegisterSpellUsage(E,
                Modes.Combo | Modes.JungleClear | Modes.Harass,
                DamageType.Magical,
                heroCondition: target => Player.Instance.IsInAutoAttackRange(target));
            // R in Combo, casting only when target is killable
            handler.RegisterSpellUsage(R,
                Modes.Combo,
                DamageType.Magical,
                heroCondition: target => Player.Instance.GetSpellDamage(target, R.Slot) > target.TotalShieldHealth());

            // Finalize by automatically creating the sub menus with the registered modes
            handler.CreateModeMenus();
        }

        private bool CheckKillsteal(SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.R:
                    return EntityManager.Heroes.Enemies.Any(o => R.IsInRange(o) && o.IsValidTarget() && Player.Instance.GetSpellDamage(o, R.Slot) > o.TotalShieldHealth());
            }
            return false;
        }
    }
}
