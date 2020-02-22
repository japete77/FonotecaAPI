namespace NuevaLuz
{
    public static class Constants
    {
        public static class Swagger
        {
            public static string EndPoint => $"/swagger/{Version}/swagger.json";
            public const string ApiName = "Fonoteca API";
            public const string Version = "v1";
        }

        public static class App
        {
            public const string MasterTenant = "master";
            public const string LogsDatabase = "logs";
            public const int StreamBufferSize = 8192;
            public const int TokenExpirationInMinutes = 60;
            public const int ResetPasswordTokenExpirationInMinutes = 1440;
            public const int RenewTokenPasswordExpirationInMinutes = 120;
        }

        public static class Roles
        {
            public const string SuperAdmin = "SUPERADMIN";
            public const string Admin = "ADMIN";
            public const string Standard = "STANDARD";
            public const string Agent = "AGENT";
        }

        public static class DefaultAdminUser
        {
            public const string Username = "admin";
            public const string NormalizedUsername = "ADMIN";
            public const string PasswordHash = "AQAAAAEAACcQAAAAEIXJAOSQCp6YeyR22WVl97+EKBy2cfKt67kdXOUVACkN4khAIy6UUNiwGSTtL/SLDQ==";
        }

        public static class Security
        {
            public const string SecurityDocResource = "Belsize.WebApi.Security.json";
            public const string Anonymous = "Anonymous";
        }

        public static class Workflow
        {
            public const string MongoDBCollection = "workflow";
            public const string RedisEventHubChannel = "event-hub";
            public const string RedisPersistenceName = "workflow";
            public const string RedisQueueName = "glass-queue";
        }
    }
}
