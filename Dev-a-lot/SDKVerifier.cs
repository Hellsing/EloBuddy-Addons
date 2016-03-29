using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Notifications;

namespace DevALot
{
    // ReSharper disable once InconsistentNaming
    public static class SDKVerifier
    {
        public static Menu Menu { get; set; }

        private static CheckBox ShowNotification { get; set; }

        static SDKVerifier()
        {
            // Initialize Menu
            Menu = Program.Menu.AddSubMenu("SDK verifier");

            Menu.AddGroupLabel("Notifications");
            (ShowNotification = Menu.Add("showNotification", new CheckBox("Show single notification", false))).CurrentValue = false;
            ShowNotification.OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue)
                {
                    sender.CurrentValue = false;
                    Notifications.Show(
                        new SimpleNotification("Verifying notifications",
                            "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et"),
                        2000);
                }
            };
        }

        public static void Initialize()
        {
        }
    }
}
