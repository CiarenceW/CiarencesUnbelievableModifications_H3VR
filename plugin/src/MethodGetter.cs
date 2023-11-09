using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;

#pragma warning disable

//ask Szikaka on discord for permission to use

public class MethodSignature
{
    static readonly Dictionary<string, Type> primitive_types_lookup = new Dictionary<string, Type>() {
        {"void", typeof(void)},
        {"bool", typeof(bool)},
        {"byte", typeof(byte)},
        {"sbyte", typeof(sbyte)},
        {"char", typeof(char)},
        {"decimal", typeof(decimal)},
        {"double", typeof(double)},
        {"float", typeof(float)},
        {"int", typeof(int)},
        {"uint", typeof(uint)},
        {"nint", typeof(nint)},
        {"nuint", typeof(nuint)},
        {"long", typeof(long)},
        {"ulong", typeof(ulong)},
        {"short", typeof(short)},
        {"ushort", typeof(ushort)},
        {"object", typeof(object)},
        {"string", typeof(string)},
    };

    public BindingFlags binding_flags { get; }
    public Type? return_type { get; }
    public Type enclosing_type { get; }
    public string method_name { get; }
    public string[] method_arguments { get; }

    private MethodSignature(BindingFlags flags, Type return_type, Type enclosing_type, string name, string[] arguments)
    {
        this.binding_flags = flags;
        this.return_type = return_type;
        this.enclosing_type = enclosing_type;
        this.method_name = name;
        this.method_arguments = arguments;
    }

    private static Type? GetTypeFromAppDomain(string typename_at_assembly)
    {
        string[] temp = typename_at_assembly.Split('@');

        return GetTypeFromAppDomain(temp[0], temp.Length > 1 ? temp[1] : "");
    }

    private static Type? GetTypeFromAppDomain(string typename, string assembly_name)
    {
        assembly_name = assembly_name.Trim();
        if (assembly_name.StartsWith("@"))
        {
            assembly_name = assembly_name.Substring(1);
        }

        Assembly method_assembly = null;

        Type enclosing_type = null;

        try
        {
            if (!string.IsNullOrEmpty(assembly_name))
            {
                method_assembly = Assembly.Load(assembly_name);
            }

            if (method_assembly != null)
            {
                enclosing_type = method_assembly.GetType(typename);
            }
            else
            {
                foreach (var code_assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    enclosing_type = code_assembly.GetType(typename);

                    if (enclosing_type != null) break;
                }
            }
        }
        catch { }


        return enclosing_type;
    }

    private static string UnmangleGenericName(string generic_name)
    {
        int split_index = generic_name.IndexOf('`');

        if (split_index <= 0)
        {
            return generic_name;
        }

        string type_name = generic_name.Substring(0, split_index);

        int args_length = generic_name[split_index + 1] - '0';

        string type_parameter_part = generic_name.Substring(generic_name.IndexOf('[') + 1, generic_name.LastIndexOf(']') - generic_name.IndexOf('[') - 1);

        return type_name + "<" + UnmangleGenericName(type_parameter_part) + ">";
    }

    private static string ExpandTypenames(string parameter_string)
    {
        bool ref_flag = false;

        Match ref_match = Regex.Match(parameter_string, @"^(ref|out)\s+");

        if (ref_match.Success)
        {
            ref_flag = true;

            parameter_string = parameter_string.Remove(0, ref_match.Length);
        }

        int startIndex = 0;
        Match separator_match = Regex.Match(parameter_string, @"[<>,]");

        if (!separator_match.Success)
        {
            string parameter_short_name = parameter_string.Split(' ')[0];

            if (primitive_types_lookup.ContainsKey(parameter_short_name))
            {
                return primitive_types_lookup[parameter_short_name].ToString() + (ref_flag ? "&" : "");
            }
            return parameter_short_name;
        }

        StringBuilder return_string_builder = new StringBuilder();

        do
        {
            string arg_string = parameter_string.Substring(startIndex, separator_match.Index - startIndex);

            if (primitive_types_lookup.ContainsKey(arg_string))
            {
                return_string_builder.Append(primitive_types_lookup[arg_string]);
            }
            else
            {
                return_string_builder.Append(arg_string);
            }

            return_string_builder.Append(parameter_string[separator_match.Index]);

            startIndex = separator_match.Index + 1;

            separator_match = separator_match.NextMatch();
        } while (separator_match.Success);

        return return_string_builder.ToString();
    }

