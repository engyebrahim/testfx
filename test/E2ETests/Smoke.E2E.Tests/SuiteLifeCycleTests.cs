﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.MSTestV2.CLIAutomation;

namespace MSTestAdapter.Smoke.E2ETests;
public class SuiteLifeCycleTests : CLITestBase
{
    private const string Assembly = "SuiteLifeCycleTestProject.dll";

    public void ValidateTestRunLifecycle_net6()
    {
        ValidateTestRunLifecycle("net462");
    }

    private void ValidateTestRunLifecycle(string targetFramework)
    {
        InvokeVsTestForExecution(new[] { targetFramework + "\\" + Assembly });
        Verify(_runEventsHandler.PassedTests.Count == 1);

        var testMethod = _runEventsHandler.PassedTests.Single();
        Verify(testMethod.Outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed);
        if (targetFramework == "net462")
        {
            Verify(testMethod.Messages.Single().Text.Contains(
                """
            ClassInitialize was called
            Ctor was called
            TestInitialize was called
            TestMethod was called
            TestCleanup was called
            Dispose was called
            ClassCleanup was called
            """));
        }
        else
        {
            Verify(testMethod.Messages.Single().Text.Contains(
                """
            ClassInitialize was called
            Ctor was called
            TestInitialize was called
            TestMethod was called
            TestCleanup was called
            DisposeAsync was called
            Dispose was called
            ClassCleanup was called
            """));
        }
    }
}
