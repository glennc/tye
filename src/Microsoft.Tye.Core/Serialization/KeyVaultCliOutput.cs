using System;
using System.Collections.Generic;
using System.Text;

namespace Tye.Serialization
{

    public class KeyVaultCliOutput
    {
        public Attributes attributes { get; set; }
        public object contentType { get; set; }
        public string id { get; set; }
        public object kid { get; set; }
        public object managed { get; set; }
        public string name { get; set; }
        public object tags { get; set; }
        public string value { get; set; }
    }

    public class Attributes
    {
        public DateTime created { get; set; }
        public bool enabled { get; set; }
        public object expires { get; set; }
        public object notBefore { get; set; }
        public string recoveryLevel { get; set; }
        public DateTime updated { get; set; }
    }

}
