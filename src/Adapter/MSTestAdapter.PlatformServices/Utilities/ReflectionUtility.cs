// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !WINDOWS_UWP

using System;
using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
using System.Collections;
using System.Collections.Generic;
using System.IO;
#endif
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Utilities;

/// <summary>
/// Utility for reflection API's.
/// </summary>
internal class ReflectionUtility
{
    /// <summary>
    /// Gets the custom attributes of the provided type on a memberInfo.
    /// </summary>
    /// <param name="attributeProvider"> The member to reflect on. </param>
    /// <param name="type"> The attribute type. </param>
    /// <returns> The vale of the custom attribute. </returns>
    [return: NotNullIfNotNull(nameof(attributeProvider))]
    internal virtual object[]? GetCustomAttributes(MemberInfo attributeProvider, Type type)
    {
        return GetCustomAttributes(attributeProvider, type, true);
    }

    /// <summary>
    /// Gets all the custom attributes adorned on a member.
    /// </summary>
    /// <param name="memberInfo"> The member. </param>
    /// <param name="inherit"> True to inspect the ancestors of element; otherwise, false. </param>
    /// <returns> The list of attributes on the member. Empty list if none found. </returns>
    [return: NotNullIfNotNull(nameof(memberInfo))]
    internal static object[]? GetCustomAttributes(MemberInfo memberInfo, bool inherit)
    {
        return GetCustomAttributes(memberInfo, type: null, inherit: inherit);
    }

    /// <summary>
    /// Get custom attributes on a member for both normal and reflection only load.
    /// </summary>
    /// <param name="memberInfo">Member for which attributes needs to be retrieved.</param>
    /// <param name="type">Type of attribute to retrieve.</param>
    /// <param name="inherit">If inherited type of attribute.</param>
    /// <returns>All attributes of give type on member.</returns>
    [return: NotNullIfNotNull(nameof(memberInfo))]
    internal static object[]? GetCustomAttributes(MemberInfo? memberInfo, Type? type, bool inherit)
    {
        if (memberInfo == null)
        {
            return null;
        }

#if NETFRAMEWORK
        bool shouldGetAllAttributes = type == null;

        if (!IsReflectionOnlyLoad(memberInfo))
        {
            if (shouldGetAllAttributes)
            {
                return memberInfo.GetCustomAttributes(inherit);
            }
            else
            {
                return memberInfo.GetCustomAttributes(type, inherit);
            }
        }
        else
        {
            List<object> nonUniqueAttributes = new();
            Dictionary<string, object> uniqueAttributes = new();

            var inheritanceThreshold = 10;
            var inheritanceLevel = 0;

            if (inherit && memberInfo.MemberType == MemberTypes.TypeInfo)
            {
                // This code is based on the code for fetching CustomAttributes in System.Reflection.CustomAttribute(RuntimeType type, RuntimeType caType, bool inherit)
                var tempTypeInfo = memberInfo as TypeInfo;

                do
                {
                    var attributes = CustomAttributeData.GetCustomAttributes(tempTypeInfo);
                    AddNewAttributes(
                        attributes,
                        shouldGetAllAttributes,
                        type!,
                        uniqueAttributes,
                        nonUniqueAttributes);
                    tempTypeInfo = tempTypeInfo!.BaseType?.GetTypeInfo();
                    inheritanceLevel++;
                }
                while (tempTypeInfo != null && tempTypeInfo != typeof(object).GetTypeInfo()
                       && inheritanceLevel < inheritanceThreshold);
            }
            else if (inherit && memberInfo.MemberType == MemberTypes.Method)
            {
                // This code is based on the code for fetching CustomAttributes in System.Reflection.CustomAttribute(RuntimeMethodInfo method, RuntimeType caType, bool inherit).
                var tempMethodInfo = memberInfo as MethodInfo;

                do
                {
                    var attributes = CustomAttributeData.GetCustomAttributes(tempMethodInfo);
                    AddNewAttributes(
                        attributes,
                        shouldGetAllAttributes,
                        type!,
                        uniqueAttributes,
                        nonUniqueAttributes);
                    var baseDefinition = tempMethodInfo!.GetBaseDefinition();

                    if (baseDefinition != null
                        && string.Equals(
                            string.Concat(tempMethodInfo.DeclaringType.FullName, tempMethodInfo.Name),
                            string.Concat(baseDefinition.DeclaringType.FullName, baseDefinition.Name)))
                    {
                        break;
                    }

                    tempMethodInfo = baseDefinition;
                    inheritanceLevel++;
                }
                while (tempMethodInfo != null && inheritanceLevel < inheritanceThreshold);
            }
            else
            {
                // Ideally we should not be reaching here. We only query for attributes on types/methods currently.
                // Return the attributes that CustomAttributeData returns in this cases not considering inheritance.
                var firstLevelAttributes =
                CustomAttributeData.GetCustomAttributes(memberInfo);
                AddNewAttributes(firstLevelAttributes, shouldGetAllAttributes, type!, uniqueAttributes, nonUniqueAttributes);
            }

            nonUniqueAttributes.AddRange(uniqueAttributes.Values);
            return nonUniqueAttributes.ToArray();
        }
#else
        if (type == null)
        {
            return memberInfo.GetCustomAttributes(inherit).ToArray();
        }
        else
        {
            return memberInfo.GetCustomAttributes(type, inherit).ToArray();
        }
#endif
    }

#if NETFRAMEWORK
    internal static object[] GetCustomAttributes(Assembly assembly, Type type)
    {
        if (!assembly.ReflectionOnly)
        {
            return assembly.GetCustomAttributes(type).ToArray();
        }

