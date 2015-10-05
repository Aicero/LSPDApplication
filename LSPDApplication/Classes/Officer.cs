using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSPDApplication.Classes
{
    public class Officer
    {
        public string workerNick { get; set; }
        public string workerRank { get; set; }
        public int workerPayday { get; set; }
        public int workerSkin { get; set; }

        public List<Duty> workerDutyList { get; set; }
    }
}
