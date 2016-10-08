using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;

// ReSharper disable UseNullPropagation
// ReSharper disable ConvertPropertyToExpressionBody

namespace PerfectSmite
{
    public static class Smite
    {
        // 370 true damage + 20 at levels 1-4, 30 at levels 5-9, 40 at levels 10-14, 50 at levels 15-18
        private static readonly int[] SmiteDamages = { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 };

        // Summoners Rift big jungle monsters
        private static readonly Dictionary<string, Tuple<string, bool>> JungleMonsters = new Dictionary<string, Tuple<string, bool>>
        {
            { "Baron Nashor", new Tuple<string, bool>("SRU_Baron", true) },
            { "Rift Herald", new Tuple<string, bool>("SRU_RiftHerald", true) },
            { "Cloud Drake", new Tuple<string, bool>("SRU_Dragon_Air", true) },
            { "Infernal Drake", new Tuple<string, bool>("SRU_Dragon_Fire", true) },
            { "Mountain Drake", new Tuple<string, bool>("SRU_Dragon_Earth", true) },
            { "Ocean Drake", new Tuple<string, bool>("SRU_Dragon_Water", true) },
            { "Elder Drake", new Tuple<string, bool>("SRU_Dragon_Elder", true) },
            { "Blue Sentinel (Blue Buff)", new Tuple<string, bool>("SRU_Blue", false) },
            { "Red Brambleback (Red Buff)", new Tuple<string, bool>("SRU_Red", false) },
            { "Gromp", new Tuple<string, bool>("SRU_Gromp", false) },
            { "Greater Murk Wolf", new Tuple<string, bool>("SRU_Murkwolf", false) },
            { "Crimson Raptor", new Tuple<string, bool>("SRU_Razorbeak", false) },
            { "Ancient Krug", new Tuple<string, bool>("SRU_Krug", false) },
            { "Rift Scuttler", new Tuple<string, bool>("Sru_Crab", false) },
        };
        private const int SmiteRange = 500;
        private static readonly Color SmiteColor = Color.Green;
        private static readonly Color SmiteColorOutOfRange = Color.Red;

        private static Spell.Targeted SmiteSpell { get; set; }

        private static int SmiteDamage
        {
            get
            {
                // Return smite damage, max 18 because URF
                return SmiteDamages[Math.Min(18, Player.Instance.Level)];

                // TODO: Remove nerd formula
                /*
                // Base damage
                var damage = 370;

                // + 20 at levels 1-4, 30 at levels 5-9, 40 at levels 10-14, 50 at levels 15-18
                for (var i = 1; i <= Math.Min(18, Player.Instance.Level); i++)
                {
                    damage += (int) (10 + 10 * Math.Ceiling((i + 1) / 5f));
                }

                return damage;
                */
            }
        }

        private static HashSet<Obj_AI_Base> CurrentySmiteable { get; set; }

        private static Menu Menu { get; set; }
        private static KeyBind HoldKey { get; set; }
        private static KeyBind ToggleKey { get; set; }
        private static Dictionary<string, CheckBox> EnabledMonsters { get; set; }
        private static CheckBox DrawRange { get; set; }

        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Check if summoner has Smite
            SpellSlot slot;
            try
            {
                slot = Player.Instance.Spellbook.Spells.First(spell => spell.Name.ToLower().Contains("smite") && (spell.Slot == SpellSlot.Summoner1 || spell.Slot == SpellSlot.Summoner2)).Slot;
            }
            catch (Exception)
            {
                Logger.Info("[PerfectSmite] No Smite summoner spell found, skipping load...");
                return;
            }

            // Setup Menu
            Menu = MainMenu.AddMenu("PerfectSmite", "hellsingPerfectSmite");

            Menu.AddGroupLabel("Key Settings");
            HoldKey = Menu.Add("holdKey", new KeyBind("Smite (Hold)", false, KeyBind.BindTypes.HoldActive, 'H'));
            ToggleKey = Menu.Add("toggleKey", new KeyBind("Smite (Toggle)", false, KeyBind.BindTypes.PressToggle, 'J'));

