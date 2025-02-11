// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices;
using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Deployment;
using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Moq;

using MSTestAdapter.PlatformServices.Tests.Utilities;

using TestFramework.ForTestingMSTest;

namespace MSTestAdapter.PlatformServices.UnitTests.Utilities;
public class DeploymentUtilityTests : TestContainer
{
    private const string TestRunDirectory = "C:\\temp\\testRunDirectory";
    private const string RootDeploymentDirectory = "C:\\temp\\testRunDirectory\\Deploy";
    private const string DefaultDeploymentItemPath = @"c:\temp";
    private const string DefaultDeploymentItemOutputDirectory = "out";

    private readonly Mock<ReflectionUtility> _mockReflectionUtility;
    private readonly Mock<FileUtility> _mockFileUtility;
    private readonly Mock<AssemblyUtility> _mockAssemblyUtility;
    private readonly Mock<IRunContext> _mockRunContext;
    private readonly Mock<ITestExecutionRecorder> _mockTestExecutionRecorder;

    private readonly DeploymentUtility _deploymentUtility;

    private IList<string> _warnings;

    public DeploymentUtilityTests()
    {
        _mockReflectionUtility = new Mock<ReflectionUtility>();
        _mockFileUtility = new Mock<FileUtility>();
        _mockAssemblyUtility = new Mock<AssemblyUtility>();
        _warnings = new List<string>();

        _deploymentUtility = new DeploymentUtility(
            new DeploymentItemUtility(_mockReflectionUtility.Object),
            _mockAssemblyUtility.Object,
            _mockFileUtility.Object);

        _mockRunContext = new Mock<IRunContext>();
        _mockTestExecutionRecorder = new Mock<ITestExecutionRecorder>();
    }

    #region Deploy tests

    public void DeployShouldReturnFalseWhenNoDeploymentItemsOnTestCase()
    {
        var testCase = new TestCase("A.C.M", new Uri("executor://testExecutor"), "A");
        testCase.SetPropertyValue(DeploymentItemUtilityTests.DeploymentItemsProperty, null);
        var testRunDirectories = new TestRunDirectories(RootDeploymentDirectory);

        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
#if NET462
        _mockAssemblyUtility.Setup(
                au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
                au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });
#endif

        Verify(
            !_deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));
    }

#if NET462
    public void DeployShouldDeploySourceAndItsConfigFile()
    {
        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        string warning;
        var sourceFile = Assembly.GetExecutingAssembly().GetName().Name + ".dll";
        var configFile = Assembly.GetExecutingAssembly().GetName().Name + ".dll.config";
        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(sourceFile)),
                Path.Combine(testRunDirectories.OutDirectory, sourceFile),
                out warning),
            Times.Once);
        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(configFile)),
                Path.Combine(testRunDirectories.OutDirectory, configFile),
                out warning),
            Times.Once);
    }

    public void DeployShouldDeployDependentFiles()
    {
        var dependencyFile = "C:\\temp\\dependency.dll";

        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(new string[] { dependencyFile });
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        string warning;

        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(dependencyFile)),
                Path.Combine(testRunDirectories.OutDirectory, Path.GetFileName(dependencyFile)),
                out warning),
            Times.Once);
    }

    public void DeployShouldDeploySatelliteAssemblies()
    {
        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);
        var assemblyFullPath = Assembly.GetExecutingAssembly().Location;
        var satelliteFullPath = Path.Combine(Path.GetDirectoryName(assemblyFullPath), "de", "satellite.dll");

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(assemblyFullPath))
            .Returns(new List<string> { satelliteFullPath });

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        string warning;

        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(satelliteFullPath)),
                Path.Combine(testRunDirectories.OutDirectory, "de", Path.GetFileName(satelliteFullPath)),
                out warning),
            Times.Once);
    }
