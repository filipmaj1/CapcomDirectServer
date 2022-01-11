using System;
using System.Collections.Generic;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class CapcomInfo
    {
        public string Id { get; set; }
        public string Handle { get; set; }
        public string Email { get; set; }
        public string TelephoneNumber { get; set; }

        public override string ToString()
        {
            if (Id != null && Handle != null)
                return $"<{Id}>[{Handle}]";
            else if (Id != null)
                return $"<{Id}>[---]";
            else if (Handle != null)
                return $"<--->[{Handle}]";
            else
                return "<--->[---]";
        }
    }
}
