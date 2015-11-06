using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LSPDApplication.Classes
{
    [Serializable]
    [XmlRoot("Officer")]
    public class Officer
    {
        public string workerNick { get; set; }
        public string workerRank { get; set; }
        public int workerPayday { get; set; }
        public int workerSkin { get; set; }

        public List<Duty> workerDutyList { get; set; }

        public string workerDutyTime { get; set; }
        public int workerHappyHours { get; set; }
        public int workerHappyHoursMoney { get; set; }

        public int workerAway { get; set; }
        public bool workerWarn { get; set; }

        public Officer()
        {
        }
    }
}
