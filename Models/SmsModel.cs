namespace TravelAd_Api.Models
{
    public class SmsModel
    {

        public class SmppConnectionRequest

        {

            public string ChannelName { get; set; }
            public string Type { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string SystemId { get; set; }
            public string Password { get; set; }
            public DateTime CreatedDate { get; set; }
            public string ServerId { get; set; }

        }

        public class SendSmsRequest
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Message { get; set; }

            public int ChannelId { get; set; }
        }

        public class SendBulkSmsRequest
        {
            public int ChannelId { get; set; }
            public string Sender { get; set; }
            public List<string> Recipients { get; set; }
            public string Message { get; set; }
        }


        public class StatusBody
        {
            public string Status { get; set; }
            public string Status_Description { get; set; }

            public int? channel_id { get; set; }
        }

        public class CreateSmppChannel
        {
            public string ChannelName { get; set; }
            public string Type { get; set; }

            public string Host { get; set; }
            public int Port { get; set; }
            public string SystemId { get; set; }
            public string Password { get; set; }

        }

        public class CreateSMSServer
        {
            public string ServerName { get; set; }

            public string ServerType { get; set; }

            public string ServerUrl { get; set; }

        }

    }
}
