using EloBuddy;
using EloBuddy.SDK;
using Settings = Gnaaar.Config.Items;

namespace Gnaaar
{
    public class ItemManager
    {
        // Offensive items
        public static readonly Item Tiamat = new Item(ItemId.Tiamat_Melee_Only, 400);
        public static readonly Item Hydra = new Item(ItemId.Ravenous_Hydra_Melee_Only, 400);

        public static readonly Item Cutlass = new Item(ItemId.Bilgewater_Cutlass, 550);
        public static readonly Item Botrk = new Item(ItemId.Blade_of_the_Ruined_King, 550);

        public static readonly Item Youmuu = new Item(ItemId.Youmuus_Ghostblade);

        // Defensive items
        public static readonly Item Randuin = new Item(ItemId.Randuins_Omen, 500);
        public static readonly Item FaceMountain = new Item(ItemId.Face_of_the_Mountain, 750);

        #region Use item methods

        public static bool UseHydra(Obj_AI_Base target)
        {
            if (target == null)
            {
                return false;
            }
            if (Settings.UseHydra && Hydra.IsReady() && target.IsValidTarget(Hydra.Range))
            {
                return Hydra.Cast();
            }
            if (Settings.UseTiamat && Tiamat.IsReady() && target.IsValidTarget(Tiamat.Range))
            {
                return Tiamat.Cast();
            }
            return false;
        }

        public static bool UseBotrk(AIHeroClient target)
        {
            if (target == null)
            {
                return false;
            }
            if (Settings.UseBotrk && Botrk.IsReady() && target.IsValidTarget(Botrk.Range) &&
                Player.Instance.Health + Player.Instance.GetItemDamage(target, Botrk.Id) < Player.Instance.MaxHealth)
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
            if (target == null)
            {
                return false;
            }
            if (Settings.UseYoumuu && Youmuu.IsReady() && target.IsValidTarget(Player.Instance.GetAutoAttackRange(target)))
            {
                return Youmuu.Cast();
            }
            return false;
        }

        public static bool UseRanduin(AIHeroClient target)
        {
            if (target == null)
            {
                return false;
            }
            if (Settings.UseRanduin && Randuin.IsReady() && target.IsValidTarget(Randuin.Range))
            {
                return Randuin.Cast();
            }
            return false;
        }

        #endregion
    }
}
