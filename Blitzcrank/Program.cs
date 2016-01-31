using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Blitzcrank
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += delegate
            {
                if (Player.Instance.Hero != Champion.Blitzcrank)
                {
                    return;
                }

                #region Menu Stuff

                var menu = MainMenu.AddMenu("Blitzcrank", "blitziii");

                menu.AddGroupLabel("Hitchance");
                var hitchances = new List<HitChance>();
                for (var i = (int) HitChance.Medium; i <= (int) HitChance.Immobile; i++)
                {
                    hitchances.Add((HitChance) i);
                }
                var slider = new Slider(hitchances[0].ToString(), 0, 0, hitchances.Count - 1);
                slider.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs) { slider.DisplayName = hitchances[changeArgs.NewValue].ToString(); };
                menu.Add("hitchance", slider);

                if (EntityManager.Heroes.Enemies.Count > 0)
                {
                    menu.AddSeparator();
                    menu.AddGroupLabel("Enabled targets");
                    var addedChamps = new List<string>();
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(enemy => !addedChamps.Contains(enemy.ChampionName)))
                    {
                        addedChamps.Add(enemy.ChampionName);
                        menu.Add(enemy.ChampionName, new CheckBox(string.Format("{0} ({1})", enemy.ChampionName, enemy.Name)));
                    }
                }

                menu.AddSeparator();
                menu.AddGroupLabel("Drawings");
                var qRange = menu.Add("rangeQ", new CheckBox("Q range"));
                var predictions = menu.Add("predictions", new CheckBox("Visualize prediction"));

                #endregion

                var Q = new Spell.Skillshot(SpellSlot.Q, 925, SkillShotType.Linear, 250, 1800, 70);
                var predictedPositions = new Dictionary<int, Tuple<int, PredictionResult>>();

                Game.OnTick += delegate
                {
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Q.IsReady())
                    {
                        foreach (
                            var enemy in
                                EntityManager.Heroes.Enemies.Where(
                                    enemy => ((TargetSelector.SeletedEnabled && TargetSelector.SelectedTarget == enemy) || menu[enemy.ChampionName].Cast<CheckBox>().CurrentValue) &&
                                             enemy.IsValidTarget(Q.Range + 150) &&
                                             !enemy.HasBuffOfType(BuffType.SpellShield)))
                        {
                            var prediction = Q.GetPrediction(enemy);
                            if (prediction.HitChance >= hitchances[0])
                            {
                                predictedPositions[enemy.NetworkId] = new Tuple<int, PredictionResult>(Environment.TickCount, prediction);

                                // Cast if hitchance is high enough
                                if (prediction.HitChance >= hitchances[slider.CurrentValue])
                                {
                                    Q.Cast(prediction.CastPosition);
                                }
                            }
                        }
                    }
                };

                Drawing.OnDraw += delegate
                {
                    if (qRange.CurrentValue && Q.IsLearned)
                    {
                        Circle.Draw(Q.IsReady() ? Color.Blue : Color.Red, Q.Range, Player.Instance);
                    }

                    if (!predictions.CurrentValue)
                    {
                        return;
                    }

                    foreach (var prediction in predictedPositions.ToArray())
                    {
                        if (Environment.TickCount - prediction.Value.Item1 > 2000)
                        {
                            predictedPositions.Remove(prediction.Key);
                            continue;
                        }

                        Circle.Draw(Color.Red, 75, prediction.Value.Item2.CastPosition);
                        Line.DrawLine(System.Drawing.Color.GreenYellow, Player.Instance.Position, prediction.Value.Item2.CastPosition);
                        Line.DrawLine(System.Drawing.Color.CornflowerBlue, EntityManager.Heroes.Enemies.Find(o => o.NetworkId == prediction.Key).Position, prediction.Value.Item2.CastPosition);
                        Drawing.DrawText(prediction.Value.Item2.CastPosition.WorldToScreen() + new Vector2(0, -20), System.Drawing.Color.LimeGreen,
                            string.Format("Hitchance: {0}%", Math.Ceiling(prediction.Value.Item2.HitChancePercent)), 10);
                    }
                };
            };
        }
    }
}
