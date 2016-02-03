using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Wrapper
{
    public interface IWriteToChat
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        /// <param name="chatWindow">0=Default, 1=Main,2-5=1-4 Windows</param>
        void WriteToChat(string message, int color, int chatWindow);
    }
}
