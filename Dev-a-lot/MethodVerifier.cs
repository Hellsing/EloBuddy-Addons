using System;
using System.IO;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace DevALot
{
    public static class MethodVerifier
    {
        public static Menu Menu { get; set; }

        static MethodVerifier()
        {
            #region Menu Creation

            // Create the menu
            Menu = Program.Menu.AddSubMenu("Method verifier");

            Menu.AddGroupLabel("Core Method Verifier");
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
            Menu.AddLabel(string.Format("Note: All of those tests will create a folder in EB AppData called '{0}'!", Path.GetFileName(Program.ResultPath)));

            #endregion

            #region Event Handling

            // Listen to required events
            Game.OnTick += OnTick;

            #endregion
        }

        public static void Initialize()
        {
        }

        private static void OnTick(EventArgs args)
        {
        }
    }
}
