using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinLib.ExceptionHandling.Rpc
{
    [Serializable]
    public class ConnectionException : Exception
    {
        public int Status { get; set; }
        public ConnectionException()
        {
        }

        public ConnectionException(string customMessage) : base(customMessage)
        {
        }

        public ConnectionException(string customMessage, int status) : base(customMessage)
        {
            Status = status;
        }

        public ConnectionException(string customMessage, Exception exception) : base(customMessage, exception)
        {
        }
    }
}