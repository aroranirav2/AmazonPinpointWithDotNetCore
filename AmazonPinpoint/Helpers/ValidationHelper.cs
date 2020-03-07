using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace AmazonPinpoint.Utilities
{
    public static class ValidationHelper
    {
        public static bool IsValidPhoneNumber(string inputMobileNumber)
        {
            const string strRegex = @"^\+[1-9]\d{1,14}$";
            var re = new Regex(strRegex);

            return re.IsMatch(inputMobileNumber);
        }

        public static bool IsValidEmailAddress(string emailAddress)
        {
            try
            {
                var validEmailAddress = new MailAddress(emailAddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsValidMessageType(string messageType)
        {
            return messageType.ToUpper().Equals("TRANSACTIONAL") || messageType.ToUpper().Equals("PROMOTIONAL");
        }
    }
}
