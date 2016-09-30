using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;

namespace Karthus
{
    public sealed class UltimateAlerter
    {
        private Karthus Instance { get; set; }

        public UltimateAlerter(Karthus instance)
        {
            // Initialize properties
            Instance = instance;

            // Listen to required events
            Drawing.OnDraw += OnDraw;
        }

        private void OnDraw(EventArgs args)
        {
            // Ultimate killable notification
            if (Instance.SpellHandler.R.IsLearned)
            {
                var killable = new Dictionary<AIHeroClient, float>();
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(o => !o.IsDead && o.Health > 0))
                {
                    var damage = Instance.SpellHandler.R.GetRealDamage(enemy);
                    if (damage > enemy.TotalShieldHealth())
                    {
                        killable.Add(enemy, damage);
                    }
                }

                if (killable.Count > 0)
                {
                    if (killable.Count > 1)
                    {
                        // Sort killable by percent damage on target
                        killable = killable.OrderBy(o => o.Value / o.Key.TotalShieldHealth()).ToDictionary(o => o.Key, o => o.Value);
                    }

                    // Draw info near mouse
                    var pos = Game.CursorPos2D + new Vector2(-50, 50);
                    Drawing.DrawText(pos, Instance.SpellHandler.R.IsReady() ? Color.GreenYellow : Color.OrangeRed, "Targets killable: " + killable.Count, 10);
                    foreach (var target in killable)
                    {
                        pos += new Vector2(0, 20);
                        var formatString = "{0} - {1}% overkill";
                        int alliesNearby;
                        if (!target.Key.IsHPBarRendered)
                        {
                            formatString += " (no vision)";
                        }
                        else if ((alliesNearby = target.Key.CountAlliesInRange(1000)) > 0)
                        {
                            formatString += string.Format(" ({0} allies nearby)", alliesNearby);
                        }
                        else
                        {
                            formatString += " (free kill)";
                        }
                        Drawing.DrawText(pos, Color.NavajoWhite, string.Format(formatString, target.Key.ChampionName, Math.Floor(target.Value / target.Key.TotalShieldHealth() * 100) - 100), 10);
                    }
                }
            }
        }
    }
}
