using LiteDB;
using Resto.Front.Api.Data.Brd;
using System;

namespace MTS_plugin.Database
{
    public class IdObj
    {
        [BsonId]
        public Guid IikoId { get; set; }
        public long MtsId { get; set; }
        public DeliveryStatus Status { get; set; }
    }
}
