using System;
using System.Diagnostics;
using System.Reflection;

namespace Funcky.Monads
{
    internal static class ExceptionUtilities
    {
        private static readonly FieldInfo StackTraceField = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Type TraceFromatType = Type.GetType("System.Diagnostics.StackTrace").GetNestedType("TraceFormat", BindingFlags.NonPublic);
        private static readonly MethodInfo TraceToStringMethodInfo = typeof(StackTrace).GetMethod("ToString", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { TraceFromatType }, null);

        public static Exception SetStackTrace(this Exception target, StackTrace stack)
        {
            var getStackTraceString = TraceToStringMethodInfo.Invoke(stack, new[] { Enum.GetValues(TraceFromatType).GetValue(0) });
            StackTraceField.SetValue(target, getStackTraceString);
            return target;
        }
    }
}