        List<CustomAttributeData> customAttributes = new();
        customAttributes.AddRange(CustomAttributeData.GetCustomAttributes(assembly));

        List<object> attributesArray = new();

        foreach (var attribute in customAttributes)
        {
            if (!IsTypeInheriting(attribute.Constructor.DeclaringType, type)
                    && !attribute.Constructor.DeclaringType.AssemblyQualifiedName.Equals(
                        type.AssemblyQualifiedName))
            {
                continue;
            }

            Attribute? attributeInstance = CreateAttributeInstance(attribute);
            if (attributeInstance != null)
            {
                attributesArray.Add(attributeInstance);
            }
        }

        return attributesArray.ToArray();
    }

    /// <summary>
    /// Create instance of the attribute for reflection only load.
    /// </summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <returns>An attribute.</returns>
    private static Attribute? CreateAttributeInstance(CustomAttributeData attributeData)
    {
        object? attribute = null;
        try
        {
            // Create instance of attribute. For some case, constructor param is returned as ReadOnlyCollection
            // instead of array. So convert it to array else constructor invoke will fail.
            Type attributeType = Type.GetType(attributeData.Constructor.DeclaringType.AssemblyQualifiedName);

            List<Type> constructorParameters = new();
            List<object> constructorArguments = new();
            foreach (var parameter in attributeData.ConstructorArguments)
            {
                Type parameterType = Type.GetType(parameter.ArgumentType.AssemblyQualifiedName);
                constructorParameters.Add(parameterType);
                if (!parameterType.IsArray
                    || parameter.Value is not IEnumerable enumerable)
                {
                    constructorArguments.Add(parameter.Value);
                    continue;
                }

                ArrayList list = new();
                foreach (var item in enumerable)
                {
                    if (item is CustomAttributeTypedArgument argument)
                    {
                        list.Add(argument.Value);
                    }
                    else
                    {
                        list.Add(item);
                    }
                }

                constructorArguments.Add(list.ToArray(parameterType.GetElementType()));
            }

            ConstructorInfo constructor = attributeType.GetConstructor(constructorParameters.ToArray());
            attribute = constructor.Invoke(constructorArguments.ToArray());

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                attributeType.GetProperty(namedArgument.MemberInfo.Name).SetValue(attribute, namedArgument.TypedValue.Value, null);
            }
        }

        // If not able to create instance of attribute ignore attribute. (May happen for custom user defined attributes).
        catch (BadImageFormatException)
        {
        }
        catch (FileLoadException)
        {
        }
        catch (TypeLoadException)
        {
        }

        return attribute as Attribute;
    }

    private static void AddNewAttributes(
        IList<CustomAttributeData> customAttributes,
        bool shouldGetAllAttributes,
        Type type,
        Dictionary<string, object> uniqueAttributes,
        List<object> nonUniqueAttributes)
    {
        foreach (var attribute in customAttributes)
        {
            if (!shouldGetAllAttributes
                && !IsTypeInheriting(attribute.Constructor.DeclaringType, type)
                    && !attribute.Constructor.DeclaringType.AssemblyQualifiedName.Equals(
                        type.AssemblyQualifiedName))
            {
                continue;
            }

            Attribute? attributeInstance = CreateAttributeInstance(attribute);
            if (attributeInstance == null)
            {
                continue;
            }

            if (GetCustomAttributes(
                    attributeInstance.GetType().GetTypeInfo(),
                    typeof(AttributeUsageAttribute),
                    true).FirstOrDefault() is AttributeUsageAttribute attributeUsageAttribute && !attributeUsageAttribute.AllowMultiple)
            {
                if (!uniqueAttributes.ContainsKey(attributeInstance.GetType().FullName))
                {
                    uniqueAttributes.Add(attributeInstance.GetType().FullName, attributeInstance);
                }
            }
            else
            {
                nonUniqueAttributes.Add(attributeInstance);
            }
        }
    }

    /// <summary>
    /// Check whether the member is loaded in a reflection only context.
    /// </summary>
    /// <param name="memberInfo"> The member Info. </param>
    /// <returns> True if the member is loaded in a reflection only context. </returns>
    private static bool IsReflectionOnlyLoad(MemberInfo? memberInfo)
    {
        if (memberInfo != null)
        {
            return memberInfo.Module.Assembly.ReflectionOnly;
        }

        return false;
    }

    private static bool IsTypeInheriting(Type? type1, Type type2)
    {
        while (type1 != null)
        {
            if (type1.AssemblyQualifiedName.Equals(type2.AssemblyQualifiedName))
            {
                return true;
            }

            type1 = type1.GetTypeInfo().BaseType;
        }

        return false;
    }
#endif
}

#endif
