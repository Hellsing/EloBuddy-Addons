using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;

namespace Hellsing.Kalista
{
    public class Kalista
    {
        public static bool IsAfterAttack { get; private set; }
        public static AttackableUnit AfterAttackTarget { get; private set; }

        static Kalista()
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        public static void Main(string[] args)
        { }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Validate champion
            if (Player.Instance.ChampionName != "Kalista")
            {
                return;
            }

            // Initialize classes
            Config.Initialize();
            SoulBoundSaver.Initialize();
            ModeLogic.Initialize();

            // Enable E damage indicators
            DamageIndicator.Initialize(Damages.GetRendDamage);

            // Listen to some required events
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
            Orbwalker.OnPostAttack += OnPostAttack;
            Game.OnPostTick += delegate { IsAfterAttack = false; };
        }

        private static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAfterAttack = true;
            AfterAttackTarget = target;
        }

        private static void OnDraw(EventArgs args)
        {
            // TODO: Add when existing
            /*
            // All circles
            foreach (var circleLink in Config.Drawing.AllCircles)
            {
                if (circleLink.Value.Active)
                {
                    Render.Circle.DrawCircle(Player.Position, circleLink.Value.Radius, circleLink.Value.Color);
                }
            }

            // E damage on healthbar
            DamageIndicator.DrawingColor = Config.Drawing.HealthbarE.Value.Color;
            DamageIndicator.Enabled = Config.Drawing.HealthbarE.Value.Active;
            */
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // E - Rend
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    // Make the orbwalker attack again, might get stuck after casting E
                    Core.DelayAction(Orbwalker.ResetAutoAttack, 250);
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            // Avoid stupid Q casts while jumping in mid air!
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.Instance.IsDashing())
            {
                // Don't process the packet since we are jumping!
                args.Process = false;
            }
        }
    }
}
