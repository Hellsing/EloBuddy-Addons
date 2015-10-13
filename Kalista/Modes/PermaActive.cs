using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Settings = Hellsing.Kalista.Config.Misc;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Hellsing.Kalista.Modes
{
    public class PermaActive : ModeBase
    {
        private enum SentinelLocations
        {
            Baron,
            Dragon,
            Mid,
            Blue,
            Red
        }

        private const float MaxRandomRadius = 15;
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);
        private static readonly Dictionary<GameObjectTeam, Dictionary<SentinelLocations, Vector2>> Locations = new Dictionary<GameObjectTeam, Dictionary<SentinelLocations, Vector2>>
        {
            {
                GameObjectTeam.Order, new Dictionary<SentinelLocations, Vector2>
                {
                    { SentinelLocations.Mid, new Vector2(8428, 6465) },
                    { SentinelLocations.Blue, new Vector2(3871.489f, 9701.054f) },
                    { SentinelLocations.Red, new Vector2(7862.244f, 4111.187f) }
                }
            },
            {
                GameObjectTeam.Chaos, new Dictionary<SentinelLocations, Vector2>
                {
                    { SentinelLocations.Mid, new Vector2(6545, 8361) },
                    { SentinelLocations.Blue, new Vector2(10931.73f, 6990.844f) },
                    { SentinelLocations.Red, new Vector2(7016.869f, 10775.55f) }
                }
            },
            {
                GameObjectTeam.Neutral, new Dictionary<SentinelLocations, Vector2>
                {
                    { SentinelLocations.Baron, new Vector2(5007.124f, 10471.45f) },
                    { SentinelLocations.Dragon, new Vector2(9866.148f, 4414.014f) }
                }
            }
        };

        private static readonly Dictionary<SentinelLocations, Func<bool>> EnabledLocations = new Dictionary<SentinelLocations, Func<bool>>
        {
            { SentinelLocations.Baron, () => Settings.Sentinel.SendBaron },
            { SentinelLocations.Blue, () => Settings.Sentinel.SendBlue },
            { SentinelLocations.Dragon, () => Settings.Sentinel.SendDragon },
            { SentinelLocations.Mid, () => Settings.Sentinel.SendMid },
            { SentinelLocations.Red, () => Settings.Sentinel.SendRed },
        };

        private static readonly List<Tuple<GameObjectTeam, SentinelLocations>> OpenLocations = new List<Tuple<GameObjectTeam, SentinelLocations>>();
        private static readonly Dictionary<GameObjectTeam, Dictionary<SentinelLocations, Obj_AI_Base>> ActiveSentinels = new Dictionary<GameObjectTeam, Dictionary<SentinelLocations, Obj_AI_Base>>();
        private static Tuple<GameObjectTeam, SentinelLocations> SentLocation { get; set; }

        public PermaActive()
        {
            // Listen to required events
            Orbwalker.OnPostAttack += OnPostAttack;
            Orbwalker.OnUnkillableMinion += OnUnkillableMinion;
            GameObject.OnCreate += OnCreate;

            // Recalculate open sentinel locations
            RecalculateOpenLocations();
        }

        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            // Clear the forced target
            Orbwalker.ForcedTarget = null;

            if (E.IsReady())
            {
                #region Killsteal

                if (Settings.UseKillsteal && EntityManager.Heroes.Enemies.Any(h => h.IsValidTarget(E.Range) && h.IsRendKillable()) && E.Cast())
                {
                    return;
                }

                #endregion

                #region E on big mobs

                if (Settings.UseEBig)
                {
                    if (EntityManager.MinionsAndMonsters.Monsters.Concat(EntityManager.MinionsAndMonsters.EnemyMinions).Any(m =>
                    {
                        if (!m.IsAlly && m.IsValidTarget(E.Range) && m.HasRendBuff())
                        {
                            var skinName = m.BaseSkinName.ToLower();
                            return (skinName.Contains("siege") ||
                                    skinName.Contains("super") ||
                                    skinName.Contains("dragon") ||
                                    skinName.Contains("baron") ||
                                    skinName.Contains("spiderboss")) &&
                                   m.IsRendKillable();
                        }
                        return false;
                    }) && E.Cast())
                    {
                        return;
                    }
                }

                #endregion

                #region E combo (harass plus)

                if (Settings.UseHarassPlus)
                {
                    if (EntityManager.Heroes.Enemies.Any(o => o.IsValidTarget() && E.IsInRange(o) && o.HasRendBuff()) &&
                        EntityManager.MinionsAndMonsters.EnemyMinions.Concat(EntityManager.MinionsAndMonsters.Monsters).Any(o => E.IsInRange(o) && o.IsRendKillable()) &&
                        E.Cast())
                    {
                        return;
                    }
                }

                #endregion

                #region E before death

                if (Player.HealthPercent() < Settings.AutoEBelowHealth && EntityManager.Heroes.Enemies.Any(o => o.IsValidTarget() && o.HasRendBuff() && E.IsInRange(o)) && E.Cast())
                {
                    return;
                }

                #endregion
            }

            // Validate all sentinels
            foreach (var entry in ActiveSentinels.ToArray())
            {
                if (Settings.Sentinel.Alert && entry.Value.Any(o => o.Value.Health == 1))
                {
                    var activeSentinel = entry.Value.First(o => o.Value.Health == 1);
                    Chat.Print("[Kalista] Sentinel at {0} taking damage! (local ping)",
                        string.Concat((entry.Key == GameObjectTeam.Order
                            ? "Blue-Jungle"
                            : entry.Key == GameObjectTeam.Chaos
                                ? "Red-Jungle"
                                : "Lake"), " (", activeSentinel.Key, ")"));
                    TacticalMap.ShowPing(PingCategory.Fallback, activeSentinel.Value.Position, true);
                }

                var invalid = entry.Value.Where(o => !o.Value.IsValid || o.Value.Health < 2 || o.Value.GetBuffCount("kalistaw") == 0).ToArray();
                if (invalid.Length > 0)
                {
                    foreach (var location in invalid)
                    {
                        ActiveSentinels[entry.Key].Remove(location.Key);
                    }
                    RecalculateOpenLocations();
                }
            }

            // Auto sentinel management
            if (Settings.Sentinel.Enabled && W.IsReady() && Player.ManaPercent() >= Settings.Sentinel.Mana)
            {
                if (!Settings.Sentinel.NoModeOnly || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
                {
                    if (OpenLocations.Count > 0 && SentLocation == null)
                    {
                        var closestLocation = OpenLocations.Where(o => Locations[o.Item1][o.Item2].IsInRange(Player, W.Range - MaxRandomRadius / 2))
                            .OrderByDescending(o => Locations[o.Item1][o.Item2].Distance(Player, true))
                            .FirstOrDefault();
                        if (closestLocation != null)
                        {
                            var position = Locations[closestLocation.Item1][closestLocation.Item2];
                            var randomized = (new Vector2(position.X - MaxRandomRadius / 2 + Random.NextFloat(0, MaxRandomRadius),
                                position.Y - MaxRandomRadius / 2 + Random.NextFloat(0, MaxRandomRadius))).To3DWorld();
                            SentLocation = closestLocation;
                            W.Cast(randomized);
                            Core.DelayAction(() => SentLocation = null, 2000);
                        }
                    }
                }
            }
        }

        public static void RecalculateOpenLocations()
        {
            OpenLocations.Clear();
            foreach (var location in Locations)
            {
                if (!ActiveSentinels.ContainsKey(location.Key))
                {
                    OpenLocations.AddRange(location.Value.Where(o => EnabledLocations[o.Key]()).Select(loc => new Tuple<GameObjectTeam, SentinelLocations>(location.Key, loc.Key)));
                }
                else
                {
                    OpenLocations.AddRange(from loc in location.Value
                                           where EnabledLocations[loc.Key]() && !ActiveSentinels[location.Key].ContainsKey(loc.Key)
                                           select new Tuple<GameObjectTeam, SentinelLocations>(location.Key, loc.Key));
                }
            }
        }

        private void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (Config.Modes.Combo.UseQAA &&
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                Player.ManaPercent() > Config.Modes.Combo.ManaQ &&
                Q.IsReady())
            {
                var hero = target as AIHeroClient;
                if (hero != null && Player.GetAutoAttackDamage(hero) < hero.Health + hero.AllShield + hero.AttackShield)
                {
                    // Cast Q after auto attack (combo setting)
                    Q.Cast(hero);
                }
            }
        }

        private void OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (Settings.SecureMinionKillsE && E.IsReady() && target.IsRendKillable())
            {
                // Cast since it's killable with E
                SpellManager.E.Cast();
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (SentLocation == null)
            {
                return;
            }

            var sentinel = sender as Obj_AI_Minion;
            if (sentinel != null && sentinel.IsAlly && sentinel.MaxHealth == 2 && sentinel.Name == "RobotBuddy")
            {
                Core.DelayAction(() => ValidateSentinel(sentinel), 1000);
            }
        }

        private static void ValidateSentinel(Obj_AI_Base sentinel)
        {
            if (sentinel.Health == 2 && sentinel.GetBuffCount("kalistaw") == 1)
            {
                if (!ActiveSentinels.ContainsKey(SentLocation.Item1))
                {
                    ActiveSentinels.Add(SentLocation.Item1, new Dictionary<SentinelLocations, Obj_AI_Base>());
                }
                ActiveSentinels[SentLocation.Item1].Remove(SentLocation.Item2);
                ActiveSentinels[SentLocation.Item1].Add(SentLocation.Item2, sentinel);

                SentLocation = null;
                RecalculateOpenLocations();
            }
        }
    }
}
