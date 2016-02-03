namespace MasterMind
{
    public interface IComponent
    {
        bool ShouldLoad(bool isSpectatorMode = false);

        void InitializeComponent();
    }
}
