namespace AmazonPinpoint.Config
{
    public class AWSSettings
    {
        public string AppId { get; set; }
        public string Region { get; set; }
        public string LanguageCode { get; set; }
        public AWSCredentials AwsCredentials { get; set; }
        public AWSEmail AwsEmail { get; set; }
        public AWSTextVoiceMessage AwsTextVoiceMessage { get; set; }
    }
}
