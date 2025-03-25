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
        public string FirebaseAndroidApiKey { get; }
        public string FirebaseAndroidApplicationId { get; }
        public string FirebaseAndroidProjectId { get; }
        public string FirebaseAndroidStorageBucket { get; }
        public string FirebaseiOSApiKey { get; }
        public string FirebaseiOSApplicationId { get; }
        public string FirebaseiOSProjectId { get; }
        public string FirebaseiOSStorageBucket { get; }
        public string FirebaseiOSGcmSenderId { get; }        
    }
}
