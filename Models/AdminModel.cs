using System.Text.Json.Serialization;

namespace TravelAd_Api.Models
{
    public class AdminModel
    {
        public class UpdateCampaign
        {
            public int campaignId { get; set; }
            public string status { get; set; }
            public int serverId { get; set; }
        }

    }
}