#endif

    public void DeployShouldNotDeployIfOutputDirectoryIsInvalid()
    {
        var assemblyFullPath = Assembly.GetExecutingAssembly().Location;
        var deploymentItemPath = "C:\\temp\\sample.dll";
        var deploymentItemOutputDirectory = "..\\..\\out";

        var testCase = GetTestCaseAndTestRunDirectories(deploymentItemPath, deploymentItemOutputDirectory, out var testRunDirectories);

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
#if NET462
        _mockAssemblyUtility.Setup(
                au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
                au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });
#endif

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        string warning;

        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(deploymentItemPath)),
                It.IsAny<string>(),
                out warning),
            Times.Never);

        // Verify the warning.
        _mockTestExecutionRecorder.Verify(
            ter =>
            ter.SendMessage(
                TestMessageLevel.Warning,
                string.Format(
                    Resource.DeploymentErrorBadDeploymentItem,
                    deploymentItemPath,
                    deploymentItemOutputDirectory)),
            Times.Once);
    }

    public void DeployShouldDeployContentsOfADirectoryIfSpecified()
    {
        var assemblyFullPath = Assembly.GetExecutingAssembly().Location;

        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);
        var content1 = Path.Combine(DefaultDeploymentItemPath, "directoryContents.dll");
        var directoryContentFiles = new List<string> { content1 };

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !(s.EndsWith(".dll") || s.EndsWith(".config"))))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
#if NET462
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });
#endif
        _mockFileUtility.Setup(
            fu => fu.AddFilesFromDirectory(DefaultDeploymentItemPath, It.IsAny<bool>())).Returns(directoryContentFiles);
        _mockFileUtility.Setup(
            fu => fu.AddFilesFromDirectory(DefaultDeploymentItemPath, It.IsAny<Func<string, bool>>(), It.IsAny<bool>())).Returns(directoryContentFiles);

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        string warning;

        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(content1)),
                It.IsAny<string>(),
                out warning),
            Times.Once);
    }

#if NET462
    public void DeployShouldDeployPdbWithSourceIfPdbFileIsPresentInSourceDirectory()
    {
        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !s.EndsWith(".dll")))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(Array.Empty<string>());
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });
        string warning;
        _mockFileUtility.Setup(fu => fu.CopyFileOverwrite(It.IsAny<string>(), It.IsAny<string>(), out warning))
            .Returns(
                (string x, string y, string z) =>
                    {
                        z = string.Empty;
                        return y;
                    });

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        var sourceFile = Assembly.GetExecutingAssembly().GetName().Name + ".dll";
        var pdbFile = Assembly.GetExecutingAssembly().GetName().Name + ".pdb";
        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(sourceFile)),
                Path.Combine(testRunDirectories.OutDirectory, sourceFile),
                out warning),
            Times.Once);
        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(pdbFile)),
                Path.Combine(testRunDirectories.OutDirectory, pdbFile),
                out warning),
            Times.Once);
    }

    public void DeployShouldNotDeployPdbFileOfAssemblyIfPdbFileIsNotPresentInAssemblyDirectory()
    {
        var dependencyFile = "C:\\temp\\dependency.dll";

        // Path for pdb file of dependent assembly if pdb file is present.
        var pdbFile = Path.ChangeExtension(dependencyFile, "pdb");

        var testCase = GetTestCaseAndTestRunDirectories(DefaultDeploymentItemPath, DefaultDeploymentItemOutputDirectory, out var testRunDirectories);

        // Setup mocks.
        _mockFileUtility.Setup(fu => fu.DoesDirectoryExist(It.Is<string>(s => !s.EndsWith(".dll")))).Returns(true);
        _mockFileUtility.Setup(fu => fu.DoesFileExist(It.IsAny<string>())).Returns(true);
        _mockAssemblyUtility.Setup(
            au => au.GetFullPathToDependentAssemblies(It.IsAny<string>(), It.IsAny<string>(), out _warnings))
            .Returns(new string[] { dependencyFile });
        _mockAssemblyUtility.Setup(
            au => au.GetSatelliteAssemblies(It.IsAny<string>()))
            .Returns(new List<string> { });
        string warning;
        _mockFileUtility.Setup(fu => fu.CopyFileOverwrite(It.IsAny<string>(), It.IsAny<string>(), out warning))
            .Returns(
                (string x, string y, string z) =>
                {
                    z = string.Empty;
                    return y;
                });

        // Act.
        Verify(
            _deploymentUtility.Deploy(
                new List<TestCase> { testCase },
                testCase.Source,
                _mockRunContext.Object,
                _mockTestExecutionRecorder.Object,
                testRunDirectories));

        // Assert.
        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(dependencyFile)),
                Path.Combine(testRunDirectories.OutDirectory, Path.GetFileName(dependencyFile)),
                out warning),
            Times.Once);

        _mockFileUtility.Verify(
            fu =>
            fu.CopyFileOverwrite(
                It.Is<string>(s => s.Contains(pdbFile)),
                Path.Combine(testRunDirectories.OutDirectory, Path.GetFileName(pdbFile)),
                out warning),
            Times.Never);
    }
