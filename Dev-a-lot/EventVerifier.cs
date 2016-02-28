using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;

namespace DevALot
{
    public static class EventVerifier
    {
        public static Menu Menu { get; set; }

        #region Menu Values

        private static bool RecursiveCheck
        {
            get { return Menu["recursive"].Cast<CheckBox>().CurrentValue; }
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
        private static bool Animation
        {
            get { return Menu["animation"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool BuffGain
        {
            get { return Menu["buffGain"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool BuffLose
        {
            get { return Menu["buffLose"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool IssueOrder
        {
            get { return Menu["issueOrder"].Cast<CheckBox>().CurrentValue; }
        }

        #endregion

        static EventVerifier()
        {
            #region Menu Creation

            // Create the menu
            Menu = Program.Menu.AddSubMenu("Event verifier");

            Menu.AddGroupLabel("Core Event Verifier");
            Menu.Add("recursive", new CheckBox("Recursively check event sender", false)).CurrentValue = false;
            Menu.AddLabel("Note: This might cause your game to crash! Only use this if you know what you are doing!");
            Menu.Add("basicAttack", new CheckBox("Obj_AI_Base.OnBasicAttack", false)).CurrentValue = false;
            Menu.Add("spellCast", new CheckBox("Obj_AI_Base.OnSpellCast", false)).CurrentValue = false;
            Menu.Add("processSpell", new CheckBox("Obj_AI_Base.OnProcessSpellCast", false)).CurrentValue = false;
            Menu.Add("stopCast", new CheckBox("Spellbook.OnStopCast", false)).CurrentValue = false;
            Menu.Add("newPath", new CheckBox("Obj_AI_Base.OnNewPath", false)).CurrentValue = false;
            Menu.Add("animation", new CheckBox("Obj_AI_Base.OnPlayAnimation (laggy)", false)).CurrentValue = false;
            Menu.Add("create", new CheckBox("GameObject.OnCreate", false)).CurrentValue = false;
            Menu.Add("delete", new CheckBox("GameObject.OnDelete", false)).CurrentValue = false;
            Menu.Add("buffGain", new CheckBox("Obj_AI_Base.OnBuffGain", false)).CurrentValue = false;
            Menu.Add("buffLose", new CheckBox("Obj_AI_Base.OnBuffLose", false)).CurrentValue = false;
            Menu.Add("issueOrder", new CheckBox("Player.OnIssueOrder", false)).CurrentValue = false;

            Menu.AddSeparator();
            Menu.AddLabel(string.Format("Note: All of those tests will create a folder on your Desktop called '{0}'!", Path.GetFileName(Program.ResultPath)));

            #endregion

            #region Event Handling

            var registered = false;
            var registerEvents = new Action(() =>
            {
                // Only register events once
                if (registered)
                {
                    return;
                }
                registered = true;

                // Listen to required events
                Obj_AI_Base.OnBasicAttack += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
                {
                    if (BasicAttack)
                    {
                        Verify(sender, args, "OnBasicAttack", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnSpellCast += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
                {
                    if (SpellCast)
                    {
                        Verify(sender, args, "OnSpellCast", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnProcessSpellCast += delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
                {
                    if (ProcessSpell)
                    {
                        Verify(sender, args, "OnProcessSpellCast", RecursiveCheck);
                    }
                };
                Spellbook.OnStopCast += delegate(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
                {
                    if (StopCast)
                    {
                        Verify(sender, args, "OnStopCast", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnBuffGain += delegate(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
                {
                    if (BuffGain)
                    {
                        Verify(sender, args, "OnProcessSpellCast", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnBuffLose += delegate(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
                {
                    if (BuffLose)
                    {
                        Verify(sender, args, "OnBuffLose", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnNewPath += delegate(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
                {
                    if (NewPath)
                    {
                        Verify(sender, args, "OnNewPath", RecursiveCheck);
                    }
                };
                Obj_AI_Base.OnPlayAnimation += delegate(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
                {
                    if (Animation)
                    {
                        Verify(sender, args, "OnPlayAnimation"); // No recursive check on sender here, cuz too much objects
                    }
                };
                GameObject.OnCreate += delegate(GameObject sender, EventArgs args)
                {
                    if (Create)
                    {
                        Verify(sender, args, "OnCreate", true); // Forced recursive check
                    }
                };
                GameObject.OnDelete += delegate(GameObject sender, EventArgs args)
                {
                    if (Delete)
                    {
                        Verify(sender, args, "OnDelete", true); // Forced recursive check
                    }
                };
                Player.OnIssueOrder += delegate(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
                {
                    if (IssueOrder)
                    {
                        Verify(sender, args, "OnIssueOrder", RecursiveCheck);
                    }
                };
            });

            #endregion

            #region Event Registering

            Menu.Get<CheckBox>("recursive").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("basicAttack").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("spellCast").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("processSpell").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("stopCast").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("newPath").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("animation").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("create").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("delete").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("buffGain").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("buffLose").OnValueChange += delegate { registerEvents(); };
            Menu.Get<CheckBox>("issueOrder").OnValueChange += delegate { registerEvents(); };

            #endregion
        }

        public static void Initialize()
        {
        }

        private static void Verify(GameObject sender, EventArgs args, string eventName, bool recursiveSender = false, bool recursiveArgs = true)
        {
            if (!Directory.Exists(Program.ResultPath))
            {
                Directory.CreateDirectory(Program.ResultPath);
            }

            using (var writer = File.CreateText(Path.Combine(Program.ResultPath, eventName + ".txt")))
            {
                using (var analyzer = new GameObjectDiagnosis(sender, writer))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    analyzer.Analyze(sender, true, recursiveSender);
                    analyzer.Analyze(args, false, recursiveArgs);
                    stopwatch.Stop();
                    Logger.Log(LogLevel.Debug, "Total analyze time of {0}<{2}> (including disk IO): {1}ms", eventName, stopwatch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond,
                        sender.GetType().Name);

                    var times = analyzer.ComputeTimes.OrderByDescending(o => o.Key).ToArray();
                    if (times.Length > 0)
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine("Top {0} compute times:", Math.Min(10, times.Length));
                        for (var i = 0; i < Math.Min(10, times.Length); i++)
                        {
                            writer.WriteLine(" - {0}: {1}ms", times[i].Value, times[i].Key);
                        }
                    }
                }
            }
        }
    }
}
