using System.Collections;
using UnityEngine;

namespace DraftTwitchViewers
{
    public class Error
    {
        public string ErrorText { get; private set; }

        public string Message { get; private set; }

        public int Status { get; private set; }

        public Error() { }
        public Error(object ar)
        {
            Hashtable table = ar as Hashtable;

            try { this.ErrorText = (string)table["ErrorText"]; } catch { }
            try { this.Message = (string)table["Message"]; } catch { }
            try { this.Status = (int)table["Status"]; } catch { }

        }
    }
}
