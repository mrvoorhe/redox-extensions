using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using RedoxExtensions.Commands;
using RedoxExtensions.General.Utilities;

namespace RedoxExtensions.Tests.CommandTests
{
    [TestFixture]
    public class CommandHelperTests
    {
        #region Create Extensions Command Tests

        [Test]
        public void CreateExtensionsCommand_NoArgs()
        {
            var result = CommandHelpers.CreateRedoxExtensionsCommandText("test");
            Assert.AreEqual("/re test", result);
        }

        [Test]
        public void CreateExtensionsCommand_WithSingleArg()
        {
            var result = CommandHelpers.CreateRedoxExtensionsCommandText("test", "one");
            Assert.AreEqual("/re test one", result);
        }

        [Test]
        public void CreateExtensionsCommand_WithManyArgs()
        {
            var result = CommandHelpers.CreateRedoxExtensionsCommandText("test", "one", "two", "three");
            Assert.AreEqual("/re test one two three", result);
        }

        #endregion

        #region Create Fellow Command Tests

        [Test]
        public void CreateFellowCommand_NoArgs()
        {
            var result = CommandHelpers.CreateRedoxFellowCommandText("test");
            Assert.AreEqual("/rf test", result);
        }

        [Test]
        public void CreateFellowCommand_WithSingleArg()
        {
            var result = CommandHelpers.CreateRedoxFellowCommandText("test", "one");
            Assert.AreEqual("/rf test one", result);
        }

        [Test]
        public void CreateFellowCommand_WithManyArgs()
        {
            var result = CommandHelpers.CreateRedoxFellowCommandText("test", "one", "two", "three");
            Assert.AreEqual("/rf test one two three", result);
        }

        #endregion

        #region Create Foreign Command Tests

        #region All

        [Test]
        public void CreateForeignCommandText_NoArgs()
        {
            var result = CommandHelpers.CreateForeignCommandText("test");
            Assert.AreEqual("!test", result);
        }

        [Test]
        public void CreateForeignCommandText_WithSingleArg()
        {
            var result = CommandHelpers.CreateForeignCommandText("test", "one");
            Assert.AreEqual("!test one", result);
        }

        [Test]
        public void CreateForeignCommandText_WithManyArgs()
        {
            var result = CommandHelpers.CreateForeignCommandText("test", "one", "two", "three");
            Assert.AreEqual("!test one|two|three", result);
        }

        #endregion

        #region Slaves Only

        [Test]
        public void CreateForeignSlaveOnlyCommandText_NoArgs()
        {
            var result = CommandHelpers.CreateForeignSlaveOnlyCommandText("test");
            Assert.AreEqual("#test", result);
        }

        [Test]
        public void CreateForeignSlaveOnlyCommandText_WithSingleArg()
        {
            var result = CommandHelpers.CreateForeignSlaveOnlyCommandText("test", "one");
            Assert.AreEqual("#test one", result);
        }

        [Test]
        public void CreateForeignSlaveOnlyCommandText_WithManyArgs()
        {
            var result = CommandHelpers.CreateForeignSlaveOnlyCommandText("test", "one", "two", "three");
            Assert.AreEqual("#test one|two|three", result);
        }

        #endregion

        #region Master Only

        [Test]
        public void CreateForeignMasterOnlyCommandText_NoArgs()
        {
            var result = CommandHelpers.CreateForeignMasterOnlyCommandText("test");
            Assert.AreEqual("$test", result);
        }

        [Test]
        public void CreateForeignMasterOnlyCommandText_WithSingleArg()
        {
            var result = CommandHelpers.CreateForeignMasterOnlyCommandText("test", "one");
            Assert.AreEqual("$test one", result);
        }

        [Test]
        public void CreateForeignMasterOnlyCommandText_WithManyArgs()
        {
            var result = CommandHelpers.CreateForeignMasterOnlyCommandText("test", "one", "two", "three");
            Assert.AreEqual("$test one|two|three", result);
        }

        #endregion

        #endregion

        #region Meta Data

