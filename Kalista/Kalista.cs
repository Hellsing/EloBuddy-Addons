using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;

namespace Hellsing.Kalista
{
    public class Kalista
    {
        public static bool IsAfterAttack { get; private set; }
        public static AttackableUnit AfterAttackTarget { get; private set; }

        static Kalista()
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args) { Logger.Log(LogLevel.Error, args.ExceptionObject.ToString()); };
        }

        public static void Main(string[] args)
        {
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Validate champion
            if (Player.Instance.ChampionName != "Kalista")
            {
                return;
            }

            // Initialize classes
            Config.Initialize();
            SoulBoundSaver.Initialize();
            ModeLogic.Initialize();
            SentinelManager.Initialize();

            // Enable E damage indicators
            DamageIndicator.Initialize(Damages.GetRendDamage);

            // Listen to some required events
            Drawing.OnDraw += OnDraw;
            Spellbook.OnCastSpell += OnCastSpell;
            Orbwalker.OnPostAttack += OnPostAttack;
            Game.OnPostTick += delegate { IsAfterAttack = false; };
        }

        private static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAfterAttack = true;
            AfterAttackTarget = target;
        }

        public static void OnTickBalistaCheck(EventArgs args)
        {
            if (!Config.Specials.UseBalista || !SpellManager.R.IsReady() ||
                (Config.Specials.BalistaComboOnly && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
            {
                return;
            }

            var target = EntityManager.Heroes.Enemies.Find(o => o.Buffs.Any(b => b.DisplayName == "RocketGrab" && b.Caster.NetworkId == SoulBoundSaver.SoulBound.NetworkId));
            if (target != null && target.IsValidTarget())
            {
                if ((Config.Specials.BalistaMoreHealthOnly && Player.Instance.HealthPercent < target.HealthPercent) ||
                    Player.Instance.Distance(target, true) < Config.Specials.BalistaTriggerRange.Pow())
                {
                    // Remove hook, target too close or has more health
                    Game.OnTick -= OnTickBalistaCheck;
                    return;
                }

                // Cast ult
                SpellManager.R.Cast();
                Game.OnTick -= OnTickBalistaCheck;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            // All circles
            foreach (var spell in SpellManager.AllSpells)
            {
                switch (spell.Slot)
                {
                    case SpellSlot.Q:
                        if (!Config.Drawing.DrawQ)
                        {
                            continue;
                        }
                        break;
                    case SpellSlot.W:
                        if (!Config.Drawing.DrawW)
                        {
                            continue;
                        }
                        break;
                    case SpellSlot.E:
                        if (Config.Drawing.DrawELeaving)
                        {
                            Circle.Draw(spell.GetColor(), spell.Range * 0.8f, Player.Instance.Position);
                        }
                        if (!Config.Drawing.DrawE)
                        {
                            continue;
                        }
                        break;
                    case SpellSlot.R:
                        if (!Config.Drawing.DrawR)
                        {
                            continue;
                        }
                        break;
                }

                Circle.Draw(spell.GetColor(), spell.Range, Player.Instance.Position);
            }

            // E damage on healthbar
            DamageIndicator.HealthbarEnabled = Config.Drawing.IndicatorHealthbar;
            DamageIndicator.PercentEnabled = Config.Drawing.IndicatorPercent;
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            // Avoid stupid Q casts while jumping in mid air!
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.Instance.IsDashing())
            {
                // Don't process the packet since we are jumping!
                args.Process = false;
            }
        }
    }
}
