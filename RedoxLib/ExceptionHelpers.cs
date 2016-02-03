using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RedoxLib
{
    public static class ExceptionHelpers
    {
        /// <summary>
        /// This attribute, applied to a member of an exception type,
        /// causes that member to be displayed as part of the stack
        /// trace output.
        /// </summary>
        public class IncludeInTraceAttribute : Attribute
        {
        }

        /// <summary>
        /// Returns a string giving information about the exception in question, and
        /// any inner exceptions, all with stack traces.
        /// </summary>
        public static string FullExceptionTrace(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            addStackTraceForNonAggregateException(sb, exception);
            string stackTrace = sb.ToString();
            return stackTrace;
        }

        /// <summary>
        /// Returns a string giving information about the exception in question, and
        /// any inner exceptions, all with stack traces.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static string ToFullStackTrace(this Exception exception)
        {
            return FullExceptionTrace(exception);
        }

        private static void addStackTraceForNonAggregateException(StringBuilder sb, Exception exception)
        {
            List<Exception> exceptions = new List<Exception>();
            for (Exception ecursor = exception; ecursor != null; ecursor = ecursor.InnerException)
            {
                exceptions.Add(ecursor);
            }
            exceptions.Reverse();

            bool isFirst = true;
            foreach (Exception ecursor in exceptions)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append("--- ");
                addInfoForException(sb, ecursor);
            }
        }

        private static void addInfoForException(StringBuilder sb, Exception exception)
        {
            sb.AppendLine(String.Format("{0}: {1}", exception.GetType(), exception.Message));
            if (exception is ReflectionTypeLoadException)
            {
                getDebugByExceptionType(sb, exception as ReflectionTypeLoadException);
            }
            else
            {
                // extract public fields and properties with the IncludeInTrace attribue
                foreach (FieldInfo field in exception.GetType().GetFields())
                    if (field.GetCustomAttributes(typeof(IncludeInTraceAttribute), true).Length > 0)
                        sb.AppendLine(String.Format("{0}: {1}", field.Name, field.GetValue(exception)));

                foreach (PropertyInfo prop in exception.GetType().GetProperties())
                    if (prop.GetCustomAttributes(typeof(IncludeInTraceAttribute), true).Length > 0)
                        sb.AppendLine(String.Format("{0}: {1}", prop.Name, prop.GetValue(exception, null)));

                sb.AppendLine(exception.StackTrace);
            }
        }

        /// <summary>
        /// Pull debug information out of the exception object - specialized
        /// for the ReflectionTypeLoadException class
        /// </summary>
        private static void getDebugByExceptionType(StringBuilder debugString, ReflectionTypeLoadException exception)
        {
            debugString.AppendLine("Detail:");
            foreach (Exception e in exception.LoaderExceptions)
            {
                debugString.Append("  ");
                debugString.AppendLine(e.Message);
            }
            debugString.AppendLine("Call stack:");
            debugString.AppendLine(exception.StackTrace);
        }

        /// <summary>
        /// Attempts to extract the first "interesting" stack frame, and return it.  
        /// The parameters are hints as to the namespaces or method names that we 
        /// know are not interesting.
        /// </summary>
        /// <remarks>I think this still needs a little work to get the right answer.</remarks>
        public static StackFrame FirstInterestingFrame(uint skipFrames, params string[] namespacesAndMethodsToIgnore)
        {
            StackTrace trace = new StackTrace(true);
            // i=1 => start with the caller's stack frame
            for (int i = 1 + (int)skipFrames; i < trace.FrameCount; ++i)
            {
                StackFrame frame = trace.GetFrame(i);
                bool firstInterestingFrame = true;
                string thisMethodNamespace = frame.GetMethod().DeclaringType.Namespace;
                if (namespacesAndMethodsToIgnore != null)
                {
                    foreach (string ns in namespacesAndMethodsToIgnore)
                    {
                        if (thisMethodNamespace == ns)
                        {
                            firstInterestingFrame = false;
                        }

                        // also allow skipping of methods by method name
                        if (frame.GetMethod().Name == ns)
                        {
                            firstInterestingFrame = false;
                        }
                    }
                }

                // we always skip things in System
                if (thisMethodNamespace == "System" || thisMethodNamespace.StartsWith("System."))
                {
                    firstInterestingFrame = false;
                }

                if (firstInterestingFrame)
                {
                    return frame;
                }
            }

            // if we got here, it's because we ignored all the frames, so just return the frame 
            // for our caller.
            if (1 + skipFrames < trace.FrameCount)
            {
                return trace.GetFrame(1 + (int)skipFrames);
            }
            else
            {
                // skipFrames was too large.  We're only writing diagnostics though, so there's
                // probably a more important error going on somewhere else.
                return trace.GetFrame(1);
            }
        }

        /// <summary>
        /// Given a stack frame, returns a string suitable describing the location, for use in
        /// error messages.  This will be the file and line number if available, or the name
        /// of the method if debug information is not available.
        /// </summary>
        public static string MessageOriginFromStackFrame(StackFrame frame)
        {
            StringBuilder message = new StringBuilder();
            if (frame.GetFileName() != null)
            {
                message.Append(frame.GetFileName());
                if (frame.GetFileLineNumber() != 0)
                {
                    message.AppendFormat("({0}", frame.GetFileLineNumber());
                    if (frame.GetFileColumnNumber() != 0)
                    {
                        message.AppendFormat(",{0})", frame.GetFileColumnNumber());
                    }
                    else
                    {
                        message.Append(")");
                    }
                }
            }
            else
            {
                message.AppendFormat("{0}.{1}", frame.GetMethod().DeclaringType.FullName, frame.GetMethod().Name);
            }
            return message.ToString();
        }
    }
}
