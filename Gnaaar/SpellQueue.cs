using EloBuddy;
using EloBuddy.SDK.Constants;

namespace Gnaaar
{
    public class SpellQueue
    {
        private static int _sendTime;

        private static int TickCount
        {
            get { return (int) (Game.Time * 1000); }
        }
        public static bool IsBusy
        {
            get
            {
                var busy =
                    _sendTime > 0 && _sendTime + Game.Ping + 200 - TickCount > 0 ||
                    Player.Instance.Spellbook.IsCastingSpell ||
                    Player.Instance.Spellbook.IsChanneling ||
                    Player.Instance.Spellbook.IsCharging;

                IsBusy = busy;

                return busy;
            }
            private set
            {
                if (!value)
                {
                    _sendTime = 0;
                }
            }
        }
        public static bool IsReady
        {
            get { return !IsBusy; }
        }

        static SpellQueue()
        {
            // Listen to required events
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnStopCast += OnStopCast;
        }

        public static void Initialize()
        {
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                switch (args.Slot)
                {
                    case SpellSlot.Q:
                    case SpellSlot.W:
                    case SpellSlot.E:
                    case SpellSlot.R:

                        if (IsReady)
                        {
                            // We are safe to cast a spell
                            _sendTime = TickCount;
                        }
                        else
                        {
                            // Don't allow the spellcast
                            args.Process = false;
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && !args.SData.IsAutoAttack())
            {
                // Reset timer
                IsBusy = false;
            }
        }

        private static void OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // Reset timer
                IsBusy = false;
            }
        }
    }
}
