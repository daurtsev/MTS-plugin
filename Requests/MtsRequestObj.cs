using System.Threading.Tasks;

namespace MTS_plugin.Requests
{
    public class MtsRequestObj
    {
        public Priority Priority { get; set; }

        public Task<bool> Task { get; set; }
    }
}