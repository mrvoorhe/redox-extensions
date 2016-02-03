using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Tests.Fakes
{
    public class NullWriteToChat : IWriteToChat
    {
        public void WriteToChat(string message, int color, int chatWindow)
        {
            // Nothing to do.
        }
    }
}
