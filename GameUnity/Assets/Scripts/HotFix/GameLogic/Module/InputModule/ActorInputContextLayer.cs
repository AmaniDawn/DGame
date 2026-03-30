namespace GameLogic
{
    public class ActorInputContextLayer : IInputContextLayer
    {
        public int Priority => 0;
        
        public void OnRelease()
        {
        }
        
        public GameplayCommand ResolveStarted()
        {
            return new GameplayCommand();
        }

        public GameplayCommand ResolvePerformed()
        {
            return new GameplayCommand();
        }

        public GameplayCommand ResolveCanceled()
        {
            return new GameplayCommand();
        }

        public void OnStarted()
        {
        }

        public void OnPerformed()
        {
        }

        public void OnCanceled()
        {
        }
    }
}