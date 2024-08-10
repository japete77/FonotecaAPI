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
        string AWSBucket { get; }
        int AWSLinkExpireInSecs { get; }
        public string AwsTopicArn { get ;  }
        public string AwsTopicName { get; }
        public string AwsPlatformApplicationArnAndroid { get; }
        public string AwsPlatformApplicationArnIOS { get; }
        public string AwsSmtpServer { get; }
        public int AwsSmtpPort { get; }
        public string AwsSmtpUser { get; }
        public string AwsSmtpPassword { get; }
        public string AwsSmtpFrom { get; }
        public string AwsSmtpFromName { get; }

    }
}