        [Test]
        public void TestAppendMetaData()
        {
            var commandString = CommandHelpers.CreateForeignCommandText("test", "arg1", "arg2");

            var result = CommandHelpers.AppendMetaData(commandString, "meta1", "meta2");

            var expectedResult = commandString + " (meta1|meta2)";
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void TestStripMetaData()
        {
            IEnumerable<string> meta;

            var commandString = CommandHelpers.CreateForeignCommandText("test");

            var commandStringWithMeta = CommandHelpers.AppendMetaData(commandString, "arg1", "arg2");

            var result = CommandHelpers.StripMetaData(commandStringWithMeta, out meta);

            Assert.AreEqual(commandString, result);

            var metaAsList = meta.ToList();

            Assert.AreEqual("arg1", metaAsList[0]);
            Assert.AreEqual("arg2", metaAsList[1]);

            Assert.AreEqual(2, metaAsList.Count);
        }

        [Test]
        public void TestStripMetaData_WhenNoMeta()
        {
            IEnumerable<string> meta;

            var commandString = CommandHelpers.CreateForeignCommandText("test");

            var result = CommandHelpers.StripMetaData(commandString, out meta);

            Assert.AreEqual(commandString, result);

            var metaAsList = meta.ToList();

            Assert.AreEqual(0, metaAsList.Count);
        }

        #endregion

        #region Splitting Tests

        [Test]
        public void Test_SplitCommandAndArguments_WithDirectEntryCommand_NoArguments()
        {
            var result = CommandHelpers.SplitCommandAndArguments("test");

            Assert.AreEqual("test", result.Item1);
            Assert.AreEqual(string.Empty, result.Item2);
        }

        [Test]
        public void Test_SplitCommandAndArguments_WithDirectEntryCommand_MultipleArguments()
        {
            var result = CommandHelpers.SplitCommandAndArguments("test arg1 arg2");

            Assert.AreEqual("test", result.Item1);
            Assert.AreEqual("arg1 arg2", result.Item2);
        }

        [Test]
        public void Test_SplitCommandAndArguments_WithForeignCommand_NoArguments()
        {
            var result = CommandHelpers.SplitCommandAndArguments("!test");

            Assert.AreEqual("!test", result.Item1);
            Assert.AreEqual(string.Empty, result.Item2);
        }

        [Test]
        public void Test_SplitCommandAndArguments_WithForeignCommand_MultipleArguments()
        {
            var result = CommandHelpers.SplitCommandAndArguments("!test arg1|arg2");

            Assert.AreEqual("!test", result.Item1);
            Assert.AreEqual("arg1|arg2", result.Item2);
        }

        [Test]
        public void Test_SplitDirectEntryArguments_EmptyString()
        {
            var result = CommandHelpers.SplitDirectEntryArguments(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void Test_SplitDirectEntryArguments_SingleArgument()
        {
            var result = CommandHelpers.SplitDirectEntryArguments(CommandHelpers.CollapseDirectEntryCommandArguments(ListOperations.Create("arg1"))).ToList();

            Assert.AreEqual("arg1", result[0]);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void Test_SplitDirectEntryArguments_MultipleArguments()
        {
            var result = CommandHelpers.SplitDirectEntryArguments(CommandHelpers.CollapseDirectEntryCommandArguments(ListOperations.Create("arg1", "arg2", "arg3"))).ToList();

            Assert.AreEqual("arg1", result[0]);
            Assert.AreEqual("arg2", result[1]);
            Assert.AreEqual("arg3", result[2]);

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Test_SplitForeignArguments_EmptyString()
        {
            var result = CommandHelpers.SplitForeignArguments(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void Test_SplitForeignArguments_SingleArgument()
        {
            var result = CommandHelpers.SplitForeignArguments(CommandHelpers.CollapseForeignCommandArguments(ListOperations.Create("arg1"))).ToList();

            Assert.AreEqual("arg1", result[0]);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void Test_SplitForeignArguments_MultipleArguments()
        {
            var result = CommandHelpers.SplitForeignArguments(CommandHelpers.CollapseForeignCommandArguments(ListOperations.Create("arg1", "arg2", "arg3"))).ToList();

            Assert.AreEqual("arg1", result[0]);
            Assert.AreEqual("arg2", result[1]);
            Assert.AreEqual("arg3", result[2]);

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Test_StripTargetTypeFromForeignCommand_WithAll()
        {
            TargetType targetType;
            var result = CommandHelpers.StripTargetTypeFromForeignCommand(CommandHelpers.CreateForeignCommandText("test", "arg1"), out targetType);

            Assert.AreEqual("test arg1", result);
            Assert.AreEqual(TargetType.All, targetType);
        }

        #endregion

        #region Convert To Foreign

        [Test]
        public void Test_ConvertToForeignCommandText_WhenRedoxFellow_NoArguments()
        {
            var cmdName = "test";
            var rfCommand = Command.CreateRedoxFellow(string.Empty, cmdName);

            var convertedCmdText = CommandHelpers.ConvertToForeignCommandText(rfCommand);

            Assert.AreEqual("!test", convertedCmdText);
        }

        [Test]
        public void Test_ConvertToForeignCommandText_WhenRedoxFellow_WithArgumentsAndAdditional()
        {
            var cmdName = "test";
            var args = ListOperations.Create("arg1", "arg2");
            var rfCommand = Command.CreateRedoxFellow(string.Empty, cmdName, args);

            var convertedCmdText = CommandHelpers.ConvertToForeignCommandText(rfCommand, "arg3", "arg4");

            Assert.AreEqual("!test arg1|arg2|arg3|arg4", convertedCmdText);
        }

        [Test]
        public void Test_ConvertToForeignCommandText_WhenRedoxFellow_UsingSlaveOnlyTargetting()
        {
            var cmdName = "test";
            var rfCommand = Command.CreateRedoxFellow(string.Empty, cmdName);

            var convertedCmdText = CommandHelpers.ConvertToForeignCommandText(rfCommand, TargetType.SlavesOnly);

            Assert.AreEqual("#test", convertedCmdText);
        }

        #endregion
    }
}
