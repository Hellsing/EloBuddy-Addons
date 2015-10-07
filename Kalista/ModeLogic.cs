using System;
using System.Collections.Generic;
using EloBuddy;
using Hellsing.Kalista.Modes;

namespace Hellsing.Kalista
{
    public class ModeLogic
    {
        private static List<ModeBase> AvailableModes { get; set; }

        public static void Initialize()
        {
            AvailableModes = new List<ModeBase>
            {
                new PermaActive(),
                new Combo(),
                new Harass(),
                new LaneClear(),
                new JungleClear(),
                new Flee()
            };

            /* // Can't use my preferred version cuz Activator.CreateInstance is blocked -.-
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(ModeBase).IsAssignableFrom(type) || type.Name.Equals(typeof(ModeBase).Name))
                {
                    continue;
                }

                try
                {
                    AvailableModes.Add((ModeBase)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to create new instance of {0}, Namespace: {1}!\nException: {2}\nTrace:\n{3}", type.Name, type.Namespace, e.Message, e.StackTrace);
                }
            }
            */

            Game.OnTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
            AvailableModes.ForEach(mode =>
            {
                if (mode.ShouldBeExecuted())
                {
                    mode.Execute();
                }
            });
        }
    }
}
