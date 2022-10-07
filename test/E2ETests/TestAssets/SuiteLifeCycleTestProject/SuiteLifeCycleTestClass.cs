// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SuiteLifeCycleTestProject;

[TestClass]
public sealed class SuiteLifeCycleTestClass : IDisposable
#if NET6_0_OR_GREATER
        , IAsyncDisposable 
#endif
{
    private static TestContext s_testContext;

    public TestContext TestContext { get; set; }

    public SuiteLifeCycleTestClass()
    {
        s_testContext.WriteLine("Ctor was called");
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        s_testContext = testContext;
        s_testContext.WriteLine("ClassInitialize was called");
    }

    [TestInitialize]
    public void TestInitialize()
    {
        TestContext.WriteLine("TestInitialize was called");
    }

    [TestMethod]
    public void TestMethod()
    {
        Debugger.Launch();
        Debugger.Break();
        TestContext.WriteLine("TestMethod was called");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        TestContext.WriteLine("TestCleanup was called");
    }

    public void Dispose()
    {
        TestContext.WriteLine("Dispose was called");
    }

#if NET6_0_OR_GREATER
    public ValueTask DisposeAsync()
    {
        TestContext.WriteLine("DisposeAsync was called");
        return ValueTask.CompletedTask;

    }
#endif

    [ClassCleanup]
    public void ClassCleanup()
    {
        TestContext.WriteLine("ClassCleanup was called");
    }
}
