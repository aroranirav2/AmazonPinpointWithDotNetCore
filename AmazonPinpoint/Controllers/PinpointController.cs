using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Pinpoint;
using Amazon.Pinpoint.Model;
using Amazon.PinpointSMSVoice;
using Amazon.PinpointSMSVoice.Model;
using Amazon.Runtime;
using AmazonPinpoint.Config;
using AmazonPinpoint.Models;
using AmazonPinpoint.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AmazonPinpoint.Controllers
{
    public class PinpointController : Controller
    {
        private readonly AWSSettings _awsSettings;
        private readonly BasicAWSCredentials _awsCredentials;
        private const string SMS = "SMS";
        private const string EMAIL = "Email";

        public PinpointController(IOptions<AWSSettings> awsSettings)
        {
            _awsSettings = awsSettings.Value;
            _awsCredentials = new BasicAWSCredentials(_awsSettings.AwsCredentials.AWSKeyId,
                _awsSettings.AwsCredentials.AWSKeySecret);
        }

        #region Actions
        [HttpPost]
        public async Task<IActionResult> SendTextMessage([FromBody] TextMessage model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.MessageBody) ||
                string.IsNullOrWhiteSpace(model.DestinationNumber)
                || string.IsNullOrWhiteSpace(model.MessageType))
                return BadRequestResult(SMS);

            if (!ValidationHelper.IsValidPhoneNumber(model.DestinationNumber))
                return BadRequestResult(SMS);

            if (!ValidationHelper.IsValidMessageType(model.MessageType))
                return BadRequestResult(SMS);


            var client = new AmazonPinpointClient(_awsCredentials, RegionEndpoint.GetBySystemName(_awsSettings.Region));

            SendMessagesRequest sendRequest =
                SendMessageRequest(SMS, model.DestinationNumber);
            sendRequest.MessageRequest.MessageConfiguration = new DirectMessageConfiguration
            {
                SMSMessage = new SMSMessage
                {
                    Body = model.MessageBody,
                    MessageType =
                        model.MessageType
                            .ToUpper(), //messageType can be TRANSACTIONAL or PROMOTIONAL
                }
            };
            try
            {
                SendMessagesResponse response = await client.SendMessagesAsync(sendRequest);
                ((IDisposable)client).Dispose();
                if (response.HttpStatusCode != HttpStatusCode.OK)
                    return BadRequestResult(SMS);

                if (response.MessageResponse.Result[model.DestinationNumber].StatusCode != StatusCodes.Status200OK)
                    return BadRequestResult(SMS);
            }
            catch
            {
                ((IDisposable)client).Dispose();
                return BadRequestResult(SMS);
            }
            return new OkObjectResult(new { success = true, message = $"{SMS} sent." });
        }

        [HttpPost]
        public async Task<IActionResult> SendVoiceMessage([FromBody] VoiceMessageModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.DestinationNumber) ||
                (string.IsNullOrWhiteSpace(model.SsmlMessage) && string.IsNullOrWhiteSpace(model.PlainTextMessage)))
                return BadRequestResult("Voice message");

            if (!ValidationHelper.IsValidPhoneNumber(model.DestinationNumber))
                return BadRequestResult("Voice message");

            using (AmazonPinpointSMSVoiceClient client = new AmazonPinpointSMSVoiceClient(_awsCredentials, RegionEndpoint.GetBySystemName(_awsSettings.Region)))
            {
                SendVoiceMessageRequest sendVoiceMessageRequest = new SendVoiceMessageRequest
                {
                    DestinationPhoneNumber = model.DestinationNumber,
                    OriginationPhoneNumber = _awsSettings.AwsTextVoiceMessage.OriginationNumber,
                    Content = new VoiceMessageContent
                    {
                        SSMLMessage = string.IsNullOrWhiteSpace(model.SsmlMessage) ? null : new SSMLMessageType
                        {
                            LanguageCode = model.LanguageCode ?? _awsSettings.LanguageCode, //en-US is ideal for US.
                            VoiceId = model.VoiceId ?? "Matthew",
                            Text = model.SsmlMessage
                        },
                        PlainTextMessage = !string.IsNullOrWhiteSpace(model.SsmlMessage) ? null : new PlainTextMessageType
                        {
                            LanguageCode = model.LanguageCode ?? _awsSettings.LanguageCode, //en-US is ideal for US.
                            VoiceId = model.VoiceId ?? "Matthew",
                            Text = model.PlainTextMessage
                        }
                    }
                };

                try
                {
                    SendVoiceMessageResponse response = await client.SendVoiceMessageAsync(sendVoiceMessageRequest);
                    if (response.HttpStatusCode != HttpStatusCode.OK)
                        return BadRequestResult("Voice message");
                }
                catch
                {
                    return BadRequestResult("Voice message");
                }
                return new OkObjectResult(new { success = true, message = "Voice message sent." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromBody] EmailModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.SubjectBody) || string.IsNullOrWhiteSpace(model.ToAddress) ||
                (string.IsNullOrWhiteSpace(model.HtmlBody) && string.IsNullOrWhiteSpace(model.TextBody)))
                return BadRequestResult(EMAIL);

            if (!ValidationHelper.IsValidEmailAddress(model.ToAddress))
                return BadRequestResult(EMAIL);

            var client = new AmazonPinpointClient(_awsCredentials, RegionEndpoint.GetBySystemName(_awsSettings.Region));

            SendMessagesRequest sendRequest =
                SendMessageRequest(EMAIL.ToUpper(), model.ToAddress);

            sendRequest.MessageRequest.MessageConfiguration = new DirectMessageConfiguration
            {
                EmailMessage = new EmailMessage
                {
                    FromAddress = _awsSettings.AwsEmail.SenderAddress,
                    SimpleEmail = new SimpleEmail
                    {
                        HtmlPart = string.IsNullOrWhiteSpace(model.HtmlBody)
                            ? null
                            : new SimpleEmailPart
                            {
                                Charset = _awsSettings.AwsEmail.CharSet,
                                Data = model.HtmlBody
                            },
                        TextPart = !string.IsNullOrWhiteSpace(model.HtmlBody)
                            ? null
                            : new SimpleEmailPart
                            {
                                Charset = _awsSettings.AwsEmail.CharSet,
                                Data = model.TextBody
                            },
                        Subject = new SimpleEmailPart
                        {
                            Charset = _awsSettings.AwsEmail.CharSet,
                            Data = model.SubjectBody
                        }
                    }
                }
            };
            try
            {
                SendMessagesResponse response = await client.SendMessagesAsync(sendRequest);
                ((IDisposable)client).Dispose();
                if (response.MessageResponse.Result[model.ToAddress].StatusCode != StatusCodes.Status200OK)
                    return BadRequestResult(EMAIL);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                    return BadRequestResult(EMAIL);

            }
            catch
            {
                ((IDisposable)client).Dispose();
                return BadRequestResult("Email");
            }
            return new OkObjectResult(new { success = true, message = "Email sent." });
        }
        #endregion

        private SendMessagesRequest SendMessageRequest(string type, string destination)
        {
            return new SendMessagesRequest
            {
                ApplicationId = _awsSettings.AppId,
                MessageRequest = new MessageRequest
                {
                    Addresses = new Dictionary<string, AddressConfiguration>
                    {
                        {
                            destination,
                            new AddressConfiguration
                            {
                                ChannelType = type
                            }
                        }
                    }
                }
            };
        }

        private static BadRequestObjectResult BadRequestResult(string type)
        {
            return new BadRequestObjectResult(new { success = false, message = $"{type} could not be sent, please contact helpdesk if issue persist." });
        }
    }
}
