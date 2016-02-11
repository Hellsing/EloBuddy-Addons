using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace TheSupport
{
    public class ModeHandler
    {
        private static readonly HitChance[] HitChances =
        {
            HitChance.Low,
            HitChance.Medium,
            HitChance.High
        };

        private TheSupport Support { get; set; }
        private List<Orbwalker.ActiveModes> OrbwalkerModes { get; set; }

        public Dictionary<Orbwalker.ActiveModes, Menu> SpellMenus { get; set; }
        private Dictionary<Orbwalker.ActiveModes, List<SpellUsage>> SpellUsages { get; set; }
        private Dictionary<Orbwalker.ActiveModes, int> ManaModes { get; set; }

        public ModeHandler(TheSupport support)
        {
            // Initialize properties
            Support = support;
            OrbwalkerModes = Enum.GetValues(typeof (Orbwalker.ActiveModes)).Cast<Orbwalker.ActiveModes>().Where(o => o != Orbwalker.ActiveModes.None).ToList();

            SpellMenus = new Dictionary<Orbwalker.ActiveModes, Menu>();
            SpellUsages = new Dictionary<Orbwalker.ActiveModes, List<SpellUsage>>
            {
                { Orbwalker.ActiveModes.None, new List<SpellUsage>() }
            };
            ManaModes = new Dictionary<Orbwalker.ActiveModes, int>();
            OrbwalkerModes.ForEach(mode => { SpellUsages.Add(mode, new List<SpellUsage>()); });
        }

        public void SetManaModes(Orbwalker.ActiveModes modes, int manaPercent)
        {
            GetModes(modes).ForEach(mode => { ManaModes[mode] = manaPercent; });
        }

        public void RegisterSpellUsage(
            Spell.SpellBase spell,
            Orbwalker.ActiveModes modes,
            DamageType damageType = DamageType.Physical,
            Func<bool> preCondition = null,
            Func<AIHeroClient, bool> heroCondition = null,
            Func<Obj_AI_Minion, bool> minionCondition = null,
            HitChance hitChance = HitChance.Unknown,
            bool checkTarget = true,
            string customName = null)
        {
            GetModes(modes).ForEach(mode => { SpellUsages[mode].Add(new SpellUsage(customName, spell, damageType, preCondition, heroCondition, minionCondition, hitChance, checkTarget)); });
        }

        public void CreateModeMenus()
        {
            // Setup prediction menu for all spells which respect prediction
            var predictableSpells = new List<Spell.Skillshot>();
            foreach (var spellUsage in SpellUsages.Values.SelectMany(o => o))
            {
                switch (spellUsage.Type)
                {
                    case SpellUsage.SpellType.Chargeable:
                    case SpellUsage.SpellType.Skillshot:

                        predictableSpells.Add((Spell.Skillshot) spellUsage.Spell);
                        break;
                }
            }
            if (predictableSpells.Count > 0)
            {
                var spells = new Dictionary<SpellSlot, List<Spell.Skillshot>>();
                foreach (var spell in predictableSpells)
                {
                    if (!spells.ContainsKey(spell.Slot))
                    {
                        spells[spell.Slot] = new List<Spell.Skillshot>();
                    }
                    spells[spell.Slot].Add(spell);
                }

                var predictionMenu = Support.Menu.AddSubMenu("HitChances");
                predictionMenu.AddGroupLabel("HitChance values for spells");
                predictionMenu.AddLabel("In here you can define your desired spell HitChances.");
                predictionMenu.AddSeparator();
                foreach (var spellEntry in spells.OrderBy(o => o.Key))
                {
                    var entry = spellEntry;
                    var slot = entry.Key;

                    var comboBox = new ComboBox(slot + " minimum HitChance value", HitChances.Select(o => o.ToString()), 1);
                    comboBox.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                    {
                        var newValue = (HitChance) Enum.Parse(typeof (HitChance), ((ComboBox) sender).SelectedText);
                        foreach (var spell in entry.Value)
                        {
                            spell.MinimumHitChance = newValue;
                        }
                    };
                    predictionMenu.Add("hitChance" + slot, comboBox);
                }
            }

            // Setting up each mode
            foreach (var entry in SpellUsages.Where(o => o.Value.Count > 0))
            {
                // Create the sub menu
                var menu = Support.Menu.AddSubMenu(entry.Key == Orbwalker.ActiveModes.None ? "PermaActive" : entry.Key.ToString());
                SpellMenus[entry.Key] = menu;

                // Add all spells to the menu
                menu.AddGroupLabel("Enabled spells:");
                foreach (var spellUsage in entry.Value)
                {
                    var checkBox = new CheckBox(spellUsage.Name);
                    menu.Add(spellUsage.Name, checkBox);
                    spellUsage.PreCondition = Conditions.Combine(Conditions.IsEnabled(checkBox), spellUsage.PreCondition);
                }

                // Check if we need to respect mana aswell
                if (ManaModes.ContainsKey(entry.Key))
                {
                    menu.AddSeparator();
                    menu.AddGroupLabel("Mana usage limiter");
                    var manaSlider = new Slider("Set to 0 to ingore mana usage", ManaModes[entry.Key]);
                    menu.Add("manaLimiter", manaSlider);
                    foreach (var spellUsage in entry.Value)
                    {
                        spellUsage.PreCondition = Conditions.Combine(Conditions.CurrentMana(manaSlider.CurrentValue), spellUsage.PreCondition);
                    }
                }
            }
        }

        private List<Orbwalker.ActiveModes> GetModes(Orbwalker.ActiveModes modes)
        {
            if (modes == Orbwalker.ActiveModes.None)
            {
                return new List<Orbwalker.ActiveModes>
                {
                    Orbwalker.ActiveModes.None
                };
            }
            return OrbwalkerModes.Where(mode => modes.HasFlag(mode)).ToList();
        }

        public void OnTick()
        {
            // Execute permanent active checks
            foreach (var spellUsage in SpellUsages[Orbwalker.ActiveModes.None].Where(spellUsage => spellUsage.CanUseSpell()))
            {
                spellUsage.CastSpell(Orbwalker.ActiveModes.None);
            }

            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var mode in OrbwalkerModes.Where(mode => Orbwalker.ActiveModesFlags.HasFlag(mode)))
            {
                foreach (var spellUsage in SpellUsages[mode])
                {
                    if (spellUsage.CanUseSpell() && spellUsage.CastSpell(mode))
                    {
                        // Do not cast more than one spell in OnTick
                        return;
                    }
                }
            }
            // ReSharper enable LoopCanBeConvertedToQuery
        }

        private class SpellUsage
        {
            private static readonly Dictionary<Type, SpellType> SpellTypeDictionary = new Dictionary<Type, SpellType>
            {
                { typeof (Spell.Active), SpellType.Active },
                { typeof (Spell.Chargeable), SpellType.Chargeable },
                { typeof (Spell.Skillshot), SpellType.Skillshot },
                { typeof (Spell.Targeted), SpellType.Targeted }
            };

            public enum SpellType
            {
                Active,
                Chargeable,
                Skillshot,
                Targeted
            }

            public string Name { get; private set; }
            public Spell.SpellBase Spell { get; private set; }
            private DamageType DamageType { get; set; }
            public Func<bool> PreCondition { get; set; }
            private Func<AIHeroClient, bool> HeroCondition { get; set; }
            private Func<Obj_AI_Minion, bool> MinionCondition { get; set; }
            private HitChance HitChance { get; set; }
            private bool CheckTarget { get; set; }
            public SpellType Type { get; private set; }

            public SpellUsage(
                string name,
                Spell.SpellBase spell,
                DamageType damageType,
                Func<bool> preCondition,
                Func<AIHeroClient, bool> heroCondition,
                Func<Obj_AI_Minion, bool> minionCondition,
                HitChance hitChance,
                bool checkTarget)
            {
                // Initialize properties
                Name = name ?? "Use " + spell.Slot;
                Spell = spell;
                DamageType = damageType;
                PreCondition = preCondition;
                HeroCondition = heroCondition;
                MinionCondition = minionCondition;
                HitChance = hitChance;
                CheckTarget = checkTarget;
                Type = SpellTypeDictionary[spell.GetType()];
            }

            public bool CanUseSpell()
            {
                return Spell.IsReady() && (PreCondition == null || PreCondition());
            }

            public bool CastSpell(Orbwalker.ActiveModes activeMode)
            {
                switch (activeMode)
                {
                    // Modes involving enemy champions
                    case Orbwalker.ActiveModes.Combo:
                    case Orbwalker.ActiveModes.Harass:
                    {
                        switch (Type)
                        {
                            case SpellType.Active:
                            {
                                var spell = (Spell.Active) Spell;
                                var target = TargetSelector.GetTarget(spell.Range, DamageType);
                                if (CheckTarget && (target == null || !(HeroCondition != null && HeroCondition(target))))
                                {
                                    break;
                                }
                                return Spell.Cast();
                            }
                            case SpellType.Chargeable:
                            {
                                var spell = (Spell.Chargeable) Spell;
                                var target = TargetSelector.GetTarget(spell.MaximumRange, DamageType);
                                if (target != null && (HeroCondition == null || HeroCondition(target)))
                                {
                                    if (!spell.IsCharging)
                                    {
                                        return spell.StartCharging();
                                    }

                                    // Overcharge spell to ensure hit
                                    if (Player.Instance.IsInRange(target, spell.IsFullyCharged ? spell.MaximumRange : spell.Range - 200))
                                    {
                                        var prediction = spell.GetPrediction(target);
                                        if (prediction.HitChance >= (HitChance == HitChance.Unknown ? spell.MinimumHitChance : HitChance))
                                        {
                                            return spell.Cast(target);
                                        }
                                    }
                                }
                                break;
                            }
                            case SpellType.Skillshot:
                            {
                                var spell = (Spell.Skillshot) Spell;
                                var target = TargetSelector.GetTarget(spell.Range, DamageType);
                                if (target != null && (HeroCondition == null || HeroCondition(target)))
                                {
                                    var prediction = spell.GetPrediction(target);
                                    if (prediction.HitChance >= (HitChance == HitChance.Unknown ? spell.MinimumHitChance : HitChance))
                                    {
                                        return spell.Cast(target);
                                    }
                                }
                                break;
                            }
                            case SpellType.Targeted:
                            {
                                var spell = (Spell.Targeted) Spell;
                                var target = TargetSelector.GetTarget(spell.Range, DamageType);
                                if (target != null && (HeroCondition == null || HeroCondition(target)) && spell.IsInRange(target))
                                {
                                    return spell.Cast(target);
                                }
                                break;
                            }
                        }
                        break;
                    }

                    // Modes involving minions and monsters
                    case Orbwalker.ActiveModes.JungleClear:
                    case Orbwalker.ActiveModes.LaneClear:
                    case Orbwalker.ActiveModes.LastHit:
                    {
                        switch (Type)
                        {
                            case SpellType.Active:
                            {
                                var spell = (Spell.Active) Spell;

                                break;
                            }
                        }
                        break;
                    }
                }

                return false;
            }
        }

        public static class Conditions
        {
            public static Func<bool> CurrentMana(int maxMana)
            {
                return () => Player.Instance.Mana >= maxMana;
            }

            public static Func<bool> IsEnabled(Menu menu, string checkBoxKey)
            {
                return IsEnabled(menu.Get<CheckBox>(checkBoxKey));
            }

            public static Func<bool> IsEnabled(CheckBox checkBox)
            {
                return () => checkBox.CurrentValue;
            }

            public static Func<bool> Combine(params Func<bool>[] conditions)
            {
                var noneNull = conditions.Where(o => o != null).ToArray();
                return () => noneNull.Length == 0 || noneNull.All(o => o());
            }
        }
    }
}
