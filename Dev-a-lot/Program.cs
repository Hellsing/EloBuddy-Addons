using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace TestAddon
{
    internal class Program
    {
        private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static readonly string ResultPath = Path.Combine(DesktopPath, "Test Results");

        private static readonly string ProcessSpellLocation = Path.Combine(ResultPath, "process_spell.txt");
        private static readonly string ObjectManagerLocation = Path.Combine(ResultPath, "object_manager.txt");
        private static readonly string NewPathLocation = Path.Combine(ResultPath, "new_path.txt");
        private static readonly string StopCastLocation = Path.Combine(ResultPath, "stop_cast.txt");
        private static readonly string CreateLocation = Path.Combine(ResultPath, "object_create.txt");
        private static readonly string DeleteLocation = Path.Combine(ResultPath, "object_delete.txt");

        private static Menu Menu { get; set; }

        private static bool ShowBuffs
        {
            get { return Menu["buffs"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowAaDamage
        {
            get { return Menu["autoAttack"].Cast<CheckBox>().CurrentValue; }
        }

        private static bool BasicAttack
        {
            get { return Menu["basicAttack"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool SpellCast
        {
            get { return Menu["spellCast"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ProcessSpell
        {
            get { return Menu["processSpell"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool NewPath
        {
            get { return Menu["newPath"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool StopCast
        {
            get { return Menu["stopCast"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool Create
        {
            get { return Menu["create"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool Delete
        {
            get { return Menu["delete"].Cast<CheckBox>().CurrentValue; }
        }

        private static void Main(string[] args)
        {
            if (!Directory.Exists(ResultPath))
            {
                Directory.CreateDirectory(ResultPath);
            }

            Loading.OnLoadingComplete += delegate
            {
                // Setup a menu
                Menu = MainMenu.AddMenu("Dev-a-lot", "devalot");

                Menu.AddGroupLabel("General");
                Menu.Add("buffs", new CheckBox("Show buffs"));
                Menu.Add("autoAttack", new CheckBox("Auto attack damage"));

                Menu.AddGroupLabel("Core event property stress tests");
                Menu.Add("basicAttack", new CheckBox("Obj_AI_Base.OnBasicAttack", false)).CurrentValue = false;
                Menu.Add("spellCast", new CheckBox("Obj_AI_Base.OnSpellCast", false)).CurrentValue = false;
                Menu.Add("processSpell", new CheckBox("Obj_AI_Base.OnProcessSpellCast", false)).CurrentValue = false;
                Menu.Add("newPath", new CheckBox("Obj_AI_Base.OnNewPath", false)).CurrentValue = false;
                Menu.Add("stopCast", new CheckBox("Spellbook.OnStopCast", false)).CurrentValue = false;
                Menu.Add("create", new CheckBox("GameObject.OnCreate", false)).CurrentValue = false;
                Menu.Add("delete", new CheckBox("GameObject.OnDelete", false)).CurrentValue = false;

                Obj_AI_Base.OnBasicAttack += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs eventArgs)
                {
                    if (BasicAttack)
                    {
                        OnProcessSpellCast(sender, eventArgs);
                    }
                };
                Obj_AI_Base.OnSpellCast += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs eventArgs)
                {
                    if (SpellCast)
                    {
                        OnProcessSpellCast(sender, eventArgs);
                    }
                };
                Obj_AI_Base.OnProcessSpellCast += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs eventArgs)
                {
                    if (ProcessSpell)
                    {
                        OnProcessSpellCast(sender, eventArgs);
                    }
                };

                Obj_AI_Base.OnNewPath += OnNewPath;
                Spellbook.OnStopCast += OnStopCast;

                GameObject.OnCreate += OnCreate;
                GameObject.OnDelete += OnDelete;

                Drawing.OnDraw += delegate
                {
                    if (!ShowBuffs && !ShowAaDamage)
                    {
                        return;
                    }

                    foreach (var hero in EntityManager.Heroes.AllHeroes)
                    {
                        if (hero.IsEnemy && ShowAaDamage && hero.IsValidTarget() && hero.IsHPBarRendered)
                        {
                            Drawing.DrawText(hero.HPBarPosition, Color.NavajoWhite, string.Format("Damage: {0}", Player.Instance.GetAutoAttackDamage(hero, true)), 10);
                        }

                        if (ShowBuffs)
                        {
                            var i = 0;
                            const int step = 20;

                            foreach (var buff in hero.Buffs.Where(o => o.IsValid()))
                            {
                                Drawing.DrawText(hero.Position.WorldToScreen() + new Vector2(0, i), Color.LawnGreen,
                                    string.Format("DisplayName: {0} | Caster: {1} | Count: {2}", buff.DisplayName, buff.Caster.Name, buff.Count), 10);
                                i += step;
                            }
                        }
                    }
                };

                return;

                Game.OnUpdate += delegate
                {
                    using (var writer = File.CreateText(ObjectManagerLocation))
                    {
                        writer.WriteLine("----------------------------------------------------------------------------------");
                        writer.WriteLine("OnUpdate, analysing all MissileClient properties in ObjectManager...");
                        writer.WriteLine("----------------------------------------------------------------------------------");
                        writer.Flush();
                        foreach (var obj in ObjectManager.Get<MissileClient>())
                        {
                            writer.WriteLine("Checking if current unit is valid");
                            writer.Flush();
                            if (true)
                            {
                                writer.Write(" - Object type: ");
                                writer.Flush();
                                writer.WriteLine(obj.GetType().Name);
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.WriteLine("Analyzing all public properties of " + obj.GetType().Name);
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.Flush();
                                foreach (var propertyInfo in obj.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                                {
                                    writer.Write(" - " + propertyInfo.Name + ": ");
                                    writer.Flush();
                                    writer.WriteLine(propertyInfo.GetValue(obj));
                                    writer.Flush();
                                }
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.WriteLine("All properties analyzed, analyzing underlaying SData");
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.Flush();
                                foreach (var propertyInfo in obj.SData.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                                {
                                    writer.Write(" - " + propertyInfo.Name + ": ");
                                    writer.Flush();
                                    writer.WriteLine(propertyInfo.GetValue(obj.SData, null));
                                    writer.Flush();
                                }
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.WriteLine("Analyzing of " + obj.GetType().Name + " complete!");
                                writer.WriteLine("----------------------------------------------------------------------------------");
                                writer.WriteLine();
                            }
                        }
                        writer.WriteLine("----------------------------------------------------------------------------------");
                        writer.WriteLine("Analyzing ObjectManager complete!");
                        writer.WriteLine("----------------------------------------------------------------------------------");
                    }
                };
            };
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (!Delete)
            {
                return;
            }

            using (var writer = File.CreateText(DeleteLocation))
            {
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("OnDelete, analysing properties...");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                writer.Write(" - Sender type: ");
                writer.Flush();
                writer.WriteLine(sender.GetType().Name);
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of sender");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in sender.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(sender, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing OnDelete complete!");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine();
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (!Create)
            {
                return;
            }

            using (var writer = File.CreateText(CreateLocation))
            {
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("OnCreate, analysing properties...");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                writer.Write(" - Sender type: ");
                writer.Flush();
                writer.WriteLine(sender.GetType().Name);
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of sender");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in sender.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(sender, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing OnCreate complete!");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine();
            }
        }

        private static void OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (!StopCast)
            {
                return;
            }

            using (var writer = File.CreateText(StopCastLocation))
            {
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("OnStopCast, analysing properties...");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                writer.Write(" - Sender type: ");
                writer.Flush();
                writer.WriteLine(sender.GetType().Name);
                writer.Write(" - Sender name: ");
                writer.Flush();
                writer.WriteLine(sender.BaseSkinName);
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of sender");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in sender.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(sender, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of SpellbookStopCastEventArgs");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in args.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(args, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing OnStopCast complete!");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine();
            }
        }

        private static void OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!NewPath)
            {
                return;
            }

            using (var writer = File.CreateText(NewPathLocation))
            {
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("OnNewPath, analysing properties...");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                writer.Write(" - Sender type: ");
                writer.Flush();
                writer.WriteLine(sender.GetType().Name);
                writer.Write(" - Sender name: ");
                writer.Flush();
                writer.WriteLine(sender.BaseSkinName);
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of sender");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in sender.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(sender, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of GameObjectNewPathEventArgs");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in args.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(args, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing OnNewPath complete!");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine();
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            using (var writer = File.CreateText(ProcessSpellLocation))
            {
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("OnProcessSpellCast (" + new StackTrace().GetFrame(1).GetMethod().Name + "), analysing properties...");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                writer.Write(" - Sender type: ");
                writer.Flush();
                writer.WriteLine(sender.GetType().Name);
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of sender");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in sender.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(sender, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing all public properties of GameObjectProcessSpellCastEventArgs");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in args.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(args, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("All properties analyzed, analyzing underlaying SData");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.Flush();
                foreach (var propertyInfo in args.SData.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetGetMethod() != null))
                {
                    writer.Write(" - " + propertyInfo.Name + ": ");
                    writer.Flush();
                    writer.WriteLine(propertyInfo.GetValue(args.SData, null));
                    writer.Flush();
                }
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine("Analyzing OnProcessSpellCast complete!");
                writer.WriteLine("----------------------------------------------------------------------------------");
                writer.WriteLine();
            }
        }
    }
}
