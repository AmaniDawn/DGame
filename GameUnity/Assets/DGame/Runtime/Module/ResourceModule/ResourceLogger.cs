namespace DGame
{
    public class ResourceLogger : YooAsset.ILogger
    {
        public void Log(string message)
        {
            DLogger.Info(message);
        }

        public void Warning(string message)
        {
            DLogger.Warning(message);
        }

        public void Error(string message)
        {
            DLogger.Error(message);
        }

        public void Exception(System.Exception exception)
        {
            DLogger.Fatal(exception.Message);
        }
    }
}