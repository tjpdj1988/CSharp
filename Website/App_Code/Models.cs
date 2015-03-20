using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Utility.Models
{
    public enum Sex{
        女,男
    }
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Sex sex { get; set; }
        public int Age { get; set; }
        public DateTime Birthday { get; set; }

        public DateTime LoginTime { get; set; }
        public bool Remember { get; set; }

        
    }
}