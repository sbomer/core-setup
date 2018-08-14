// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.StartupHooks
{
    public class GivenThatICareAboutStartupHooks : IClassFixture<GivenThatICareAboutStartupHooks.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private string startupHookVarName = "DOTNET_STARTUP_HOOKS";

        public GivenThatICareAboutStartupHooks(GivenThatICareAboutStartupHooks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        // Run the app with an invalid syntax in startup hook variable
        [Fact]
        public void Muxer_activation_of_Invalid_StartupHook_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.ArgumentException: The syntax of the startup hook variable was invalid.";

            // Incorrect syntax in type name
            var startupHookVar = "Assembly.dll!Type!Type" + Path.PathSeparator + "Assembly.dll";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Incorrect syntax with empty type
            startupHookVar = "Assembly.dll!" + Path.PathSeparator + startupHookDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Incorrect syntax with empty assembly path
            startupHookVar = "!TypeName" + Path.PathSeparator + startupHookDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // No assembly path and type separator
            startupHookVar = "Assembly.dll" + Path.PathSeparator + "Assembly2.dll";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Syntax errors are caught before any hooks run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + "!";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError)
                .And
                .NotHaveStdOutContaining("Hello from startup hook!");
        }

        // Run the app with startup hook that has no public Initialize method
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Method()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.MissingMethodException: Method '{0}' not found.";

            // Non-public Initialize method
            var startupHookVar = startupHookDll + "!StartupHook.StartupHookWithNonPublicMethod";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithNonPublicMethod.Initialize"));

            // No Initialize method
            startupHookVar = startupHookDll + "!StartupHook.StartupHookWithoutInitializeMethod";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithoutInitializeMethod.Initialize"));

            // Missing Initialize method is caught after previous hooks have run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + startupHookDll + "!StartupHook.StartupHookWithoutInitializeMethod";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithoutInitializeMethod.Initialize"));
        }

        // Run the app with startup hook that has the wrong signature
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Incorrect_Signature_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.ArgumentException: The signature of the startup hook '{0}' was invalid. It must be 'public static void Initialize()'.";

            // Initialize method that has parameters or returns non-void
            var startupHookVar = startupHookDll + "!StartupHook.StartupHookWithIncorrectSignature";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithIncorrectSignature.Initialize"));

            // Initialize is an instance method
            startupHookVar = startupHookDll + "!StartupHook.StartupHookWithInstanceMethod";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithInstanceMethod.Initialize"));

            // Signature problem is caught after previous hooks have run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + startupHookDll + "!StartupHook.StartupHookWithIncorrectSignature";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining(String.Format(expectedError, "StartupHook.StartupHookWithIncorrectSignature.Initialize"));
        }

        // Run the app with missing startup hook assembly
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;
            var startupHookMissingDll = Path.Combine(Path.GetDirectoryName(startupHookDll), "StartupHookMissing.dll");

            var expectedError = "System.IO.FileNotFoundException: Could not resolve assembly '{0}";

            // Missing dll is detected with appropriate error
            var startupHookVar = startupHookMissingDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, Path.GetFullPath(startupHookMissingDll)));

            // Missing dll is detected before any hooks run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + startupHookMissingDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, Path.GetFullPath((startupHookMissingDll))))
                .And
                .NotHaveStdOutContaining("Hello from startup hook!");
        }

        // Run the app with an invalid startup hook assembly
        [Fact]
        public void Muxer_activation_of_Invalid_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;
            var startupHookFakeDll = Path.Combine(Path.GetDirectoryName(startupHookDll), "StartupHookFake.dll");

            var expectedError = "System.BadImageFormatException";

            // Dll load gives meaningful error message
            var startupHookVar = startupHookFakeDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Dll load error happens after previous hooks run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + startupHookFakeDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);
        }

        // Run the app with the startup hook type missing
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Type_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Missing type is detected
            var startupHookVar = startupHookDll + "!StartupHook.StartupHookMissingType";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook.StartupHookMissingType' from assembly 'StartupHook");

            // Missing type is detected after previous hooks have run
            startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator + startupHookDll + "!StartupHook.StartupHookMissingType";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook.StartupHookMissingType' from assembly 'StartupHook");
        }

        // Run the app with a startup hook
        [Fact]
        public void Muxer_activation_of_StartupHook_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Simple startup hook
            var startupHookVar = startupHookDll + "!StartupHook.StartupHook";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdOutContaining("Hello World");

            // Startup hook in type that has an additional overload of Initialize with a different signature
            startupHookVar = startupHookDll + "!StartupHook.StartupHookWithOverload";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook with overload! Input: 123")
                .And
                .HaveStdOutContaining("Hello World");
        }

        // Run the app with multiple startup hooks
        [Fact]
        public void Muxer_activation_of_Multiple_StartupHooks_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHook2Fixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Copy();
            var startupHook2Dll = startupHook2Fixture.TestProject.AppDll;

            // Mulitiple startup hooks
            var startupHookVar = startupHookDll + "!StartupHook.StartupHook" + Path.PathSeparator +
                startupHook2Dll + "!StartupHook.StartupHookWithDependency";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdOutContaining("Hello from startup hook with dependency!")
                .And
                .HaveStdOutContaining("Hello World");
        }

        // Run the app with a startup hook assembly that depends on assemblies not on the TPA list
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Dependencies_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Startup hook has a dependency not on the TPA list
            var startupHookVar = startupHookDll + "!StartupHook.StartupHookWithDependency";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("System.IO.FileNotFoundException: Could not load file or assembly 'Newtonsoft.Json");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture { get; set; }

            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture { get; set; }

            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyPublishedAndRestoredPortableAppProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture = new TestProjectFixture("PortableAppWithException", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredStartupHookProjectFixture = new TestProjectFixture("StartupHook", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture = new TestProjectFixture("StartupHookWithDependency", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                PreviouslyPublishedAndRestoredPortableAppProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Dispose();

                PreviouslyPublishedAndRestoredStartupHookProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Dispose();
            }
        }
    }
}