#endif

    #endregion

    #region CreateDeploymentDirectories tests

    public void CreateDeploymentDirectoriesShouldCreateDeploymentDirectoryFromRunContext()
    {
        // Setup mocks
        _mockRunContext.Setup(rc => rc.TestRunDirectory).Returns(TestRunDirectory);
        _mockFileUtility.Setup(fu => fu.GetNextIterationDirectoryName(TestRunDirectory, It.IsAny<string>()))
            .Returns(RootDeploymentDirectory);

        // Act.
        _deploymentUtility.CreateDeploymentDirectories(_mockRunContext.Object);

        // Assert.
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix)), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix), Environment.MachineName)), Times.Once);
    }

    public void CreateDeploymentDirectoriesShouldCreateDefaultDeploymentDirectoryIfTestRunDirectoryIsNull()
    {
        // Setup mocks
        _mockRunContext.Setup(rc => rc.TestRunDirectory).Returns((string)null);
        _mockFileUtility.Setup(fu => fu.GetNextIterationDirectoryName(It.Is<string>(s => s.Contains(TestRunDirectories.DefaultDeploymentRootDirectory)), It.IsAny<string>()))
            .Returns(RootDeploymentDirectory);

        // Act.
        _deploymentUtility.CreateDeploymentDirectories(_mockRunContext.Object);

        // Assert.
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix)), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix), Environment.MachineName)), Times.Once);
    }

    public void CreateDeploymentDirectoriesShouldCreateDefaultDeploymentDirectoryIfRunContextIsNull()
    {
        // Setup mocks
        _mockFileUtility.Setup(fu => fu.GetNextIterationDirectoryName(It.Is<string>(s => s.Contains(TestRunDirectories.DefaultDeploymentRootDirectory)), It.IsAny<string>()))
            .Returns(RootDeploymentDirectory);

        // Act.
        _deploymentUtility.CreateDeploymentDirectories(null);

        // Assert.
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix)), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(RootDeploymentDirectory), Times.Once);
        _mockFileUtility.Verify(fu => fu.CreateDirectoryIfNotExists(Path.Combine(Path.Combine(RootDeploymentDirectory, TestRunDirectories.DeploymentInDirectorySuffix), Environment.MachineName)), Times.Once);
    }

    #endregion

    #region private methods

    private static TestCase GetTestCaseAndTestRunDirectories(string deploymentItemPath, string defaultDeploymentItemOutputDirectoryOut, out TestRunDirectories testRunDirectories)
    {
        var testCase = new TestCase("A.C.M", new Uri("executor://testExecutor"), Assembly.GetExecutingAssembly().Location);
        var kvpArray = new[]
        {
            new KeyValuePair<string, string>(
                deploymentItemPath,
                defaultDeploymentItemOutputDirectoryOut),
        };
        testCase.SetPropertyValue(DeploymentItemUtilityTests.DeploymentItemsProperty, kvpArray);
        var currentExecutingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        testRunDirectories = new TestRunDirectories(currentExecutingFolder);

        return testCase;
    }

    #endregion
}
