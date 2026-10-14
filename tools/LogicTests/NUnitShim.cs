// Minimal stand-in for NUnit so the *real* test files in Assets/_Game/Tests/EditMode can run
// under plain Mono (no Unity needed). Inside Unity the real NUnit package is used instead.
using System;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)] public class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute { }

    public class AssertionException : Exception
    {
        public AssertionException(string m) : base(m) { }
    }

    public static class Assert
    {
        private static void Fail(string msg, string extra)
        {
            throw new AssertionException(string.IsNullOrEmpty(extra) ? msg : msg + " — " + extra);
        }

        public static void AreEqual(object expected, object actual) { AreEqual(expected, actual, null); }
        public static void AreEqual(object expected, object actual, string message)
        {
            if (!object.Equals(expected, actual))
                Fail("Expected <" + expected + "> but was <" + actual + ">", message);
        }
        public static void AreEqual(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance)
                Fail("Expected <" + expected + "> but was <" + actual + ">", null);
        }
        public static void IsTrue(bool c) { IsTrue(c, null); }
        public static void IsTrue(bool c, string m) { if (!c) Fail("Expected true", m); }
        public static void IsFalse(bool c) { IsFalse(c, null); }
        public static void IsFalse(bool c, string m) { if (c) Fail("Expected false", m); }
        public static void IsNull(object o) { IsNull(o, null); }
        public static void IsNull(object o, string m) { if (o != null) Fail("Expected null", m); }
        public static void IsNotNull(object o) { IsNotNull(o, null); }
        public static void IsNotNull(object o, string m) { if (o == null) Fail("Expected not null", m); }
        public static void GreaterOrEqual(int a, int b) { if (a < b) Fail("Expected " + a + " >= " + b, null); }
        public static void Greater(int a, int b) { if (a <= b) Fail("Expected " + a + " > " + b, null); }
        public static void Less(float a, float b) { if (a >= b) Fail("Expected " + a + " < " + b, null); }
        public static void Less(int a, int b) { if (a >= b) Fail("Expected " + a + " < " + b, null); }
    }

    public static class StringAssert
    {
        public static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new AssertionException("Expected string containing <" + expected + "> but was <" + actual + ">");
        }
        public static void DoesNotContain(string expected, string actual)
        {
            if (actual != null && actual.IndexOf(expected, StringComparison.Ordinal) >= 0)
                throw new AssertionException("Expected string not containing <" + expected + "> but was <" + actual + ">");
        }
    }
}
