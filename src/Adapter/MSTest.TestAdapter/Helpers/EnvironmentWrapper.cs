﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Helpers;

internal class EnvironmentWrapper : IEnvironment
{
    public string MachineName => Environment.MachineName;
}
