//-----------------------------------------------------------------------------
// FILE:        MarkupPropertyParser.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Supports parsing and execution of markup extension references embedded within
    /// a markup extension's property value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unforunately, Xamarin.Forms does not support setting the property value of one
    /// markup extension by referencing another extension.  This support would be very
    /// useful for situations where property values need to be computed using parameters
    /// (e.g. setting a grid column width using the font, size, and the text to be displayed).
    /// </para>
    /// <para>
    /// Nested markup extensions are referenced just like they would be in a XAML element,
    /// using the brace syntax:
    /// </para>
    /// <example>
    /// {Static Foo.Bar ...}
    /// </example>
    /// <para>
    /// where <b>x</b> specifies the extension namespace, <b>Static</b> identifies the extension,
    /// and <b>Foo.Bar</b> is the extension parameter.
    /// </para>
    /// <para>
    /// This class provides a way to support the behavior in custom markup extensions via
    /// a few reasonable hacks.  First, you'll need to code your extension such that the 
    /// properties allowing embedded references are defined as the <c>string</c> type.
    /// Then in your <see cref="IMarkupExtension.ProvideValue"/> method, you'll need to
    /// call <see cref="Parse{T}(string, IServiceProvider)"/> to obtain the actual property value, passing 
    /// the raw/unparsed property value.
    /// </para>
    /// <para>
    /// Finally, you'll need to register your namespaces in code by calling the <see cref="RegisterXmlnsMapping(string, string)"/>
    /// method for each custom namespace, passing the namespace identifier and the related .NET namespace.  
    /// Note that the <b>x</b> namespace is automatically mapped to the <b>Xamarin.Forms.Xaml</b> assembly.  
    /// Most portable applications will perform this initialization within the app's static constructor: <c>static</c> <b>App()</b>.
    /// </para>
    /// <note type="note">
    /// This class assumes that all application pages use the same namespace mappings and
    /// that these pages also use the <b>x</b> namespace to reference the built-in Xamarin
    /// extensions.  This is a bit fragile but not far from typical practices.
    /// </note>
    /// <note type="note">
    /// <para>
    /// <see cref="Parse{T}(string, IServiceProvider)"/> currently supports only the following result types:
    /// </para>
    /// <list type="bullet">
    ///     <item><c>double</c></item>
    ///     <item><c>int</c></item>
    ///     <item><c>bool</c></item>
    ///     <item><c>string</c></item>
    ///     <item><see cref="Thickness"/></item>
    /// </list>
    /// </note>
    /// <note type="note">
    /// Nested markup extension references are not supported.
    /// </note>
    /// </remarks>
    public static class MarkupPropertyParser
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Describes an XML namespace mapping.
        /// </summary>
        private class NamespaceInfo
        {
            /// <summary>
            /// Returns the mapped .NET namespace.
            /// </summary>
            public readonly string DotNetNamespace;

            /// <summary>
            /// Returns the mapped .NET assembly.
            /// </summary>
            public readonly Assembly Assembly;

            /// <summary>
            /// Constructs an instance based on a XAML namespace reference.
            /// </summary>
            /// <param name="namespaceRef">The namespace reference.</param>
            public NamespaceInfo(string namespaceRef)
            {
                // Parse the namespace reference to extract the .NET namespace and
                // the assembly name.

                const string NamespaceFormatMessage = "Invalid or unsupported XAML namespace reference.";

                if (string.IsNullOrEmpty(namespaceRef) || !namespaceRef.StartsWith("clr-namespace:"))
                {
                    throw new FormatException(NamespaceFormatMessage);
                }

                string      assemblyName;
                int         pos;
                int         posEnd;

                pos    = "clr-namespace:".Length;
                posEnd = namespaceRef.IndexOf(";assembly=", pos);

                if (posEnd < 0)
                {
                    throw new FormatException(NamespaceFormatMessage);
                }

                DotNetNamespace = namespaceRef.Substring(pos, posEnd - pos);

                pos = posEnd + ";assembly=".Length;

                assemblyName = namespaceRef.Substring(pos);

                if (DotNetNamespace.Length == 0 || assemblyName.Length == 0)
                {
                    throw new FormatException(NamespaceFormatMessage);
                }

                // Load the assembly.

                Assembly = Assembly.Load(new AssemblyName(assemblyName));
            }
        }

        /// <summary>
        /// Holds cached information about a markup extension type.
        /// </summary>
        private class ExtensionInfo
        {
            /// <summary>
            /// Returns the extension type.
            /// </summary>
            public readonly Type Type;

            /// <summary>
            /// Returns the extension type property information keyed by name.
            /// </summary>
            public readonly Dictionary<string, PropertyInfo> Properties;

            /// <summary>
            /// Returns the extensions default content property or <c>null</c>.
            /// </summary>
            public readonly PropertyInfo ContentProperty;

            /// <summary>
            /// Returns the markup extension's <see cref="IMarkupExtension.ProvideValue(IServiceProvider)"/> method.
            /// </summary>
            public readonly MethodInfo ProvideValueMethod;

            /// <summary>
            /// Captures the important information about an extension type.
            /// </summary>
            /// <param name="type">The extension type.</param>
            public ExtensionInfo(Type type)
            {
                if (!typeof(IMarkupExtension).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                {
                    throw new TypeLoadException($"Type [{type.FullName}] does not implement [IMarkupExtension].");
                }

                this.Type = type;

                var contentPropertyAttribute = type.GetTypeInfo().GetCustomAttribute<ContentPropertyAttribute>();

                if (contentPropertyAttribute != null)
                {
                    ContentProperty = type.GetRuntimeProperty(contentPropertyAttribute.Name);
                }

                this.Properties = new Dictionary<string, PropertyInfo>(InitialDictionaryItems);

                foreach (var property in type.GetRuntimeProperties())
                {
                    if (!property.IsSpecialName)
                    {
                        this.Properties.Add(property.Name, property);
                    }
                }

                this.ProvideValueMethod = type.GetRuntimeMethod("ProvideValue", new Type[] { typeof(IServiceProvider) });
            }

            /// <summary>
            /// Calls the markup extension, passing parsed arguments.
            /// </summary>
            /// <param name="serviceProvider">The markup service provider.</param>
            /// <param name="args">The parsed arguments.</param>
            /// <returns>The value provided by the extension.</returns>
            public object ProvideValue(IServiceProvider serviceProvider, Dictionary<string, string> args)
            {
                var extensionInstance = Activator.CreateInstance(this.Type);

                foreach (var arg in args)
                {
                    PropertyInfo property;

                    if (string.IsNullOrEmpty(arg.Key))
                    {
                        if (this.ContentProperty == null)
                        {
                            throw new FormatException($"Type [{this.Type.FullName}] does not have a [ContentProperty] attribute so all property assignments must explicitly specify the propert name.");
                        }

                        property = this.ContentProperty;
                    }
                    else if (!Properties.TryGetValue(arg.Key, out property))
                    {
                        throw new FormatException($"Type [{this.Type.FullName}] does not define the [{arg.Key}] property.");
                    }

                    property.SetValue(extensionInstance, arg.Value);
                }

                return this.ProvideValueMethod.Invoke(extensionInstance, new object[] { serviceProvider });
            }
        }

        //---------------------------------------------------------------------
        // Implementation

        // Note: 
        //
        // We don't need to support concurrency since it will only be called
        // on the UI thread (because that's where XAML parsing happens).

        private const string    InvalidMarkupMessage   = "Improperly formatted markup extension reference.";
        private const int       InitialDictionaryItems = 10;

        private static Dictionary<string, NamespaceInfo>    nameToNamespaceInfo;
        private static Dictionary<string, ExtensionInfo>    extensionCache;
        private static char[]                               spaceSep           = new char[] { ' ', '\t' };
        private static char[]                               spaceBraceSep      = new char[] { ' ', '\t', '}' };
        private static char[]                               spaceCommaBraceSep = new char[] { ' ', '\t', ',', '}' };
        private static char[]                               equalCommaBraceSep = new char[] { '=', ',', '}' };

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MarkupPropertyParser()
        {
            // Create the dictionaries.

            nameToNamespaceInfo = new Dictionary<string, NamespaceInfo>(InitialDictionaryItems);
            extensionCache      = new Dictionary<string, ExtensionInfo>(InitialDictionaryItems);

            // Initialize the built-in namespaces.

            RegisterXmlnsMapping("",  "clr-namespace:Xamarin.Forms.Xaml;assembly=Xamarin.Forms.Xaml");
            RegisterXmlnsMapping("x", "clr-namespace:Xamarin.Forms.Xaml;assembly=Xamarin.Forms.Xaml");
        }

        /// <summary>
        /// Globally registers an XAML namespace mapping to a .NET namespace.
        /// </summary>
        /// <param name="name">The XAML namespace name.</param>
        /// <param name="namespaceRef">Indentifies the mapped .NET namespace and assembly.</param>
        /// <remarks>
        /// <para>
        /// The <paramref name="namespaceRef"/> parameter should be formatted just as an
        /// <b>xmlns</b> reference would be in the application's XAML pages.  For example:
        /// </para>
        /// <code language="c#">
        /// MarkupPropertyParser.RegisterXmlnsMapping("c", "clr-namespace:MyLib.Common;assembly=MyLib.Common");
        /// </code>
        /// <para>
        /// maps the XAML namespace <b>"c"</b> to the .NET namespace <b>"MyLib.Common</b> and the 
        /// .NET assembly <b>MyLib.Common.dll</b>.
        /// </para>
        /// <para>
        /// Namespace mappings must be registered early during the application initialization,
        /// before any XAML pages that include references to markup extensions that will need
        /// to make references to the assemblies.  Typically, applications will do this in
        /// the portable application's static constructor: <c>static</c> <b>App()</b>.
        /// </para>
        /// <para>
        /// The namespace mappings must match those defined in the application's XAML pages.
        /// This is a bit fragile but I don't see a way around this without being able to
        /// modify the Xamarin source code.
        /// </para>
        /// </remarks>
        public static void RegisterXmlnsMapping(string name, string namespaceRef)
        {
            // $hack(jeff.lill):
            //
            // It may be possible to avoid this hack by using a Xamarin.Forms service
            // provider in the [Parse()] method below.  It seems likely that Xamarin
            // would have providers that provide services to markup extensions during
            // parsing and perhaps runtime as well.
            //
            // Hopefully a provider exists that exposes the XAML namespace mappings 
            // and is public.

            nameToNamespaceInfo[name] = new NamespaceInfo(namespaceRef);
        }

        /// <summary>
        /// Parses a property value that potentially includes a reference to a markup extension.
        /// </summary>
        /// <typeparam name="T">The desired property result type.</typeparam>
        /// <param name="value">The property value string to be parsed.</param>
        /// <param name="serviceProvider">The markup service provide.</param>
        /// <returns>
        /// <para>
        /// The parsed value.
        /// </para>
        /// <note type="note">
        /// The result returns as type <c>object</c>.  You'll need to cast this explictly
        /// into type <typeparamref name="T"/>.
        /// </note>
        /// </returns>
        /// <exception cref="FormatException">Thrown if the value could not be parsed.</exception>
        /// <exception cref="TypeLoadException">Thrown if the namespace ID or type name is not valid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not supported.</exception>
        /// <remarks>
        /// <para>
        /// <see cref="Parse{T}(string, IServiceProvider)"/> currently supports only the following result types:
        /// </para>
        /// <list type="bullet">
        ///     <item><c>double</c></item>
        ///     <item><c>int</c></item>
        ///     <item><c>bool</c></item>
        ///     <item><c>string</c></item>
        ///     <item><see cref="Thickness"/></item>
        /// </list>
        /// <note type="note">
        /// Nested markup extension references are not supported.
        /// </note>
        /// </remarks>
        public static object Parse<T>(string value, IServiceProvider serviceProvider)
        {
            // $todo(jeff.lill):
            //
            // I realized that I don't really need to handle parsing of single-quoted 
            // strings below since we're not supported nesting of more than one markup
            // extension.  This code shouldn't hurt anything, but I may want to remove
            // it sometime in the future.

            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }

            var type = typeof(T);

            // Handle values that don't include a markup extension reference.

            if (value[0] != '{')
            {
                if (type == typeof(double))
                {
                    return double.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (type == typeof(int))
                {
                    return int.Parse(value);
                }
                else if (type == typeof(bool))
                {
                    return bool.Parse(value);
                }
                else if (type == typeof(string))
                {
                    return value;
                }
                else if (type == typeof(Thickness))
                {
                    var fields = value.Split(spaceSep);

                    switch (fields.Length)
                    {
                        case 1:

                            return new Thickness(double.Parse(fields[0], CultureInfo.InvariantCulture));

                        case 2:

                            return new Thickness(double.Parse(fields[0], CultureInfo.InvariantCulture),
                                                 double.Parse(fields[1], CultureInfo.InvariantCulture));

                        case 4:

                            return new Thickness(double.Parse(fields[0], CultureInfo.InvariantCulture),
                                                 double.Parse(fields[1], CultureInfo.InvariantCulture),
                                                 double.Parse(fields[2], CultureInfo.InvariantCulture),
                                                 double.Parse(fields[3], CultureInfo.InvariantCulture));

                        default:

                            throw new FormatException($"[Thickness] class requires 0, 1, 2, or 4 size values, not [{fields.Length}]");
                    }
                }
                else
                {
                    throw new NotSupportedException($"[MarkupPropertyParser] does not support parsing the [{type.FullName}] type.");
                }
            }

            //-----------------------------------------------------------------
            // Parse the markup extension reference.

            string  extensionName;
            int     pos;
            int     posEnd;
            var     args = new Dictionary<string, string>(InitialDictionaryItems);

            if (value[value.Length - 1] != '}')
            {
                throw new FormatException(InvalidMarkupMessage);
            }

            // Parse the extension name.

            pos    = 1;
            posEnd = value.IndexOfAny(spaceBraceSep);

            if (posEnd < 0)
            {
                throw new FormatException(InvalidMarkupMessage);
            }

            extensionName = value.Substring(pos, posEnd - pos);

            // Parse the arguments.  These will look like:
            //
            //      <value>
            // or:  '<value>'
            // or:  <name>=<value>
            // or:  <name>='<value>'
            //
            // and will be separated by commas.

            if (value[posEnd] != '}')
            {
                pos = posEnd + 1;

                while (true)
                {
                    // Skip over any whitespace.

                    while (char.IsWhiteSpace(value[pos]))
                    {
                        pos++;
                    }

                    if (value[pos] == '}')
                    {
                        break;
                    }
                    else if (value[pos] == '\'')
                    {
                        // Parse the default property surrounded by single quotes.

                        pos++;
                        posEnd = value.IndexOf('\'', pos);

                        if (posEnd < 0)
                        {
                            throw new FormatException(InvalidMarkupMessage);
                        }

                        args[string.Empty] = value.Substring(pos, posEnd);

                        pos = posEnd + 1;
                    }
                    else
                    {
                        // Scan forward until we see an '=', comma or a closing brace.

                        posEnd = value.IndexOfAny(equalCommaBraceSep, pos);

                        if (posEnd < 0)
                        {
                            throw new FormatException(InvalidMarkupMessage);
                        }

                        switch (value[posEnd])
                        {
                            case '=':

                                // We have:
                                //
                                //      <name>=<value>
                                // or:  <name>='<value>'

                                var argName = value.Substring(pos, posEnd - pos);

                                pos = posEnd + 1;

                                if (value[pos] == '\'')
                                {
                                    // Quoted value

                                    pos++;
                                    posEnd = value.IndexOf('\'', pos);

                                    if (posEnd < 0)
                                    {
                                        throw new FormatException(InvalidMarkupMessage);
                                    }

                                    args[argName] = value.Substring(pos, posEnd - pos);

                                    pos = posEnd + 1;
                                }
                                else
                                {
                                    // Unquoted value

                                    posEnd = value.IndexOfAny(spaceCommaBraceSep, pos);

                                    if (posEnd < 0)
                                    {
                                        throw new FormatException(InvalidMarkupMessage);
                                    }

                                    args[argName] = value.Substring(pos, posEnd - pos).Trim();
                                }
                                break;

                            case ',':
                            case '}':

                                // We have the default property formatted as:
                                //
                                //      <value>
                                // or:  '<value>'

                                if (value[pos] == '\'')
                                {
                                    // Quoted value.

                                    pos++;
                                    posEnd = value.IndexOf('\'', pos);

                                    if (posEnd < 0)
                                    {
                                        throw new FormatException(InvalidMarkupMessage);
                                    }

                                    args[string.Empty] = value.Substring(pos, posEnd - pos);

                                    pos = posEnd + 1;
                                }
                                else
                                {
                                    // Unquoted value.

                                    posEnd = value.IndexOfAny(spaceCommaBraceSep, pos);

                                    if (posEnd < 0)
                                    {
                                        throw new FormatException(InvalidMarkupMessage);
                                    }

                                    args[string.Empty] = value.Substring(pos, posEnd - pos).Trim();
                                }

                                break;

                            default:

                                throw new FormatException(InvalidMarkupMessage);
                        }
                    }

                    if (value[pos] != ',')
                    {
                        break;
                    }
                }
            }

            //-----------------------------------------------------------------
            // Locate and the extension type.  Note that we're going to cache type 
            // references for better performance.

            string              namespaceName;
            NamespaceInfo       namespaceInfo;
            string              extensionTypeName;
            ExtensionInfo       extensionInfo;

            pos = extensionName.IndexOf(':');

            if (pos <= 0)
            {
                // Extension name doesn't have a namespace prefix.

                namespaceName = string.Empty;
            }
            else
            {
                namespaceName = extensionName.Substring(0, pos);
                extensionName = extensionName.Substring(pos + 1);
            }

            if (!nameToNamespaceInfo.TryGetValue(namespaceName, out namespaceInfo))
            {
                throw new TypeLoadException($"Namespace prefix [{namespaceName}] was not mapped via a previous call to [MarkupPropertyParser.RegisterXmlnsMapping()].");
            }

            extensionTypeName = namespaceInfo.DotNetNamespace + "." + extensionName + "Extension";

            if (!extensionCache.TryGetValue(extensionTypeName, out extensionInfo))
            {
                try
                {
                    var extensionType = namespaceInfo.Assembly.GetType(extensionTypeName);

                    if (extensionType == null)
                    {
                        throw new Exception();
                    }

                    extensionInfo                     = new ExtensionInfo(extensionType);
                    extensionCache[extensionTypeName] = extensionInfo;
                }
                catch (Exception e)
                {
                    throw new TypeLoadException($"Cannot locate markup extension type [{extensionTypeName}].", e);
                }
            }

            //-----------------------------------------------------------------
            // Call the extension.

            return (T)extensionInfo.ProvideValue(serviceProvider, args);
        }
    }
}
