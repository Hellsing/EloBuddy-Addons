using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using Settings = Hellsing.Kalista.Config.Misc;

namespace Hellsing.Kalista
{
    public class SoulBoundSaver
    {
        private static Spell.Active R
        {
            get { return SpellManager.R; }
        }
        public static AIHeroClient SoulBound { get; private set; }

        private static readonly Dictionary<float, float> IncDamage = new Dictionary<float, float>();
        private static readonly Dictionary<float, float> InstDamage = new Dictionary<float, float>();
        public static float IncomingDamage
        {
            get { return IncDamage.Sum(e => e.Value) + InstDamage.Sum(e => e.Value); }
        }

        public static void Initialize()
        {
            // Listen to related events
            Game.OnTick += OnTick;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnTick(EventArgs args)
        {
            // SoulBound is not found yet!
            if (SoulBound == null)
            {
                SoulBound = EntityManager.Heroes.Allies.Find(h => !h.IsMe && h.Buffs.Any(b => b.Caster.IsMe && b.Name == "kalistacoopstrikeally"));
            }
            else if (Settings.SaveSouldBound && R.IsReady())
            {
                // Ult casting
                if (SoulBound.HealthPercent < 5 && SoulBound.CountEnemiesInRange(500) > 0 ||
                    IncomingDamage > SoulBound.Health)
                    R.Cast();
            }

            // Check spell arrival
            foreach (var entry in IncDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                IncDamage.Remove(entry.Key);
            }

            // Instant damage removal
            foreach (var entry in InstDamage.Where(entry => entry.Key < Game.Time).ToArray())
            {
                InstDamage.Remove(entry.Key);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy)
            {
                // Calculations to save your souldbound
                if (SoulBound != null && Settings.SaveSouldBound)
                {
                    // Auto attacks
                    if ((!(sender is AIHeroClient) || args.SData.IsAutoAttack()) && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                    {
                        // Calculate arrival time and damage
                        IncDamage[SoulBound.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time] = sender.GetAutoAttackDamage(SoulBound);
                    }
                    // Sender is a hero
                    else
                    {
                        var attacker = sender as AIHeroClient;
                        if (attacker != null)
                        {
                            var slot = attacker.GetSpellSlotFromName(args.SData.Name);

                            if (slot != SpellSlot.Unknown)
                            {
                                if (slot == attacker.GetSpellSlotFromName("SummonerDot") && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                                {
                                    // Ingite damage (dangerous)
                                    InstDamage[Game.Time + 2] = attacker.GetSummonerSpellDamage(SoulBound, DamageLibrary.SummonerSpells.Ignite);
                                }
                                else
                                {
                                    switch (slot)
                                    {
                                        case SpellSlot.Q:
                                        case SpellSlot.W:
                                        case SpellSlot.E:
                                        case SpellSlot.R:

                                            if ((args.Target != null && args.Target.NetworkId == SoulBound.NetworkId) || args.End.Distance(SoulBound.ServerPosition) < Math.Pow(args.SData.LineWidth, 2))
                                            {
                                                // Instant damage to target
                                                InstDamage[Game.Time + 2] = attacker.GetSpellDamage(SoulBound, slot);
                                            }

                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
