using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Launcher.Settings;
using Microsoft.CSharp;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;

namespace Launcher
{
    class CreatorDynamicLibrary : MarshalByRefObject
    {
        delegate IntPtr GetProcMethod(IntPtr memoryModulePTR, string name);
        delegate void MemoryFreeLibrary(IntPtr memoryModulePTR);
        public object GetInstance(IUnmanagedCode code)
        {
            LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.CreatorDynamicLibrary_Message0_P1, code.Path));
            if (code.CanDebug)
            {
                LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.CreatorDynamicLibrary_Message1_P1, code.Path));
                CreatorUnmanagedProxyIsAtached creator = new CreatorUnmanagedProxyIsAtached();
                Type type = creator.CreateType(code);
                return Activator.CreateInstance(type);
            }
            else
            {
                IntPtr modulePtr = Unmanaged.UnmanagedLoader.MemoryLoadLibrary(code.Path);
                var methods = typeof(Unmanaged.UnmanagedLoader).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
                var getProcMethod = methods.FirstOrDefault(x => x.Name == "GetProcMethod" && x.IsGenericMethod == false);
                var memoryFreeLibrary = methods.FirstOrDefault(x => x.Name == "MemoryFreeLibrary" && x.IsGenericMethod == false);
                CreatorUnmanagedProxy creator = new CreatorUnmanagedProxy();
                Type type = creator.CreateType(code);

                var members = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Static);
                var getProcMethodD = members.FirstOrDefault(x => x.Name == "GetProcAddress");
                var memoryFreeLibraryD = members.FirstOrDefault(x => x.Name == "MemoryFreeLibrary");
                IntPtr ProcMethodPtr = Marshal.GetFunctionPointerForDelegate(Delegate.CreateDelegate((Type)getProcMethodD, getProcMethod));
                IntPtr MemoryFreePtr = Marshal.GetFunctionPointerForDelegate(Delegate.CreateDelegate((Type)memoryFreeLibraryD, memoryFreeLibrary));
                var constructor = type.GetConstructor(new Type[] { typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) });
                object obj = constructor.Invoke(new object[] { modulePtr, ProcMethodPtr, MemoryFreePtr });
                return obj;
            }
        }

        class CreatorUnmanagedProxyIsAtached
        {
            List<string> typeNames;

            public CreatorUnmanagedProxyIsAtached()
            {
                typeNames = new List<string>();
            }

            public Type CreateType(IUnmanagedCode unmanagedCode)
            {
                string name = GetClassName(unmanagedCode.Path);
                if (typeNames.Find(x => x == name) != null)
                    return null;
                List<IMemberCreator> creators = new List<IMemberCreator>();
                List<Type> types = new List<Type>();
                foreach (var item in unmanagedCode.AvailableInterfaces)
                {
                    Type type = InterfaceCollection.Instance.GetInterface(item);
                    if (type == null)
                        continue;
                    types.Add(type);
                    FillIMemberCreator(creators, type, unmanagedCode.Path);
                }
                FillIMemberCreator(creators, typeof(IConnected), unmanagedCode.Path);
                types.Add(typeof(IConnected));
                Type returnType = CreateType(CreateSource(creators, types, name), name);
                if (returnType != null)
                    typeNames.Add(name);
                return returnType;
            }

            private Type CreateType(string source, string className)
            {
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                parameters.GenerateExecutable = false;
                parameters.GenerateInMemory = true;
                parameters.ReferencedAssemblies.Add("TypeLib.dll");
                parameters.ReferencedAssemblies.Add("System.dll");
                CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, source);
                if (results.Errors.Count > 0)
                {
                    return null;
                }
                Type[] types = results.CompiledAssembly.GetTypes();
                foreach (var item in types)
                {
                    if (item.Name == className)
                        return item;
                }
                return null;
            }

            private void FillIMemberCreator(List<IMemberCreator> creators, Type type, string path)
            {
                MemberInfo[] members = type.GetMembers();
                foreach (var member in members)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Method:
                            CreateMethod(creators, (MethodInfo)member, path);
                            break;
                        case MemberTypes.Property:
                            CreateProperty(creators, (PropertyInfo)member, path);
                            break;
                    }
                }
            }

            private string CreateSource(List<IMemberCreator> creators, List<Type> types, string className)
            {
                StringBuilder str = new StringBuilder();
                str.AppendLine("using System;");
                str.AppendLine("using System.Runtime.ExceptionServices;");
                str.AppendLine("using System.Runtime.InteropServices;");
                str.Append("namespace ").AppendLine(this.GetType().Namespace);
                str.AppendLine("{");
                str.Append("\tpublic class ").Append(className).Append(" : ").Append(typeof(MarshalByRefObject).FullName);
                foreach (var item in types)
                    str.Append(", ").Append(item.FullName);
                str.AppendLine();
                str.AppendLine("\t{");
                str.AppendLine("\t\tstatic class StaticWrapper");
                str.AppendLine("\t\t{");
                foreach (var item in creators)
                {
                    str.AppendLine(item.StaticMember);
                }
                str.AppendLine("\t\t}");
                foreach (var item in creators)
                {
                    str.AppendLine(item.InterfaceMember);
                }
                str.AppendLine("\t}");
                str.AppendLine("}");
                return str.ToString();
            }

            private void CreateProperty(List<IMemberCreator> creators, PropertyInfo property, string path)
            {
                PropertyCreator creator = new PropertyCreator(property, path);
                foreach (var item in creators)
                {
                    if (item.Compare(creator))
                        return;
                }
                creators.Add(creator);
            }

            private void CreateMethod(List<IMemberCreator> creators, MethodInfo method, string path)
            {
                if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
                    return;
                MethodCreator creator = new MethodCreator(method, path);
                foreach (var item in creators)
                {
                    if (item.Compare(creator))
                        return;
                }
                creators.Add(creator);
            }

            private string GetClassName(string path)
            {
                string name = path;
                int index = path.LastIndexOf('\\');
                if (index != -1)
                    name = path.Remove(0, index + 1);
                name = name.Replace('.', '_');
                return name;
            }

            interface IMemberCreator
            {
                string StaticMember { get; }

                string InterfaceMember { get; }

                bool Compare(IMemberCreator member);
            }

            class PropertyCreator : IMemberCreator
            {
                bool canRead;
                bool canWrite;
                string name;
                string interfaceHeader;
                string interfaceRead;
                string interfaceWrite;
                string staticRead;
                string staticWrite;

                public PropertyCreator(PropertyInfo property, string dllPath)
                {
                    canRead = property.CanRead;
                    canWrite = property.CanWrite;
                    name = property.Name;
                    StringBuilder str = new StringBuilder();
                    str.Append("\t\tpublic ").Append(property.PropertyType.ToString()).Append(" ").Append(property.Name);
                    interfaceHeader = str.ToString();
                    if (property.CanRead)
                    {
                        str = new StringBuilder();
                        str.AppendLine("\t\t\t[HandleProcessCorruptedStateExceptions]");
                        str.AppendLine("\t\t\tget");
                        str.AppendLine("\t\t\t{");
                        str.Append("\t\t\t\ttry { return StaticWrapper.get_").Append(property.Name).AppendLine("(); }");
                        str.AppendLine("\t\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                        str.AppendLine("\t\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                        str.Append("\t\t\t}");
                        interfaceRead = str.ToString();
                        str = new StringBuilder();
                        str.Append("\t\t\t[DllImport(@\"").Append(dllPath).AppendLine("\", CallingConvention = CallingConvention.Cdecl)]");
                        if (property.PropertyType == typeof(string))
                            str.AppendLine("\t\t\t[return: MarshalAs(UnmanagedType.LPStr)]");
                        str.Append("\t\t\tpublic static extern ").Append(property.PropertyType.ToString()).Append(" get_").Append(property.Name).Append("();");
                        staticRead = str.ToString();
                    }
                    if (property.CanWrite)
                    {
                        str = new StringBuilder();
                        str.AppendLine("\t\t\t[HandleProcessCorruptedStateExceptions]");
                        str.AppendLine("\t\t\tset");
                        str.AppendLine("\t\t\t{");
                        str.Append("\t\t\t\ttry { StaticWrapper.put_").Append(property.Name).AppendLine("(value); }");
                        str.AppendLine("\t\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                        str.AppendLine("\t\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                        str.Append("\t\t\t}");
                        interfaceWrite = str.ToString();

                        str = new StringBuilder();
                        str.Append("\t\t\t[DllImport(@\"").Append(dllPath).AppendLine("\", CallingConvention = CallingConvention.Cdecl)]");
                        str.Append("\t\t\tpublic static extern void put_").Append(property.Name).Append("(");
                        if (property.PropertyType == typeof(string))
                            str.AppendLine("[MarshalAs(UnmanagedType.LPStr)] ");
                        str.Append(property.PropertyType.ToString()).Append(" value);");
                        staticWrite = str.ToString();
                    }
                    CreateMember();
                }

                private void CreateMember()
                {
                    StringBuilder str = new StringBuilder();
                    if (canRead)
                        str.AppendLine(staticRead);
                    if (canWrite)
                        str.AppendLine(staticWrite);
                    StaticMember = str.ToString();

                    str = new StringBuilder();
                    str.AppendLine(interfaceHeader).AppendLine("\t\t{");
                    if (canRead)
                        str.AppendLine(interfaceRead);
                    if (canWrite)
                        str.AppendLine(interfaceWrite);
                    str.AppendLine("\t\t}");
                    InterfaceMember = str.ToString();
                }


                public string StaticMember { get; private set; }

                public string InterfaceMember { get; private set; }

                public bool Compare(IMemberCreator member)
                {
                    PropertyCreator creator = member as PropertyCreator;
                    if (creator == null)
                        return false;
                    if (creator.name == name)
                    {
                        if (creator.canRead && creator.canRead != canRead)
                        {
                            canRead = true;
                            interfaceRead = creator.interfaceRead;
                            staticRead = creator.staticRead;
                        }
                        if (creator.canWrite && creator.canWrite != canWrite)
                        {
                            canWrite = true;
                            interfaceWrite = creator.interfaceWrite;
                            staticWrite = creator.staticWrite;
                        }
                        CreateMember();
                        return true;
                    }
                    return false;
                }
            }

            class MethodCreator : IMemberCreator
            {
                public MethodCreator(MethodInfo method, string dllPath)
                {
                    bool isReturn = false;
                    StringBuilder inputParameters = new StringBuilder();
                    StringBuilder str = new StringBuilder();
                    if (method.ReturnType.ToString() != "System.Void")
                    {
                        isReturn = true;
                        if (method.ReturnType == typeof(string))
                            str.Append("[return: MarshalAs(UnmanagedType.LPStr)]");
                        str.Append(method.ReturnType.ToString());
                    }
                    else
                        str.Append("void");
                    str.Append(" ").Append(method.Name).Append("(");

                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i != 0)
                        {
                            str.Append(", ");
                            inputParameters.Append(", ");
                        }
                        if (parameters[i].ParameterType == typeof(string))
                            str.Append("[MarshalAs(UnmanagedType.LPStr)] ");
                        if (parameters[i].ParameterType.IsByRef)
                        {
                            if (parameters[i].IsOut)
                            {
                                str.Append("out ");
                                inputParameters.Append("out ");
                            }
                            else
                            {
                                str.Append("ref ");
                                inputParameters.Append("ref ");
                            }
                        }
                        str.Append(parameters[i].ParameterType.ToString().Replace("&", "")).Append(" ").Append(parameters[i].Name);
                        inputParameters.Append(parameters[i].Name);
                    }
                    str.Append(")");
                    str.ToString();

                    StringBuilder temp = new StringBuilder();
                    temp.Append("\t\t\t[DllImport(@\"").Append(dllPath).AppendLine("\", CallingConvention = CallingConvention.Cdecl)]");
                    temp.Append("\t\t\tpublic static extern ").Append(str).Append(";");
                    StaticMember = temp.ToString();
                    temp = new StringBuilder();
                    temp.AppendLine("\t\t[HandleProcessCorruptedStateExceptions]");
                    temp.Append("\t\tpublic ").Append(str).AppendLine();
                    temp.AppendLine("\t\t{");
                    temp.Append("\t\t\ttry { ");

                    if (isReturn)
                        temp.Append("return ");
                    temp.Append("StaticWrapper.").Append(method.Name).Append("(").Append(inputParameters).AppendLine("); }");
                    temp.AppendLine("\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                    temp.AppendLine("\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                    temp.Append("\t\t}");
                    InterfaceMember = temp.ToString();
                }

                public string StaticMember { get; private set; }

                public string InterfaceMember { get; private set; }

                public bool Compare(IMemberCreator member)
                {
                    MethodCreator creator = member as MethodCreator;
                    if (creator == null)
                        return false;
                    if (creator.StaticMember == StaticMember)
                        return true;
                    else
                        return false;
                }
            }
        }

        class CreatorUnmanagedProxy
        {
            List<string> typeNames;

            public CreatorUnmanagedProxy()
            {
                typeNames = new List<string>();
            }

            public Type CreateType(IUnmanagedCode unmanagedCode)
            {
                string name = GetClassName(unmanagedCode.Path);
                if (typeNames.Find(x => x == name) != null)
                    return null;
                List<IMemberCreator> creators = new List<IMemberCreator>();
                List<Type> types = new List<Type>();
                foreach (var item in unmanagedCode.AvailableInterfaces)
                {
                    Type type = InterfaceCollection.Instance.GetInterface(item);
                    if (type == null)
                        continue;
                    types.Add(type);
                    FillIMemberCreator(creators, type, unmanagedCode.Path);
                }
                FillIMemberCreator(creators, typeof(IConnected), unmanagedCode.Path);
                types.Add(typeof(IConnected));
                Type returnType = CreateType(CreateSource(creators, types, name), name);
                if (returnType != null)
                    typeNames.Add(name);
                return returnType;
            }

            private Type CreateType(string source, string className)
            {
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                parameters.GenerateExecutable = false;
                parameters.GenerateInMemory = true;
                parameters.ReferencedAssemblies.Add("TypeLib.dll");
                parameters.ReferencedAssemblies.Add("System.dll");
                CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, source);
                if (results.Errors.Count > 0)
                {
                    return null;
                }
                Type[] types = results.CompiledAssembly.GetTypes();
                foreach (var item in types)
                {
                    if (item.Name == className)
                        return item;
                }
                return null;
            }

            private void FillIMemberCreator(List<IMemberCreator> creators, Type type, string path)
            {
                MemberInfo[] members = type.GetMembers();
                foreach (var member in members)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Method:
                            CreateMethod(creators, (MethodInfo)member, path);
                            break;
                        case MemberTypes.Property:
                            CreateProperty(creators, (PropertyInfo)member, path);
                            break;
                    }
                }
            }
            private delegate void MemoryFreeLibrary(IntPtr memoryModulePTR);
            private string CreateSource(List<IMemberCreator> creators, List<Type> types, string className)
            {
                StringBuilder str = new StringBuilder();
                str.AppendLine("using System;");
                str.AppendLine("using System.Runtime.ExceptionServices;");
                str.AppendLine("using System.Runtime.InteropServices;");
                str.Append("namespace ").AppendLine(this.GetType().Namespace);
                str.AppendLine("{");
                str.Append("\tpublic class ").Append(className).Append(" : ").Append(typeof(MarshalByRefObject).FullName).Append(", ").Append(typeof(IDisposable).FullName);
                foreach (var item in types)
                    str.Append(", ").Append(item.FullName);
                str.AppendLine();
                str.AppendLine("\t{");
                str.AppendLine("\t\tstatic class StaticWrapper");
                str.AppendLine("\t\t{");
                foreach (var item in creators) str.AppendLine(item.StaticMember);
                str.AppendLine("\t\t}");
                str.AppendLine("\t\tprivate delegate IntPtr GetProcAddress(IntPtr memoryModulePTR, string name);");
                str.AppendLine("\t\tprivate delegate void MemoryFreeLibrary(IntPtr memoryModulePTR);");
                str.AppendLine("\t\t");
                foreach (var item in creators) str.AppendLine(item.DelegateDefinition);
                str.AppendLine("\t\t");
                str.AppendLine("\t\tprivate MemoryFreeLibrary memoryFreeLibrary = null;");
                str.AppendLine("\t\tprivate IntPtr memoryLibraryPTR;");
                str.AppendLine("\t\t");
                str.Append("\t\tpublic ").Append(className).AppendLine("(IntPtr memoryLibraryPTR, IntPtr getProcMethodPTR, IntPtr memoryFreeLibraryPTR)");
                str.AppendLine("\t\t{");
                str.AppendLine("\t\t\tIntPtr methodPtr;");
                str.AppendLine("\t\t\tthis.memoryLibraryPTR = memoryLibraryPTR;");
                str.AppendLine("\t\t\tGetProcAddress getProcAddress = (GetProcAddress)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(getProcMethodPTR, typeof(GetProcAddress));");
                str.AppendLine("\t\t\tmemoryFreeLibrary = (MemoryFreeLibrary)System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(memoryFreeLibraryPTR, typeof(MemoryFreeLibrary));");
                str.AppendLine("\t\t\t");
                foreach (var item in creators) str.AppendLine(item.DelegateInitialize);
                str.AppendLine("\t\t}");
                foreach (var item in creators) str.AppendLine(item.InterfaceMember);
                str.AppendLine("\t\tpublic void Dispose()");
                str.AppendLine("\t\t{");
                str.AppendLine("\t\t\tmemoryFreeLibrary(memoryLibraryPTR);");
                str.AppendLine("\t\t}");
                str.AppendLine("\t}");
                str.AppendLine("}");
                return str.ToString();
            }

            private void CreateProperty(List<IMemberCreator> creators, PropertyInfo property, string path)
            {
                PropertyCreator creator = new PropertyCreator(property, path);
                foreach (var item in creators)
                {
                    if (item.Compare(creator))
                        return;
                }
                creators.Add(creator);
            }

            private void CreateMethod(List<IMemberCreator> creators, MethodInfo method, string path)
            {
                if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
                    return;
                MethodCreator creator = new MethodCreator(method, path);
                foreach (var item in creators)
                {
                    if (item.Compare(creator))
                        return;
                }
                creators.Add(creator);
            }

            private string GetClassName(string path)
            {
                string name = path;
                int index = path.LastIndexOf('\\');
                if (index != -1)
                    name = path.Remove(0, index + 1);
                name = name.Replace('.', '_');
                return name;
            }

            interface IMemberCreator
            {
                string DelegateInitialize { get; }

                string DelegateDefinition { get; }

                string StaticMember { get; }

                string InterfaceMember { get; }

                bool Compare(IMemberCreator member);
            }

            class PropertyCreator : IMemberCreator
            {
                bool canRead;
                bool canWrite;
                string name;
                string interfaceRead;
                string interfaceWrite;
                string staticRead;
                string staticWrite;

                public PropertyCreator(PropertyInfo property, string dllPath)
                {
                    canRead = property.CanRead;
                    canWrite = property.CanWrite;
                    name = property.Name;
                    InitializeDelegateDefinition(property);
                    InitializeStaticMember(property);
                    InitializeDelegateInitialize(property);
                    InitializeProperty(property);
                 
                }

                private void InitializeDelegateDefinition(PropertyInfo property)
                {
                    StringBuilder str = new StringBuilder();
                    if (property.CanRead)
                    {
                        if (property.PropertyType == typeof(string)) str.AppendLine("\t\t[return: MarshalAs(UnmanagedType.LPStr)]");
                        str.Append("\t\tprivate delegate ").Append(property.PropertyType.ToString()).Append(" get_").Append(property.Name).Append("_D();");
                    }
                    if (property.CanWrite)
                    {
                        if (property.CanRead) str.AppendLine();
                        str.Append("\t\tprivate delegate void put_").Append(property.Name).AppendLine("_D(");
                        if (property.PropertyType == typeof(string)) str.Append("[MarshalAs(UnmanagedType.LPStr)] ");
                        str.Append(property.PropertyType.ToString()).Append(" value);");
                    }
                    DelegateDefinition = str.ToString();
                }

                private void InitializeStaticMember(PropertyInfo property)
                {
                    StringBuilder str = new StringBuilder();
                    if (property.CanRead) str.Append("\t\t\tpublic static get_").Append(property.Name).Append("_D get_").Append(property.Name).Append(";");
                    if (property.CanWrite && property.CanRead) str.AppendLine();
                    if (property.CanWrite) str.Append("\t\t\tpublic static put_").Append(property.Name).Append("_D put_").Append(property.Name).Append(";");
                    StaticMember = str.ToString();
                }

                private void InitializeDelegateInitialize(PropertyInfo property)
                {
                    StringBuilder str = new StringBuilder();
                    if (property.CanRead)
                    {
                        str.Append("\t\t\tmethodPtr = getProcAddress(memoryLibraryPTR, \"get_").Append(property.Name).AppendLine("\");");
                        str.Append("\t\t\tif (methodPtr != IntPtr.Zero) ");
                        str.Append("StaticWrapper.get_").Append(property.Name).Append(" = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(").Append("methodPtr").Append(", typeof(get_").Append(property.Name).Append("_D)) as get_").Append(property.Name).Append("_D;");
                    }
                    if (property.CanWrite && property.CanRead) str.AppendLine();
                    if (property.CanWrite)
                    {
                        str.Append("\t\t\tmethodPtr = getProcAddress(memoryLibraryPTR, \"put_").Append(property.Name).AppendLine("\");");
                        str.Append("\t\t\tif (methodPtr != IntPtr.Zero) ");
                        str.Append("StaticWrapper.put_").Append(property.Name).Append(" = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(").Append("methodPtr").Append(", typeof(put_").Append(property.Name).Append("_D)) as put_").Append(property.Name).Append("_D;");
                    }
                    DelegateInitialize = str.ToString();
                }

                private void InitializeProperty(PropertyInfo property)
                {
                    StringBuilder str = new StringBuilder();
                    str.Append("\t\tpublic ").Append(property.PropertyType.ToString()).Append(" ").AppendLine(property.Name);
                    str.AppendLine("\t\t{");
                    if (property.CanRead)
                    {
                        str.AppendLine("\t\t\t[HandleProcessCorruptedStateExceptions]");
                        str.AppendLine("\t\t\tget");
                        str.AppendLine("\t\t\t{");
                        str.Append("\t\t\t\ttry { return StaticWrapper.get_").Append(property.Name).AppendLine("(); }");
                        str.AppendLine("\t\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                        str.AppendLine("\t\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                        str.AppendLine("\t\t\t}");
                    }
                    if (property.CanWrite && property.CanRead) str.AppendLine();
                    str.AppendLine("\t\t}");
                    if (property.CanWrite)
                    {
                        str.AppendLine("\t\t\t[HandleProcessCorruptedStateExceptions]");
                        str.AppendLine("\t\t\tset");
                        str.AppendLine("\t\t\t{");
                        str.Append("\t\t\t\ttry { StaticWrapper.put_").Append(property.Name).AppendLine("(value); }");
                        str.AppendLine("\t\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                        str.AppendLine("\t\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                        str.AppendLine("\t\t\t}");
                    }
                    InterfaceMember = str.ToString();
                }
                
                public string DelegateInitialize { get; private set; }

                public string DelegateDefinition { get; private set; }

                public string StaticMember { get; private set; }

                public string InterfaceMember { get; private set; }

                public bool Compare(IMemberCreator member)
                {
                    PropertyCreator creator = member as PropertyCreator;
                    if (creator == null)
                        return false;
                    if (creator.name == name)
                    {
                        if (creator.canRead && creator.canRead != canRead)
                        {
                            canRead = true;
                            interfaceRead = creator.interfaceRead;
                            staticRead = creator.staticRead;
                        }
                        if (creator.canWrite && creator.canWrite != canWrite)
                        {
                            canWrite = true;
                            interfaceWrite = creator.interfaceWrite;
                            staticWrite = creator.staticWrite;
                        }
                        return true;
                    }
                    return false;
                }
                
            }

            class MethodCreator : IMemberCreator
            {
                public MethodCreator(MethodInfo method, string dllPath)
                {
                    StringBuilder str = new StringBuilder();
                    StringBuilder inputParameters = new StringBuilder();
                    ReadMethodDefinition(method, str, inputParameters);
                    InitializeDelegateDefinition(str);
                    InitializeStaticMember(method);
                    InitializeDelegateInitialize(method);
                    InitializeMethod(method);
                }

                private void ReadMethodDefinition(MethodInfo method, StringBuilder methodStr, StringBuilder inputParameters)
                {
                    if (method.ReturnType.ToString() != "System.Void")
                    {
                        if (method.ReturnType == typeof(string))
                            methodStr.Append("[return: MarshalAs(UnmanagedType.LPStr)]");
                        methodStr.Append(method.ReturnType.ToString());
                    }
                    else
                        methodStr.Append("void");
                    methodStr.Append(" ").Append(method.Name).Append("_D(");

                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i != 0)
                        {
                            methodStr.Append(", ");
                            inputParameters.Append(", ");
                        }
                        if (parameters[i].ParameterType == typeof(string))
                            methodStr.Append("[MarshalAs(UnmanagedType.LPStr)] ");
                        if (parameters[i].ParameterType.IsByRef)
                        {
                            if (parameters[i].IsOut)
                            {
                                methodStr.Append("out ");
                                inputParameters.Append("out ");
                            }
                            else
                            {
                                methodStr.Append("ref ");
                                inputParameters.Append("ref ");
                            }
                        }
                        methodStr.Append(parameters[i].ParameterType.ToString().Replace("&", "")).Append(" ").Append(parameters[i].Name);
                        inputParameters.Append(parameters[i].Name);
                    }
                    methodStr.Append(")");
                }

                private void InitializeDelegateDefinition(StringBuilder methodStr)
                {
                    StringBuilder str = new StringBuilder();
                    str.Append("\t\tprivate delegate ").Append(methodStr).Append(";");
                    DelegateDefinition = str.ToString();
                }

                private void InitializeStaticMember(MethodInfo method)
                {
                    StringBuilder str = new StringBuilder();
                    str.Append("\t\t\tpublic static ").Append(method.Name).Append("_D ").Append(method.Name).Append(";");
                    StaticMember = str.ToString();
                }

                private void InitializeDelegateInitialize(MethodInfo method)
                {
                    StringBuilder str = new StringBuilder();
                    str.Append("\t\t\tmethodPtr = getProcAddress(memoryLibraryPTR, \"").Append(method.Name).AppendLine("\");");
                    str.Append("\t\t\tif (methodPtr != IntPtr.Zero) ");
                    str.Append("StaticWrapper.").Append(method.Name).Append(" = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(").Append("methodPtr").Append(", typeof(").Append(method.Name).Append("_D)) as ").Append(method.Name).Append("_D;");
                    DelegateInitialize = str.ToString();
                }

                private void InitializeMethod(MethodInfo method)
                {
                    bool isReturn = method.ReturnType.ToString() != "System.Void";
                    StringBuilder str = new StringBuilder();
                    str.AppendLine("\t\t[HandleProcessCorruptedStateExceptions]");
                    str.Append("\t\tpublic ");
                    if (isReturn) str.Append(method.ReturnType.ToString());
                    else str.Append("void");
                    str.Append(" ").Append(method.Name).Append("(");
                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i != 0) str.Append(", ");
                        if (parameters[i].ParameterType.IsByRef)
                        {
                            if (parameters[i].IsOut) str.Append("out ");
                            else str.Append("ref ");
                        }
                        str.Append(parameters[i].ParameterType.ToString().Replace("&", "")).Append(" ").Append(parameters[i].Name);
                    }
                    str.AppendLine(")");
                    str.AppendLine("\t\t{");
                    str.Append("\t\t\ttry { ");
                    if (isReturn) str.Append("return ");
                    str.Append("StaticWrapper.").Append(method.Name).Append("(");
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i != 0) str.Append(", ");
                        if (parameters[i].ParameterType.IsByRef)
                        {
                            if (parameters[i].IsOut) str.Append("out ");
                            else str.Append("ref ");
                        }
                        str.Append(parameters[i].Name);
                    }
                    str.AppendLine("); }");
                    str.AppendLine("\t\t\tcatch (AccessViolationException ex) { throw new AccessViolationException(ex.Message); }");
                    str.AppendLine("\t\t\tcatch (Exception ex) { throw new Exception(ex.Message); }");
                    str.AppendLine("\t\t}");
                    InterfaceMember = str.ToString();
                }

                public string DelegateInitialize { get; private set; }

                public string DelegateDefinition { get; private set; }

                public string StaticMember { get; private set; }

                public string InterfaceMember { get; private set; }

                public bool Compare(IMemberCreator member)
                {
                    MethodCreator creator = member as MethodCreator;
                    if (creator == null)
                        return false;
                    if (creator.StaticMember == StaticMember)
                        return true;
                    else
                        return false;
                }
            }
        }
    }
}
