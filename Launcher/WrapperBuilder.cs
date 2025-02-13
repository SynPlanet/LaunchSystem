using System;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;

namespace Launcher
{
    class WrapperBuilder
    {
        #region Singleton
        static WrapperBuilder instance;
        public static WrapperBuilder Instance
        {
            get
            {
                if (instance == null)
                    instance = new WrapperBuilder(InterfaceCollection.Instance.GetInterfaces());
                return instance;
            }
        }
        #endregion

        List<Item> items;
        private WrapperBuilder(Type[] interfaces)
        {
            items = new List<Item>();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Item item = CreateItem(interfaces[i]);
                if (item != null)
                    items.Add(item);
            }
        }

        private Item CreateItem(Type type)
        {
            LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.WrapperBuilder_Message0_P1, type.FullName));
            Item item = null;
            string className = type.FullName.Replace('.', '_');
            Type wrapper = CreateType(CreateSource(type, className), className);
            if (wrapper != null) item = new Item(type.FullName, wrapper);
            try { return item; }
            finally { if (item != null)LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.WrapperBuilder_Message1_P1, type.FullName)); }
        }

        private string CreateSource(Type type, string className)
        {
            StringBuilder str = new StringBuilder();
            str.Append("namespace ").AppendLine(this.GetType().Namespace);
            str.AppendLine("{");
            str.Append("\tclass ").Append(className).Append(" : ").Append(typeof(IWrapper).FullName).Append(", ").AppendLine(type.FullName);
            str.AppendLine("\t{");
            str.Append("\t\t").Append(type.FullName).AppendLine(" instance;");
            str.Append("\t\tpublic ").Append(className).AppendLine("()");
            str.AppendLine("\t\t{");
            str.Append("\t\t\tWrapperName = typeof(").Append(typeof(IWrapper).FullName).AppendLine(").FullName;");
            str.AppendLine("\t\t}");

            
            // ReflectInterface(typeof(IWrapper), ref str);
            str.AppendLine("\t\tpublic void Add(object obj)");
            str.AppendLine("\t\t{");
            str.Append("\t\t\tinstance = (").Append(type.FullName).AppendLine(")obj;");
            str.AppendLine("\t\t}");
            str.AppendLine("\t\tpublic object Instance { get { return this; } }");
            str.AppendLine("\t\tpublic string WrapperName { get; private set; }");

            
            StringBuilder implementation = new StringBuilder();
            CreateImplementation(type, ref implementation);
            str.Append(implementation.ToString());

            str.AppendLine("\t}");
            str.AppendLine("}");
            return str.ToString();
        }

        private void CreateImplementation(Type type, ref StringBuilder implement)
        {
            /* get all members from base type */
            MemberInfo[] members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in members) {
                implement.Append(CreateMember(item));
            }
            ReflectionImplement(type.GetInterfaces(), ref implement);
        }

        void ReflectionImplement(Type[] interfaces, ref StringBuilder implement)
        {
            /* get all inherited interfaces and implement them */
            if (interfaces != null) {
                foreach (var inter in interfaces)
                    ReflectInterface(inter, ref implement);
            }
        }

        void ReflectInterface(Type tInterface, ref StringBuilder implement)
        {
            MemberInfo[] members = tInterface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var item in members) {
                implement.Append(CreateMember(item));
            }
        }


        private string CreateMember(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Method && ((MethodBase)member).Attributes.HasFlag(MethodAttributes.SpecialName))
                return string.Empty;
            StringBuilder str = new StringBuilder();
            switch (member.MemberType)
            {
                case MemberTypes.Method:
                    str.Append(CreateMethod((MethodInfo)member));
                    break;
                case MemberTypes.Property:
                    str.Append(CreateProperty((PropertyInfo)member));
                    break;
            }
            return str.ToString();
        }

        private string CreateProperty(PropertyInfo property)
        {
            StringBuilder str = new StringBuilder();
            str.Append("\t\tpublic ").Append(property.PropertyType.ToString()).Append(" ").AppendLine(property.Name);
            str.AppendLine("\t\t{");
            if (property.CanRead)
            {
                str.AppendLine("\t\t\tget");
                str.AppendLine("\t\t\t{");
                str.Append("\t\t\t\treturn instance.").Append(property.Name).AppendLine(";");
                str.AppendLine("\t\t\t}");
            }
            if (property.CanWrite)
            {
                str.AppendLine("\t\t\tset");
                str.AppendLine("\t\t\t{");
                str.Append("\t\t\t\tinstance.").Append(property.Name).AppendLine(" = value;");
                str.AppendLine("\t\t\t}");
            }
            str.AppendLine("\t\t}");
            return str.ToString();
        }

        private string CreateMethod(MethodInfo method)
        {
            StringBuilder str = new StringBuilder();
            str.Append("\t\tpublic ");
            if (method.ReturnType.ToString() != "System.Void")
                str.Append(method.ReturnType.ToString());
            else
                str.Append("void");
            str.Append(" ").Append(method.Name).Append("(");
            
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    str.Append(", ");
                if (parameters[i].ParameterType.IsByRef)
                {
                    if (parameters[i].IsOut)
                        str.Append("out ");
                    else
                        str.Append("ref ");
                }
                str.Append(parameters[i].ParameterType.ToString().Replace("&", "")).Append(" ").Append(parameters[i].Name);
            }
            str.AppendLine(")");
            str.AppendLine("\t\t{");
            str.Append("\t\t\t");
            if (method.ReturnType.ToString() != "System.Void")
                str.Append("return ");
            str.Append("instance.").Append(method.Name).Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    str.Append(", ");
                if (parameters[i].ParameterType.IsByRef)
                {
                    if (parameters[i].IsOut)
                        str.Append("out ");
                    else
                        str.Append("ref ");
                }
                str.Append(parameters[i].Name);
            }
            str.AppendLine(");");
            str.AppendLine("\t\t}");
            return str.ToString();
        }

        private Type CreateType(string source, string className)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = true;
            parameters.ReferencedAssemblies.Add("TypeLib.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.Xaml.dll");
            parameters.ReferencedAssemblies.Add("System.Drawing.dll");
            parameters.ReferencedAssemblies.Add(@"WPF\WindowsBase.dll");
            parameters.ReferencedAssemblies.Add(@"WPF\PresentationFramework.dll");
            parameters.ReferencedAssemblies.Add(@"WPF\PresentationCore.dll");
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, source);
            if (results.Errors.Count > 0)
            {
                LogManager.Instance.Launcher.Exception(LogLevel.Basic, GetErrors(results.Errors.GetEnumerator()), Local.Message.WrapperBuilder_Message2);
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

        private Exception GetErrors(System.Collections.IEnumerator errors)
        {
            Exception ex;
            if (errors.MoveNext()) ex = new Exception(((CompilerError)errors.Current).ErrorText, GetErrors(errors));
            else ex = null;
            return ex;
        }

        public IWrapper GetWrapper(string name)
        {
            Item item = items.Find(x => x.Name == name);
            if (item == null)
                return null;
            return (IWrapper)Activator.CreateInstance(item.Type);
        }

        class Item
        {
            public Item(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public Type Type { get; private set; }

            public string Name { get; private set; }
        }
    }
}
