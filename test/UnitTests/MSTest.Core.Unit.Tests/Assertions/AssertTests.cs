// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestFramework.UnitTests;

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using global::TestFramework.ForTestingMSTest;

public partial class AssertTests
{
    #region That tests
    public void ThatShouldReturnAnInstanceOfAssert()
    {
        Verify(Assert.That is not null);
    }

    public void ThatShouldCacheAssertInstance()
    {
        Verify(Assert.That == Assert.That);

    }
    #endregion

    #region ReplaceNullChars tests
    public void ReplaceNullCharsShouldReturnStringIfNullOrEmpty()
    {
        Verify(Assert.ReplaceNullChars(null) == null);
        Verify(string.Empty == Assert.ReplaceNullChars(string.Empty));
    }

    public void ReplaceNullCharsShouldReplaceNullCharsInAString()
    {
        Verify("The quick brown fox \\0 jumped over the la\\0zy dog\\0" == Assert.ReplaceNullChars("The quick brown fox \0 jumped over the la\0zy dog\0"));
    }
    #endregion

    #region BuildUserMessage tests
    // See https://github.com/dotnet/sdk/issues/25373
    public void BuildUserMessageThrowsWhenMessageContainsInvalidStringFormatComposite()
    {
        var ex = VerifyThrows(() => Assert.BuildUserMessage("{", "arg"));

        Verify(ex is not null);
        Verify(typeof(FormatException) == ex.GetType());
    }

    // See https://github.com/dotnet/sdk/issues/25373
    public void BuildUserMessageDoesNotThrowWhenMessageContainsInvalidStringFormatCompositeAndNoArgumentsPassed()
    {
        string message = Assert.BuildUserMessage("{");
        Verify("{" == message);
    }
    #endregion
}
