using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Hellsing.Kalista
{
    public static class Cache
    {
        public static HashSet<Obj_AI_Base> RendEntities { get; private set; }
        public static HashSet<Obj_AI_Minion> RendMinions { get; private set; }
        public static HashSet<AIHeroClient> RendHeroes { get; private set; }

        private static Dictionary<int, BuffInstance> RendBuffs { get; set; }
        private static Dictionary<int, float> RendDamages { get; set; }
        private static Dictionary<int, bool> RendKillable { get; set; } 

        static Cache()
        {
            // Initialize properties
            RendEntities = new HashSet<Obj_AI_Base>();
            RendMinions = new HashSet<Obj_AI_Minion>();
            RendHeroes = new HashSet<AIHeroClient>();
            RendBuffs = new Dictionary<int, BuffInstance>();
            RendDamages = new Dictionary<int, float>();
            RendKillable = new Dictionary<int, bool>();

            // Listen to required events
            Game.OnTick += OnTick;
        }

        internal static void Initialize()
        {
            // Dummy method to trigger static constructor
        }

        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return GetRendBuff(target) != null;
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return RendBuffs.ContainsKey(target.NetworkId) ? RendBuffs[target.NetworkId] : null;
        }

        public static float GetRendDamage(this Obj_AI_Base target)
        {
            return RendDamages.ContainsKey(target.NetworkId) ? RendDamages[target.NetworkId] : 0;
        }

        public static bool IsRendKillable(this Obj_AI_Base target)
        {
            return RendKillable.ContainsKey(target.NetworkId) && RendKillable[target.NetworkId];
        }

        private static void OnTick(EventArgs args)
        {
            // Update rend entities
            RendEntities.Clear();
            RendBuffs.Clear();
            foreach (var entity in ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(onlyEnemyTeam: true)))
            {
                // Get the rend buff
                var rendBuff = GetRendBuffInternal(entity);
                if (rendBuff != null)
                {
                    RendEntities.Add(entity);
                    RendBuffs[entity.NetworkId] = rendBuff;
                }
            }
            RendMinions = new HashSet<Obj_AI_Minion>(RendEntities.OfType<Obj_AI_Minion>());
            RendHeroes = new HashSet<AIHeroClient>(RendEntities.OfType<AIHeroClient>());

            // Calculate rend damages and killable status
            RendDamages.Clear();
            RendKillable.Clear();
            foreach (var entity in RendEntities.ToArray())
            {
                // Get the rend buff
                var rendBuff = entity.GetRendBuff();

                // Should always be true
                if (rendBuff != null)
                {
                    var damage = Damages.GetRendDamage(entity, rendBuff: rendBuff);
                    var killable = Damages.IsRendKillable(entity, damage);

                    // Update values
                    RendDamages[entity.NetworkId] = damage;
                    RendKillable[entity.NetworkId] = killable;
                }
                else
                {
                    // Should never happen, but if so, remove from the lists
                    RendEntities.Remove(entity);
                    RendMinions.Remove(entity as Obj_AI_Minion);
                    RendHeroes.Remove(entity as AIHeroClient);
                }
            }
        }

        private static BuffInstance GetRendBuffInternal(Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster.IsMe && b.IsValid() && b.DisplayName == "KalistaExpungeMarker");
        }
    }
}
