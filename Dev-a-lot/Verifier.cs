using System;
using System.Collections.Generic;
using EloBuddy;

namespace TestAddon
{
    public static class Verifier
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

        static Verifier()
        {
        }

        public static void Initialize()
        {
        }

        private static void BlameFinn()
        {
            foreach (var obj in ObjectManager.Get<GameObject>())
            {
                if (!TypeDictionary.ContainsKey(obj.Type))
                {
                    Console.WriteLine("'{0}' was not found in dictionary!", obj.Type);
                    continue;
                }
                if (TypeDictionary[obj.Type] != obj.GetType())
                {
                    Console.WriteLine("Blame finn0x, '{0}' is not '{1}' as expected with 'GameObjectType.{2}'!", obj.GetType().Name, TypeDictionary[obj.Type].Name, obj.Type);
                }
            }
        }
    }
}
