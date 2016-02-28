using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace DevALot
{
    public static class PropertyVerifier
    {
        private static readonly Dictionary<GameObjectType, Type> TypeDictionary = new Dictionary<GameObjectType, Type>
        {
            { GameObjectType.AIHeroClient, typeof (AIHeroClient) },
            { GameObjectType.DrawFX, typeof (DrawFX) },
            { GameObjectType.FollowerObject, typeof (FollowerObject) },
            { GameObjectType.FollowerObjectWithLerpMovement, typeof (FollowerObjectWithLerpMovement) },
            { GameObjectType.GameObject, typeof (GameObject) },
            { GameObjectType.GrassObject, typeof (GrassObject) },
            { GameObjectType.LevelPropAI, typeof (LevelPropAI) },
            { GameObjectType.LevelPropGameObject, typeof (LevelPropGameObject) },
            { GameObjectType.LevelPropSpawnerPoint, typeof (LevelPropSpawnerPoint) },
            { GameObjectType.MissileClient, typeof (MissileClient) },
            { GameObjectType.NeutralMinionCamp, typeof (NeutralMinionCamp) },
            { GameObjectType.UnrevealedTarget, typeof (UnrevealedTarget) },
            { GameObjectType.obj_AI_Base, typeof (Obj_AI_Base) },
            { GameObjectType.obj_AI_Marker, typeof (Obj_AI_Marker) },
            { GameObjectType.obj_AI_Minion, typeof (Obj_AI_Minion) },
            { GameObjectType.obj_AI_Turret, typeof (Obj_AI_Turret) },
            { GameObjectType.obj_Barracks, typeof (Obj_Barracks) },
            { GameObjectType.obj_BarracksDampener, typeof (Obj_BarracksDampener) },
            { GameObjectType.obj_Building, typeof (Obj_Building) },
            { GameObjectType.obj_GeneralParticleEmitter, typeof (Obj_GeneralParticleEmitter) },
            { GameObjectType.obj_HQ, typeof (Obj_HQ) },
            { GameObjectType.obj_InfoPoint, typeof (Obj_InfoPoint) },
            { GameObjectType.obj_Lake, typeof (Obj_Lake) },
            { GameObjectType.obj_LampBulb, typeof (Obj_LampBulb) },
            { GameObjectType.obj_Levelsizer, typeof (Obj_Levelsizer) },
            { GameObjectType.obj_NavPoint, typeof (Obj_NavPoint) },
            { GameObjectType.obj_Shop, typeof (Obj_Shop) },
            { GameObjectType.obj_SpawnPoint, typeof (Obj_SpawnPoint) },
            { GameObjectType.obj_Turret, typeof (Obj_Turret) },
            { GameObjectType.obj_Ward, typeof (Obj_Ward) }
        };

        public static Menu Menu { get; set; }

        public static bool UseOnUpdate
        {
            get { return Menu["onUpdate"].Cast<CheckBox>().CurrentValue; }
        }
        public static bool UseOnTick
        {
            get { return Menu["onTick"].Cast<CheckBox>().CurrentValue; }
        }

        public static readonly Dictionary<GameObjectType, CheckBox> CurrentlyEnabled = new Dictionary<GameObjectType, CheckBox>();

        static PropertyVerifier()
        {
            #region Menu Creation

            // Create the menu
            Menu = Program.Menu.AddSubMenu("Property verifier");

            Menu.AddGroupLabel("GameObject Type Verifier");
            Menu.Add("correctTypes", new CheckBox("Check if all types match", false)).CurrentValue = false;
            Menu["correctTypes"].Cast<CheckBox>().OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    sender.CurrentValue = false;
                    Console.WriteLine("[TypeMatch] Checking...");
                    foreach (var obj in ObjectManager.Get<GameObject>().Where(obj => TypeDictionary.ContainsKey(obj.Type)).Where(obj => TypeDictionary[obj.Type] != obj.GetType()))
                    {
                        Console.WriteLine("[TypeMatch] Got: {0} ({1}) | Expected: {2} ({3})", obj.Type, obj.GetType().Name, obj.Type, TypeDictionary[obj.Type].Name);
                    }
                    Console.WriteLine("[TypeMatch] Completed!");
                    Chat.Print("Check console for TypeMatch results!");
                }
            };

            Menu.AddGroupLabel("Property Verifier");
            Menu.Add("onUpdate", new CheckBox("Use Game.OnUpdate (more checks)", false)).OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    Menu["onTick"].Cast<CheckBox>().CurrentValue = false;
                }
            };
            Menu["onUpdate"].Cast<CheckBox>().CurrentValue = false;
            Menu.Add("onTick", new CheckBox("Use Game.OnTick", false)).OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    Menu["onUpdate"].Cast<CheckBox>().CurrentValue = false;
                }
            };
            Menu["onTick"].Cast<CheckBox>().CurrentValue = false;
            Menu.AddLabel("Note: This might cause your game to crash! Only use this if you know what you are doing!");
            Menu.AddLabel("Note: Only direct matches are being checked, inherited types are ingored");
            foreach (var entry in TypeDictionary)
            {
                CurrentlyEnabled.Add(entry.Key, Menu.Add(entry.Value.Name, new CheckBox(entry.Value.Name, false)));
                CurrentlyEnabled[entry.Key].CurrentValue = false;
            }

            if (GameObjectDiagnosis.PropertiesToIgnore.Count > 0)
            {
                Menu.AddLabel("Enable the disabled (bugged) properties:");
                foreach (var property in GameObjectDiagnosis.PropertiesToIgnore)
                {
                    Menu.Add(property, new CheckBox(property)).CurrentValue = false;
                    Menu[property].Cast<CheckBox>().OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                    {
                        if (args.NewValue)
                        {
                            GameObjectDiagnosis.PropertiesToIgnore.Remove(sender.DisplayName);
                        }
                        else
                        {
                            GameObjectDiagnosis.PropertiesToIgnore.Add(sender.DisplayName);
                        }
                    };
                }
            }

            Menu.AddSeparator();
            Menu.AddLabel(string.Format("Note: Some of those tests will create a folder in EB AppData called '{0}'!", Path.GetFileName(Program.ResultPath)));

            #endregion

            // Listen to required events
            Game.OnUpdate += delegate
            {
                if (UseOnUpdate)
                {
                    VerifyObjects();
                }
            };
            Game.OnTick += delegate
            {
                if (UseOnTick)
                {
                    VerifyObjects();
                }
            };
        }

        private static IEnumerable<GameObjectType> GetCurrentlyEnabled()
        {
            return CurrentlyEnabled.Where(o => o.Value.CurrentValue).Select(o => o.Key).ToArray();
        }

        private static void VerifyObjects()
        {
            var currentlyEnabled = GetCurrentlyEnabled();
            var objects = ObjectManager.Get<GameObject>().Where(o => currentlyEnabled.Contains(o.Type)).ToArray();
            if (objects.Length > 0)
            {
                if (!Directory.Exists(Program.ResultPath))
                {
                    Directory.CreateDirectory(Program.ResultPath);
                }

                using (var writer = File.CreateText(Path.Combine(Program.ResultPath, "PropertyDiagnosis.txt")))
                {
                    writer.WriteLine("Verifying a total of {0} GameObjects!", objects.Length);
                    writer.WriteLine();
                    writer.Flush();

                    var times = new Dictionary<double, string>();

                    foreach (var obj in objects)
                    {
                        using (var analyzer = new GameObjectDiagnosis(obj, writer))
                        {
                            analyzer.Analyze();
                            foreach (var time in analyzer.ComputeTimes)
                            {
                                times[time.Key] = time.Value;
                            }
                        }
                    }

                    var computeResults = new List<AnalyzeComputeResult>();
                    foreach (var time in times)
                    {
                        if (computeResults.All(o => o.Property != time.Value))
                        {
                            computeResults.Add(new AnalyzeComputeResult
                            {
                                Property = time.Value,
                                All = new List<double> { time.Key }
                            });
                        }
                        else
                        {
                            computeResults.First(o => o.Property == time.Value).All.Add(time.Key);
                        }
                    }

                    var orderedTimes = computeResults.OrderByDescending(o => o.Average).ToArray();
                    if (orderedTimes.Length > 0)
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine("Top {0} compute times:", Math.Min(100, orderedTimes.Length));
                        for (var i = 0; i < Math.Min(100, orderedTimes.Length); i++)
                        {
                            writer.WriteLine(" - {0}:", orderedTimes[i].Property);
                            writer.WriteLine("     - Average: {0}", orderedTimes[i].Average);
                            writer.WriteLine("     - Min:     {0}", orderedTimes[i].Min);
                            writer.WriteLine("     - Max:     {0}", orderedTimes[i].Max);
                            writer.WriteLine("     - Amount:  {0}", orderedTimes[i].Amount);
                        }
                    }
                }
            }
        }

        public class AnalyzeComputeResult
        {
            public string Property { get; set; }
            public int Amount
            {
                get { return All.Count; }
            }
            public double Min
            {
                get { return All.Min(); }
            }
            public double Max
            {
                get { return All.Max(); }
            }
            public double Average
            {
                get { return All.Sum() / All.Count; }
            }
            public List<double> All { get; set; }
        }

        public static void Initialize()
        {
        }
    }
}
