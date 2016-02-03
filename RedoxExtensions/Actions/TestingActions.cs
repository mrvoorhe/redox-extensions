using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;

namespace RedoxExtensions.Actions
{
    /// <summary>
    /// Playground for testing random stuff
    /// </summary>
    internal static class TestingActions
    {

        internal static void ProcessTestCommand(ICommand command)
        {
            if(command.Arguments.Count == 0)
            {
                TestOfTheDay();
                return;
            }

            // A place to put test functionality that I want to keep available for awhile.
            // I can put it behind a name
            switch (command.Arguments[0])
            {
                default:
                    REPlugin.Instance.Chat.WriteLine("Unknown Test Value : {0} ", command.Arguments[0]);
                    break;
            }
        }

        /// <summary>
        /// Place to put in w/e test code I feel like testing
        /// </summary>
        private static void TestOfTheDay()
        {
            REPlugin.Instance.Chat.WriteLine("Test something more interesting!!!");
        }
    }
}
