using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Tests.Core
{
    public class TestDiscoveryService
    {
        public List<TestBase> DiscoverTests(Assembly assembly)
        {
            return DiscoverTests(new[] { assembly });
        }

        public List<TestBase> DiscoverTests(params Assembly[] assemblies)
        {
            var tests = new List<TestBase>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(TestBase).IsAssignableFrom(t));

                foreach (var type in types)
                {
                    var testInstance = CreateTestInstance(type);
                    if (testInstance != null)
                    {
                        tests.Add(testInstance);
                    }
                }
            }

            return tests;
        }

        private TestBase? CreateTestInstance(Type testType)
        {
            try
            {
                var constructors = testType.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .ToList();

                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    var parameterValues = new object[parameters.Length];

                    bool canCreate = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.HasDefaultValue)
                        {
                            parameterValues[i] = param.DefaultValue!;
                        }
                        else if (param.ParameterType.IsValueType)
                        {
                            parameterValues[i] = Activator.CreateInstance(param.ParameterType)!;
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            parameterValues[i] = string.Empty;
                        }
                        else
                        {
                            parameterValues[i] = null!;
                        }
                    }

                    if (canCreate)
                    {
                        try
                        {
                            return (TestBase)Activator.CreateInstance(testType, parameterValues)!;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
