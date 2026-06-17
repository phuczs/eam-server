using System;
using System.Collections.Generic;
using System.Text;


    public class UserException
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string ExceptionType { get; set; } = null!;

        public string Reason { get; set; } = null!;

        public string Severity { get; set; } = "Medium";

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

