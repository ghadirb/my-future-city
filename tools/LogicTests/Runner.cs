using System;
using System.Reflection;
using NUnit.Framework;

public static class LogicTestRunner
{
    public static int Main()
    {
        int pass = 0, fail = 0;
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttributes(typeof(TestFixtureAttribute), false).Length == 0) continue;
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.GetCustomAttributes(typeof(TestAttribute), false).Length == 0) continue;
                string name = type.Name + "." + m.Name;
                try
                {
                    var inst = Activator.CreateInstance(type);
                    m.Invoke(inst, null);
                    pass++;
                    Console.WriteLine("PASS  " + name);
                }
                catch (TargetInvocationException tie)
                {
                    fail++;
                    Console.WriteLine("FAIL  " + name + "\n      " + tie.InnerException.GetType().Name + ": " + tie.InnerException.Message);
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine("Passed: " + pass + "  Failed: " + fail);
        return fail == 0 && pass > 0 ? 0 : 1;
    }
}
