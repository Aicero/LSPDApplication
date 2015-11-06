using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LSPDApplication.Classes
{
    [Serializable]
    [XmlRoot("Duty")]
    public class Duty
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Duration { get; set; }

        public Duty()
        {
        }
    }
}
