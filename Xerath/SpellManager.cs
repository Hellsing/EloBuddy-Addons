using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace Xerath
{
    public static class SpellManager
    {
        public delegate void TapKeyPressedEventHandler(object sender, EventArgs args);

        public static event TapKeyPressedEventHandler OnTapKeyPressed;

        public static Spell.Chargeable Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Skillshot R { get; private set; }

        public static Spell.SpellBase[] Spells { get; private set; }
        public static Dictionary<SpellSlot, Color> ColorTranslation { get; private set; }

        public static bool IsCastingUlt
        {
            get { return Player.Instance.Buffs.Any(b => b.Caster.IsMe && b.IsValid() && b.DisplayName == "XerathR"); }
        }
        public static int LastChargeTime { get; private set; }
        public static Vector3 LastChargePosition { get; private set; }
        public static int ChargesRemaining { get; private set; }

        public static bool TapKeyPressed { get; private set; }

        public static void Initialize()
        {
            // Initialize spells
            Q = new Spell.Chargeable(SpellSlot.Q, 750, 1500, 1500, 500, int.MaxValue, 100);
            W = new Spell.Skillshot(SpellSlot.W, 1100, SkillShotType.Circular, 250, int.MaxValue, 100);
            E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 1600, 70);
            R = new Spell.Skillshot(SpellSlot.R, 3200, SkillShotType.Circular, 500, int.MaxValue, 120);

            // Finetune spells
            Q.AllowedCollisionCount = int.MaxValue;
            W.AllowedCollisionCount = int.MaxValue;
            R.AllowedCollisionCount = int.MaxValue;

            Spells = (new Spell.SpellBase[] { Q, W, E, R }).OrderByDescending(o => o.Range).ToArray();
            ColorTranslation = new Dictionary<SpellSlot, Color>
            {
                { SpellSlot.Q, Color.IndianRed.ToArgb(150) },
                { SpellSlot.W, Color.PaleVioletRed.ToArgb(150) },
                { SpellSlot.E, Color.IndianRed.ToArgb(150) },
                { SpellSlot.R, Color.DarkRed.ToArgb(150) }
            };

            // Setup ult management
            Game.OnTick += OnTick;
            Messages.RegisterEventHandler<Messages.KeyUp>(OnKeyUp);
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static float _previousLevel;

        private static void OnTick(EventArgs args)
        {
            // Adjust R range
            if (_previousLevel < R.Level)
            {
                R.Range = Convert.ToUInt32(2000 + 1200 * R.Level);
                _previousLevel = R.Level;
            }
        }

        private static void OnKeyUp(Messages.KeyUp args)
        {
            if (IsCastingUlt && (Config.Ultimate.ShootKey.Keys.Item1 == args.Key || Config.Ultimate.ShootKey.Keys.Item2 == args.Key))
            {
                // Only handle the tap key if the mode is set to tap key
                switch (Config.Ultimate.CurrentMode)
                {
                    // Auto
                    case 3:
                    // Near mouse
                    case 4:

                        // Tap key has been pressed
                        TapKeyPressed = true;
                        if (OnTapKeyPressed != null)
                        {
                            OnTapKeyPressed(null, EventArgs.Empty);
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.SData.Name)
                {
                    // Ult activation
                    case "XerathLocusOfPower2":
                        LastChargePosition = Vector3.Zero;
                        LastChargeTime = 0;
                        ChargesRemaining = 3;
                        TapKeyPressed = false;
                        break;
                    // Ult charge usage
                    case "xerathlocuspulse":
                        LastChargePosition = args.End;
                        LastChargeTime = Environment.TickCount;
                        ChargesRemaining--;
                        TapKeyPressed = false;
                        break;
                }
            }
        }

        public static bool IsEnabled(this Spell.SpellBase spell, Orbwalker.ActiveModes mode)
        {
            switch (mode)
            {
                case Orbwalker.ActiveModes.Combo:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Config.Modes.Combo.UseQ;
                        case SpellSlot.W:
                            return Config.Modes.Combo.UseW;
                        case SpellSlot.E:
                            return Config.Modes.Combo.UseE;
                        case SpellSlot.R:
                            return Config.Modes.Combo.UseR;
                    }
                    break;
                case Orbwalker.ActiveModes.Harass:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Config.Modes.Harass.UseQ;
                        case SpellSlot.W:
                            return Config.Modes.Harass.UseW;
                        case SpellSlot.E:
                            return Config.Modes.Harass.UseE;
                    }
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Config.Modes.LaneClear.UseQ;
                        case SpellSlot.W:
                            return Config.Modes.LaneClear.UseW;
                    }
                    break;
                case Orbwalker.ActiveModes.JungleClear:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Config.Modes.JungleClear.UseQ;
                        case SpellSlot.W:
                            return Config.Modes.JungleClear.UseW;
                        case SpellSlot.E:
                            return Config.Modes.JungleClear.UseE;
                    }
                    break;
            }

            return false;
        }

        public static bool IsEnabledAndReady(this Spell.SpellBase spell, Orbwalker.ActiveModes mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }

        public static AIHeroClient GetTarget(this Spell.SpellBase spell, params AIHeroClient[] excludeTargets)
        {
            var targets = EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget() && !excludeTargets.Contains(o) && spell.IsInRange(o)).ToArray();
            return TargetSelector.GetTarget(targets, DamageType.Magical);
        }

        public static bool CastOnBestTarget(this Spell.SpellBase spell)
        {
            var target = spell.GetTarget();
            return target != null && spell.Cast(target);
        }

        private static Color ToArgb(this Color color, byte a)
        {
            return new ColorBGRA(color.R, color.G, color.B, a);
        }

        public static Color GetColor(this Spell.SpellBase spell)
        {
            return ColorTranslation.ContainsKey(spell.Slot) ? ColorTranslation[spell.Slot] : Color.Wheat;
        }
    }
}
