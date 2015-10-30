using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace Gnaaar
{
    public static class SpellManager
    {
        public static Spell.Skillshot QMini { get; private set; }
        public static Spell.Active WMini { get; private set; }
        public static Spell.Skillshot EMini { get; private set; }
        public static Spell.Active RMini { get; private set; }

        public static Spell.Skillshot QMega { get; private set; }
        public static Spell.Skillshot WMega { get; private set; }
        public static Spell.Skillshot EMega { get; private set; }
        public static Spell.Targeted RMega { get; private set; }

        public static Spell.Skillshot Q
        {
            get { return Player.Instance.IsMiniGnar() ? QMini : QMega; }
        }
        public static Spell.SpellBase W
        {
            get { return Player.Instance.IsMiniGnar() ? (Spell.SpellBase) WMini : WMega; }
        }
        public static Spell.Skillshot E
        {
            get { return Player.Instance.IsMiniGnar() ? EMini : EMega; }
        }
        public static Spell.SpellBase R
        {
            get { return Player.Instance.IsMiniGnar() ? (Spell.SpellBase) RMini : RMega; }
        }

        public static Spell.SpellBase[] Spells
        {
            get { return new[] { Q, W, E, R }; }
        }

        public static Dictionary<Spell.SpellBase, Color> ColorTranslation { get; private set; }

        private static float _lastCastedStun;
        public static bool HasCastedStun
        {
            get { return Game.Time - _lastCastedStun < 0.25; }
        }

        static SpellManager()
        {
            // Initialize spells
            // Mini
            QMini = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 1200, 55);
            WMini = new Spell.Active(SpellSlot.W);
            EMini = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Circular, 500, int.MaxValue, 150);
            RMini = new Spell.Active(SpellSlot.R);
            // Mega
            QMega = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 1200, 80);
            WMega = new Spell.Skillshot(SpellSlot.W, 525, SkillShotType.Linear, 250, int.MaxValue, 80);
            EMega = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Circular, 500, int.MaxValue, 150);
            RMega = new Spell.Targeted(SpellSlot.R, 590);

            ColorTranslation = new Dictionary<Spell.SpellBase, Color>
            {
                { QMini, Color.IndianRed.ToArgb(150) },
                { EMini, Color.Azure.ToArgb(150) },
                { QMega, Color.IndianRed.ToArgb(150) },
                { WMega, Color.Azure.ToArgb(150) },
                { EMega, Color.IndianRed.ToArgb(150) },
                { RMega, Color.Azure.ToArgb(150) }
            };

            Spellbook.OnCastSpell += OnCastSpell;
        }

        public static void Initialize()
        {
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && Player.Instance.IsMegaGnar())
            {
                switch (args.Slot)
                {
                    case SpellSlot.W:
                    case SpellSlot.R:

                        _lastCastedStun = Game.Time;
                        break;
                }
            }
        }

        public static Spell.SpellBase GetSpellFromSlot(SpellSlot slot)
        {
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
        }

        public static bool IsMiniSpell(this Spell.SpellBase spell)
        {
            return
                spell.Equals(QMini) ||
                spell.Equals(WMini) ||
                spell.Equals(EMini) ||
                spell.Equals(RMini);
        }

        public static bool IsEnabled(this Spell.SpellBase spell, Orbwalker.ActiveModes mode)
        {
            switch (mode)
            {
                case Orbwalker.ActiveModes.Combo:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Player.Instance.IsMiniGnar() ? Config.Modes.Combo.UseQ : Config.Modes.Combo.UseQMega;
                        case SpellSlot.W:
                            return Player.Instance.IsMegaGnar() && Config.Modes.Combo.UseWMega;
                        case SpellSlot.E:
                            return Player.Instance.IsMiniGnar() ? Config.Modes.Combo.UseE : Config.Modes.Combo.UseEMega;
                        case SpellSlot.R:
                            return Player.Instance.IsMegaGnar() && Config.Modes.Combo.UseRMega;
                    }
                    break;
                case Orbwalker.ActiveModes.Harass:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Player.Instance.IsMiniGnar() ? Config.Modes.Harass.UseQ : Config.Modes.Harass.UseQMega;
                        case SpellSlot.W:
                            return Player.Instance.IsMegaGnar() && Config.Modes.Harass.UseWMega;
                    }
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Player.Instance.IsMiniGnar() ? Config.Modes.LaneClear.UseQ : Config.Modes.LaneClear.UseQMega;
                        case SpellSlot.W:
                            return Player.Instance.IsMegaGnar() && Config.Modes.LaneClear.UseWMega;
                        case SpellSlot.E:
                            return Player.Instance.IsMegaGnar() && Config.Modes.LaneClear.UseEMega;
                    }
                    break;
                case Orbwalker.ActiveModes.JungleClear:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Player.Instance.IsMiniGnar() ? Config.Modes.JungleClear.UseQ : Config.Modes.JungleClear.UseQMega;
                        case SpellSlot.W:
                            return Player.Instance.IsMegaGnar() && Config.Modes.JungleClear.UseWMega;
                        case SpellSlot.E:
                            return Player.Instance.IsMegaGnar() && Config.Modes.JungleClear.UseEMega;
                    }
                    break;
            }

            return false;
        }

        public static bool IsEnabledAndReady(this Spell.SpellBase spell, Orbwalker.ActiveModes mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }

        public static AIHeroClient GetTarget(this Spell.SpellBase spell, float extraRange = 0)
        {
            return TargetSelector.GetTarget(spell.Range + extraRange, DamageType.Physical);
        }

        public static EntityManager.MinionsAndMonsters.FarmLocation? GetFarmLocation(
            this Spell.SpellBase spell,
            EntityManager.UnitTeam team = EntityManager.UnitTeam.Enemy,
            EntityManager.MinionsAndMonsters.EntityType type = EntityManager.MinionsAndMonsters.EntityType.Minion,
            IEnumerable<Obj_AI_Minion> targets = null)
        {
            // Get minions if not set
            if (targets == null)
            {
                switch (type)
                {
                    case EntityManager.MinionsAndMonsters.EntityType.Minion:
                        targets = EntityManager.MinionsAndMonsters.GetLaneMinions(team, Player.Instance.ServerPosition, spell.Range, false);
                        break;
                    case EntityManager.MinionsAndMonsters.EntityType.Monster:
                        targets = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.ServerPosition, spell.Range, false);
                        break;
                    default:
                        targets = EntityManager.MinionsAndMonsters.CombinedAttackable.Where(o => o.IsInRange(Player.Instance, spell.Range));
                        break;
                }
            }
            var allTargets = targets.ToArray();

            // Validate
            var skillshot = spell as Spell.Skillshot;
            if (skillshot == null || allTargets.Length == 0)
            {
                return null;
            }

            // Get best location to shoot
            var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(allTargets, skillshot.Width, (int) spell.Range);
            if (farmLocation.HitNumber == 0)
            {
                return null;
            }
            return farmLocation;
        }

        private static Color ToArgb(this Color color, byte a)
        {
            return new ColorBGRA(color.R, color.G, color.B, a);
        }

        public static Color GetColor(this Spell.SpellBase spell)
        {
            return ColorTranslation.ContainsKey(spell) ? ColorTranslation[spell] : Color.Wheat;
        }
    }
}
