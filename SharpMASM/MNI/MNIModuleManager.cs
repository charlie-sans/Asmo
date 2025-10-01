using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpMASM.MNI;
using static SharpMASM.Core_Interpreter;

namespace SharpMASM.MNI
{
    public class MNIModuleManager
    {
        private static MNIModuleManager _instance;
        private readonly Dictionary<string, Dictionary<string, MethodInfo>> _modules;
        private readonly MappedMemoryFile _memory;

        private MNIModuleManager(MappedMemoryFile memory)
        {
            _modules = new Dictionary<string, Dictionary<string, MethodInfo>>();
            _memory = memory;

            // Load built-in modules
            LoadBuiltInModules();

            // Load external modules
            LoadExternalModules();
        }

        public static MNIModuleManager GetInstance(MappedMemoryFile memory = null)
        {
            if (_instance == null)
            {
                if (memory == null)
                {
                    memory = MappedMemoryFile.GetInstance(Common.MappedFile);
                }
                _instance = new MNIModuleManager(memory);
            }
            return _instance;
        }

        private void LoadBuiltInModules()
        {
            // Load all types in the current assembly
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            LoadModulesFromAssembly(currentAssembly);
        }

        private void LoadExternalModules()
        {
            try
            {
                // Look for modules in a "modules" directory next to the executable
                string modulesDirectory = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "root", "modules");

                if (Directory.Exists(modulesDirectory))
                {
                    foreach (string dllFile in Directory.GetFiles(modulesDirectory, "*.dll"))
                    {
                        try
                        {
                            Assembly assembly = Assembly.LoadFrom(dllFile);
                            LoadModulesFromAssembly(assembly);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Failed to load module from {dllFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading external modules: {ex.Message}");
            }
        }

        private void LoadModulesFromAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                MNIClassAttribute classAttr = type.GetCustomAttribute<MNIClassAttribute>();
                if (classAttr != null)
                {
                    string moduleName = classAttr.ModuleName;

                    // Create module dictionary if it doesn't exist
                    if (!_modules.ContainsKey(moduleName))
                    {
                        _modules[moduleName] = new Dictionary<string, MethodInfo>();
                    }

                    // Find all MNI functions in this class
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        MNIFunctionAttribute funcAttr = method.GetCustomAttribute<MNIFunctionAttribute>();
                        if (funcAttr != null && funcAttr.Module == moduleName)
                        {
                            _modules[moduleName][funcAttr.Name] = method;
                        }
                        if (CmdArgs.GetInstance().VeryVerbose)
                        {
                            Console.WriteLine($"Loaded MNI function {moduleName}.{funcAttr.Name}");
                        }
                    }
                }
            }
        }

        public bool ExecuteMNIFunction(instruction instruction)
        {
            // Format expected: MNI ModuleName.functionName arg1 arg2 arg3...
            if (instruction.args.Length < 1) return false;

            string[] parts = instruction.args[0].Split('.');
            if (parts.Length != 2) return false;

            string moduleName = parts[0];
            string functionName = parts[1];

            // Check if module and function exist
            if (!_modules.ContainsKey(moduleName) ||
                !_modules[moduleName].ContainsKey(functionName))
            {
                Console.Error.WriteLine($"MNI function not found: {moduleName}.{functionName}");
                return false;
            }

            try
            {
                // Create method object
                MNIMethodObject methodObj = new MNIMethodObject(_memory);

                // Set arguments
                if (instruction.args.Length > 1) methodObj.arg1 = instruction.args[1];
                if (instruction.args.Length > 2) methodObj.arg2 = instruction.args[2];
                if (instruction.args.Length > 3) methodObj.arg3 = instruction.args[3];
                if (instruction.args.Length > 4) methodObj.arg4 = instruction.args[4];

                // Execute method
                MethodInfo method = _modules[moduleName][functionName];
                method.Invoke(null, new object[] { methodObj });

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing MNI function {moduleName}.{functionName}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
}
