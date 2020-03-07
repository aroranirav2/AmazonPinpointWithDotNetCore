namespace AmazonPinpoint.Models
{
    public class EmailModel
    {
        public string ToAddress { get; set; }
        public string SubjectBody { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
    }
}
