using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTEJA_SemestralniPrace
{
    public class NativeFunctionManager
    {
        private Dictionary<string, NativeFunctionInfo> nativeFunctions;

        public NativeFunctionManager()
        {
            nativeFunctions = new Dictionary<string, NativeFunctionInfo>();
            RegisterBuiltInFunctions();
        }

        private void RegisterBuiltInFunctions()
        {
            // Vstup/výstup
            RegisterFunction("Put_Line", new[] { "String" }, null);
            RegisterFunction("Put", new[] { "String" }, null);
            RegisterFunction("Put_Integer", new[] { "Integer" }, null);
            RegisterFunction("Put_Real", new[] { "Real" }, null);
            RegisterFunction("New_Line", new string[] { }, null);
            RegisterFunction("Get_Line", new string[] { }, "String");

            // Konverze typů
            RegisterFunction("Integer_To_String", new[] { "Integer" }, "String");
            RegisterFunction("Real_To_String", new[] { "Real" }, "String");
            RegisterFunction("String_To_Integer", new[] { "String" }, "Integer");
            RegisterFunction("String_To_Real", new[] { "String" }, "Real");
            RegisterFunction("Integer_To_Real", new[] { "Integer" }, "Real");
            RegisterFunction("Real_To_Integer", new[] { "Real" }, "Integer");

            // Matematické funkce
            RegisterFunction("Sqrt", new[] { "Real" }, "Real");
            RegisterFunction("Abs_Integer", new[] { "Integer" }, "Integer");
            RegisterFunction("Abs_Real", new[] { "Real" }, "Real");
            RegisterFunction("Sin", new[] { "Real" }, "Real");
            RegisterFunction("Cos", new[] { "Real" }, "Real");
            RegisterFunction("Tan", new[] { "Real" }, "Real");
            RegisterFunction("Exp", new[] { "Real" }, "Real");
            RegisterFunction("Log", new[] { "Real" }, "Real");
            RegisterFunction("Power", new[] { "Real", "Real" }, "Real");

            // Řetězcové funkce
            RegisterFunction("Length", new[] { "String" }, "Integer");
            RegisterFunction("Substring", new[] { "String", "Integer", "Integer" }, "String");
            RegisterFunction("Concat", new[] { "String", "String" }, "String");
            RegisterFunction("To_Upper", new[] { "String" }, "String");
            RegisterFunction("To_Lower", new[] { "String" }, "String");

            // Náhodná čísla
            RegisterFunction("Random_Integer", new[] { "Integer", "Integer" }, "Integer");
            RegisterFunction("Random_Real", new string[] { }, "Real");
        }

        private void RegisterFunction(string name, string[] paramTypes, string returnType)
        {
            nativeFunctions[name] = new NativeFunctionInfo
            {
                Name = name,
                ParameterTypes = paramTypes,
                ReturnType = returnType
            };
        }

        public bool IsNativeFunction(string name)
        {
            return nativeFunctions.ContainsKey(name);
        }

        public NativeFunctionInfo GetFunctionInfo(string name)
        {
            return nativeFunctions.TryGetValue(name, out var info) ? info : null;
        }
    }

    public class NativeFunctionInfo
    {
        public string Name { get; set; }
        public string[] ParameterTypes { get; set; }
        public string ReturnType { get; set; }
    }
}
