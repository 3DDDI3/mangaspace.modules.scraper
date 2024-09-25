using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper.Core.Errors
{

    [Serializable]
    public class CustomException : Exception
    {
        public CustomException() { }
        public CustomException(string message) : base(message) { }
        public CustomException(string message, Exception inner) : base(message, inner) { }
        protected CustomException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
