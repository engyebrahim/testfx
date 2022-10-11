// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Helpers;

namespace Microsoft.VisualStudio.TestPlatform.MSTestAdapter.UnitTests.TestableImplementations;

/// <summary>
/// A testable implementation of reflect helper.
/// </summary>
internal class TestableReflectHelper : ReflectHelper
{
    /// <summary>
    /// A dictionary to hold mock custom attributes. The int represents a hascode of
    /// the Type of custom attribute and the level its applied at :
    /// MemberTypes.All for assembly level
    /// MemberTypes.TypeInfo for class level
    /// MemberTypes.Method for method level.
    /// </summary>
    private readonly Dictionary<int, Attribute[]> _customAttributes;

    public TestableReflectHelper()
    {
        _customAttributes = new Dictionary<int, Attribute[]>();
    }

    public void SetCustomAttribute(Type type, Attribute[] values, MemberTypes memberTypes)
    {
        var hashcode = type.FullName.GetHashCode() + memberTypes.GetHashCode();
        if (_customAttributes.ContainsKey(hashcode))
        {
            _customAttributes[hashcode] = _customAttributes[hashcode].Concat(values).ToArray();
        }
        else
        {
            _customAttributes[hashcode] = values;
        }
    }

    internal override Attribute[] GetCustomAttributeForAssembly(MemberInfo memberInfo, Type type)
    {
        var hashcode = MemberTypes.All.GetHashCode() + type.FullName.GetHashCode();

        if (_customAttributes.TryGetValue(hashcode, out Attribute[] value))
        {
            return value;
        }
        else
        {
            return Enumerable.Empty<Attribute>().ToArray();
        }
    }

    internal override Attribute[] GetCustomAttributes(MemberInfo memberInfo, Type type)
    {
        var hashcode = memberInfo.MemberType.GetHashCode() + type.FullName.GetHashCode();

        if (_customAttributes.TryGetValue(hashcode, out Attribute[] value))
        {
            return value;
        }
        else
        {
            return Enumerable.Empty<Attribute>().ToArray();
        }
    }
}
