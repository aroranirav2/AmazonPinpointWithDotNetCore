namespace AmazonPinpoint.Models
{
    public class VoiceMessageModel
    {
        public string DestinationNumber { get; set; }
        public string SsmlMessage { get; set; }
        public string PlainTextMessage { get; set; }
        public string LanguageCode { get; set; }
        public string VoiceId { get; set; }
    }
}
