namespace Config.Interfaces
{
    public interface ISettings
    {
        string Server { get; }
        int Port { get; }
        string Database { get; }
        string User { get; }
        string Password { get; }
        string SessionTableName { get; }
        int SessionTTLSeconds { get; }
    }
}