            Menu.AddGroupLabel("Enabled Monsters");
            EnabledMonsters = new Dictionary<string, CheckBox>();
            var previousEnabled = true;
            foreach (var monster in JungleMonsters)
            {
                if (previousEnabled && !monster.Value.Item2)
                {
                    previousEnabled = false;
                    Menu.AddSeparator();
                }
                EnabledMonsters[monster.Value.Item1] = Menu.Add(monster.Value.Item1, new CheckBox(monster.Key, monster.Value.Item2));
            }

            Menu.AddGroupLabel("Drawings");
            DrawRange = Menu.Add("drawRange", new CheckBox("Draw range around smiteable"));

            // Initialize properties
            SmiteSpell = new Spell.Targeted(slot, SmiteRange, DamageType.True);
            CurrentySmiteable = new HashSet<Obj_AI_Base>();

            // Listen to Update event for most accurate smite time
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;

            // Register known objects
            foreach (var obj in ObjectManager.Get<AttackableUnit>())
            {
                OnCreate(obj, EventArgs.Empty);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            // Check if summoner wants to smite
            if ((HoldKey.CurrentValue || ToggleKey.CurrentValue) && SmiteSpell.IsReady())
            {
                // Get first smiteable monster
                var smiteable =
                    CurrentySmiteable.Where(o => Player.Instance.IsInRange(o, SmiteRange + Player.Instance.BoundingRadius + o.BoundingRadius)).OrderByDescending(o => o.MaxHealth).FirstOrDefault();
                if (smiteable != null)
                {
                    // Check if that monster is enabled
                    var name = smiteable.BaseSkinName.ToLower();
                    if (EnabledMonsters.Any(enabled => name.Equals(enabled.Key.ToLower()) && enabled.Value.CurrentValue))
                    {
                        // Check if monster can be killed
                        if (smiteable.TotalShieldHealth() <= SmiteDamage)
                        {
                            // Cast Smite
                            SmiteSpell.Cast(smiteable);
                        }
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            // Check if summoner wants circles around smiteables
            if (DrawRange.CurrentValue && (HoldKey.CurrentValue || ToggleKey.CurrentValue))
            {
                foreach (var smiteable in CurrentySmiteable.Where(o => o.IsHPBarRendered).Where(smiteable => Player.Instance.IsInRange(smiteable, SmiteRange * 2)))
                {
                    // Get default out of range color
                    var color = SmiteColorOutOfRange;
                    var width = 1;

                    // Check if player is in range of smiteable
                    if (Player.Instance.IsInRange(smiteable, SmiteRange + Player.Instance.BoundingRadius + smiteable.BoundingRadius))
                    {
                        // In range, apply new color
                        color = SmiteColor;

                        // Check if killable
                        if (smiteable.TotalShieldHealth() <= SmiteDamage)
                        {
                            width = 10;
                        }
                    }

                    // Adjust color
                    if (!SmiteSpell.IsReady())
                    {
                        color = SmiteColorOutOfRange;
                        color.A = 75;
                    }

                    // Draw the circle around the smiteable
                    Circle.Draw(color, smiteable.BoundingRadius + SmiteRange, width, smiteable);

                    // Check if killable
                    if (SmiteSpell.IsReady() && smiteable.TotalShieldHealth() <= SmiteDamage)
                    {
                        // Draw a bounding circle around the smiteable if killable
                        Circle.Draw(color, smiteable.BoundingRadius, width, smiteable);
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            // Only neutrals
            if (sender.Team != GameObjectTeam.Neutral)
            {
                return;
            }

            // Only attackables
            var attackable = sender as Obj_AI_Base;
            if (attackable == null)
            {
                return;
            }

            // Check if neutral is a smiteable
            var name = attackable.BaseSkinName.ToLower();
            if (JungleMonsters.Any(entry => name.Equals(entry.Value.Item1.ToLower())))
            {
                CurrentySmiteable.Add(attackable);
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            // Remove game object from set
            CurrentySmiteable.Remove(sender as Obj_AI_Base);
        }
    }
}
