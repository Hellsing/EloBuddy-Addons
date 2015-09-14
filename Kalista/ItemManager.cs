using EloBuddy;
using EloBuddy.SDK;
using Settings = Hellsing.Kalista.Config.Items;

namespace Hellsing.Kalista
{
    public class ItemManager
    {
        private static readonly AIHeroClient Player = EloBuddy.Player.Instance;

        // Offensive items
        public static readonly Item Cutlass = new Item((int) ItemId.Bilgewater_Cutlass, 550);
        public static readonly Item Botrk = new Item((int) ItemId.Blade_of_the_Ruined_King, 550);

        public static readonly Item Youmuu = new Item((int) ItemId.Youmuus_Ghostblade);

        public static bool UseBotrk(AIHeroClient target)
        {
            if (Settings.UseBotrk && Botrk.IsReady() && target.IsValidTarget(Botrk.Range) && Player.Health + Player.GetItemDamage(target, (ItemId) Botrk.Id) < Player.MaxHealth)
            {
                return Botrk.Cast(target);
            }
            if (Settings.UseCutlass && Cutlass.IsReady() && target.IsValidTarget(Cutlass.Range))
            {
                return Cutlass.Cast(target);
            }
            return false;
        }

        public static bool UseYoumuu(Obj_AI_Base target)
        {
            if (Settings.UseGhostblade && Youmuu.IsReady() && target.IsValidTarget(Player.GetAutoAttackRange(target) + 100))
            {
                return Youmuu.Cast();
            }
            return false;
        }
    }
}
