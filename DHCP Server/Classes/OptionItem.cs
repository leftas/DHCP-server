using System;
using System.Collections.Generic;
using System.Text;

namespace DNS_Server.Classes
{
    public class OptionItem
    {
        public IDHCPOption Option { get; }
        public bool Force { get; }

        public OptionItem(IDHCPOption option, bool force)
        {
            this.Force = force;
            this.Option = option;
        }
    }
}
