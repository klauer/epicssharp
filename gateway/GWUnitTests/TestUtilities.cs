using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GWUnitTests
{
    class TestUtilities
    {
        public static object RunMethod(object classObject, string method, object[] parameters)
        {
            return classObject.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(classObject, parameters);
        }
    }
}
