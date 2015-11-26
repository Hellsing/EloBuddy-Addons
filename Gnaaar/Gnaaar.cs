using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using Gnaaar.Modes;

namespace Gnaaar
{
    public static class Gnaaar
    {
        public static bool IsAfterAttack { get; private set; }
        public static AttackableUnit AfterAttackTarget { get; private set; }

        public static bool HasIgnite { get; private set; }

        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Validate champ
            if (Player.Instance.Hero != Champion.Gnar)
            {
                return;
            }

            // Initialize classes
            SpellQueue.Initialize();
            SpellManager.Initialize();
            Config.Initialize();
            ModeManager.Initialize();

            // Check if the player has ignite
            HasIgnite = Player.Instance.GetSpellSlotFromName("SummonerDot") != SpellSlot.Unknown;

            // Initialize damage indicator
            DamageIndicator.Initialize(Damages.GetTotalDamage);

            // Listen to required events
            Orbwalker.OnPostAttack += OnPostAttack;
            Game.OnPostTick += delegate { IsAfterAttack = false; };
            Drawing.OnDraw += OnDraw;
        }

        private static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAfterAttack = true;
            AfterAttackTarget = target;
        }

        private static void OnDraw(EventArgs args)
        {
            // All circles
            foreach (var spell in SpellManager.Spells)
            {
                if (Player.Instance.IsMiniGnar())
                {
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            if (!Config.Drawings.DrawQ)
                            {
                                continue;
                            }
                            break;
                        case SpellSlot.E:
                            if (!Config.Drawings.DrawE)
                            {
                                continue;
                            }
                            break;
                        default:
                            continue;
                    }
                }
                else
                {
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            if (!Config.Drawings.DrawQMega)
                            {
                                continue;
                            }
                            break;
                        case SpellSlot.W:
                            if (!Config.Drawings.DrawWMega)
                            {
                                continue;
                            }
                            break;
                        case SpellSlot.E:
                            if (!Config.Drawings.DrawEMega)
                            {
                                continue;
                            }
                            break;
                        case SpellSlot.R:
                            if (!Config.Drawings.DrawRMega)
                            {
                                continue;
                            }
                            break;
                        default:
                            continue;
                    }
                }

                Circle.Draw(spell.GetColor(), spell.Range, Player.Instance);
            }

            // E damage on healthbar
            DamageIndicator.HealthbarEnabled = Config.Drawings.IndicatorHealthbar;
            DamageIndicator.PercentEnabled = Config.Drawings.IndicatorPercent;
        }
    }
}
