using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.SharedSetting
{
    public static class RescueRequest_Status 
    {
        public const string PENDING_STATUS = "Pending";
        public const string PROCESSING_STATUS = "Processing";
        public const string COMPLETED_STATUS = "Completed";
        public const string REJECTED_STATUS = "Rejected";
    }

    public static class RescueRequestType 
    {
        public const string RESCUE_TYPE = "Rescue";
        public const string SUPPLY_TYPE = "Supply";
    }
}
