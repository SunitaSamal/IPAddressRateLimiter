using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPAddressRateLimiterUsingAttribute
{ 
    [AttributeUsage(AttributeTargets.Class)]
    public class RateLimitAttribute : Attribute
    {
        public int TimeWindow { get; set; }
        public int MaxRequests { get; set; }
    }
}
