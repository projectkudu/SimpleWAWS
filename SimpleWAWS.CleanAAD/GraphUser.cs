using System;

namespace SimpleWAWS.CleanAAD
{
    public class GraphArray
    {
        public GraphUser[] value { get; set; }
    }
    public class GraphUser
    {
        public string displayName { get; set; }
        public string objectId { get; set; }
        public DateTime? acceptedOn { get; set; }
    }
}