    private static string[] GetArgumentTypes(string argument_string)
    {
        List<string> actual_arguments = new List<string>() { "" };

        int depth = 0;
        int index = 0;
        foreach (char ch in argument_string)
        {
            if (ch == ',' && depth == 0)
            {
                actual_arguments.Add("");

                index++;
            }
            else
            {
                if (ch == '<')
                {
                    depth++;
                }
                else if (ch == '>')
                {
                    depth--;
                }
                else if (depth > 0 && char.IsWhiteSpace(ch))
                {
                    continue;
                }

                actual_arguments[index] += ch;
            }
        }

        return actual_arguments.Select(arg => ExpandTypenames(arg.Trim())).Where(arg => !string.IsNullOrEmpty(arg)).ToArray();
    }

    public static MethodSignature Parse(string string_signature)
    {
        string[] method_signature_components = string_signature.Trim().Split('(', ')');

        string[] method_modifiers_and_type_dot_name = method_signature_components[0].Split(' ');

        BindingFlags flags = BindingFlags.Default;

        if (method_modifiers_and_type_dot_name.Contains("public"))
        {
            flags |= BindingFlags.Public;
        }
        else if (method_modifiers_and_type_dot_name.Contains("private") || method_modifiers_and_type_dot_name.Contains("protected"))
        {
            flags |= BindingFlags.NonPublic;
        }
        else
        {
            flags |= BindingFlags.Public | BindingFlags.NonPublic;
        }

        if (method_modifiers_and_type_dot_name.Contains("static"))
        {
            flags |= BindingFlags.Static;
        }
        else
        {
            flags |= BindingFlags.Instance;
        }

        Type return_type = null;

        if (method_modifiers_and_type_dot_name.Length > 1)
        {
            string return_type_name = method_modifiers_and_type_dot_name[method_modifiers_and_type_dot_name.Length - 2];

            if (primitive_types_lookup.ContainsKey(return_type_name))
            {
                return_type = primitive_types_lookup[return_type_name];
            }
            else
            {
                var temp_type = GetTypeFromAppDomain(return_type_name);

                if (temp_type != null)
                {
                    return_type = temp_type;
                }
            }
        }

        string type_dot_name = method_modifiers_and_type_dot_name.Last();

        Type enclosing_type = GetTypeFromAppDomain(
            type_dot_name.Substring(0, type_dot_name.LastIndexOf('.')),
            method_signature_components.Last()
        );

        return new MethodSignature(
            flags,
            return_type,
            enclosing_type,
            type_dot_name.Substring(type_dot_name.LastIndexOf('.') + 1),
            GetArgumentTypes(method_signature_components[1])
        );
    }

    public static MethodInfo? FindMethod(string method_signature)
    {
        return Parse(method_signature).FindMethod();
    }

    public static MethodInfo? FindMethod(Type targetClass, string targetMethod, string args = "", BindingFlags flags = BindingFlags.Default, Type returnType = null)
    {
        if (returnType == null) returnType = typeof(void);
        return new MethodSignature(flags, returnType, targetClass, targetMethod, GetArgumentTypes(args.Trim('(', ')'))).FindMethod();
    }

    public MethodInfo? FindMethod()
    {
        List<MethodInfo> compatible_methods = new List<MethodInfo>();

        foreach (var method in this.enclosing_type.GetMethods(this.binding_flags))
        {
            if (method.Name == this.method_name)
            {
                compatible_methods.Add(method);
            }
        }

        if (compatible_methods.Count > 0)
        {
            if (compatible_methods.Count == 1)
            {
                return compatible_methods[0];
            }

            foreach (var method in compatible_methods)
            {
                string[] compatible_method_params = method.GetParameters().Select(param => MethodSignature.UnmangleGenericName(param.ParameterType.ToString())).ToArray();

                if (compatible_method_params.Length == this.method_arguments.Length)
                {
                    bool flag = true;

                    for (int i = 0; i < this.method_arguments.Length; i++)
                    {
                        if (compatible_method_params[i] != method_arguments[i])
                        {
                            flag = false;

                            break;
                        }
                    }

                    if (flag)
                    {
                        return method;
                    }
                }
            }
        }

        return null;
    }
}
