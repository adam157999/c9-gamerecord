using System;

namespace GameRecord.Gamess.OctoPlay.Models
{
    public class ErrorResponse
    {
        public string Code { get; set; } // Error code
        public string Description { get; set; } // Error description
        public string TraceId { get; set; } // Trace ID
    }
}
