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
            public int ServerId { get; set; }

            // NEW: TON & NPI Fields
            public int BindingTON { get; set; }
            public int BindingNPI { get; set; }
            public int SenderTON { get; set; }
            public int SenderNPI { get; set; }
            public int DestinationTON { get; set; }
            public int DestinationNPI { get; set; }

        }

        public class SendSmsRequest
        {
            public int ChannelId { get; set; }
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Message { get; set; }

            // NEW: TON & NPI for Sender and Receiver
            public byte SenderTON { get; set; } = 5;  // Default Alphanumeric
            public byte SenderNPI { get; set; } = 0;  // Default Unknown
            public byte ReceiverTON { get; set; } = 1; // Default International/National
            public byte ReceiverNPI { get; set; } = 1; // Default E.164 standard
        }

        public class SendBulkSmsRequest
        {
            public int ChannelId { get; set; }
            public string Sender { get; set; }
            public List<string> Recipients { get; set; }
            public string Message { get; set; }

            // NEW: TON & NPI for Sender and Recipients
            public byte SenderTON { get; set; } = 5;  // Default Alphanumeric
            public byte SenderNPI { get; set; } = 0;  // Default Unknown
            public byte ReceiverTON { get; set; } = 1; // Default International/National
            public byte ReceiverNPI { get; set; } = 1; // Default E.164 standard
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

        public class SmppConnection
        {
            public int ChannelId { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string SystemId { get; set; }
            public string Password { get; set; }
            public string AddressRange { get; set; } = ""; // Optional

            // NEW: TON & NPI for Binding
            public byte BindingTON { get; set; } = 0; // Default Unknown
            public byte BindingNPI { get; set; } = 0; // Default Unknown
        }

    }
}
