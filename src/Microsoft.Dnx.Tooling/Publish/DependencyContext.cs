// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Dnx.Runtime.Common.Impl;
using NuGet;

namespace Microsoft.Dnx.Tooling.Publish
{
    public class DependencyContext
    {
        public DependencyContext(Runtime.Project project, FrameworkName targetFramework, IEnumerable<string> runtimeIdentifiers)
        {
            var applicationHostContext = new ApplicationHostContext
            {
                Project = project,
                TargetFramework = targetFramework,
                RuntimeIdentifiers = runtimeIdentifiers
            };

            ApplicationHostContext.Initialize(applicationHostContext);

            FrameworkName = targetFramework;
            LibraryManager = applicationHostContext.LibraryManager;
            PackagesDirectory = applicationHostContext.PackagesDirectory;
            RuntimeIdentifiers = runtimeIdentifiers;
        }

        public LibraryManager LibraryManager { get; set; }
        public FrameworkName FrameworkName { get; set; }
        public IEnumerable<string> RuntimeIdentifiers { get; set; }
        public string PackagesDirectory { get; private set; }

        public static FrameworkName SelectFrameworkNameForRuntime(IEnumerable<FrameworkName> availableFrameworks, FrameworkName currentFramework, string runtime)
        {
            // Filter out frameworks incompatible with the current framework before selecting
            return SelectFrameworkNameForRuntime(
                availableFrameworks.Where(f => VersionUtility.IsCompatible(currentFramework, f)),
                runtime);
        }

        public static FrameworkName SelectFrameworkNameForRuntime(IEnumerable<FrameworkName> availableFrameworks, string runtime)
        {
            var parts = runtime.Split(new[] { '.' }, 2);
            if (parts.Length != 2)
            {
                return null;
            }
            parts = parts[0].Split(new[] { '-' }, 4);
            if (parts.Length < 2)
            {
                return null;
            }
            if (parts.Length == 2 && !string.Equals(parts[1].ToLowerInvariant(), "mono"))
            {
                return null;
            }
            switch (parts[1].ToLowerInvariant())
            {
                case "mono":
                case "clr":
                    // CLR currently supports anything <= dnx46
                    return availableFrameworks
                        .Where(fx => fx.Identifier.Equals(FrameworkNames.LongNames.Dnx, StringComparison.Ordinal) && fx.Version <= new Version(4, 6))
                        .OrderByDescending(fx => fx.Version)
                        .FirstOrDefault();
                case "coreclr":
                    return availableFrameworks.FirstOrDefault(fx => fx.Equals(VersionUtility.ParseFrameworkName("dnxcore50")));
            }
            return null;
        }

        public static IEnumerable<string> GetRuntimeIdentifiers(string runtime)
        {
            // NOTE(anurse): This is a little bit of a workaround. We need to smooth this out.
            var parts = runtime.Split(new[] { '.' }, 2);
            if (parts.Length != 2)
            {
                yield break;
            }
            parts = parts[0].Split(new[] { '-' }, 4);
            if (parts.Length < 2)
            {
                yield break;
            }

            if (string.Equals(parts[1].ToLowerInvariant(), "mono"))
            {
                yield return "osx.10.10-x86";
                yield return "osx.10.10-x64";
                yield return "ubuntu.14.04-x86";
                yield return "ubuntu.14.04-x64";
            }
            else if (parts.Length == 4)
            {
                var arch = parts[3];
                switch (parts[2].ToLowerInvariant())
                {
                    case "darwin":
                        yield return "osx.10.10-" + arch;
                        break;
                    case "linux":
                        yield return "ubuntu.14.04-" + arch;
                        break;
                    case "win":
                        yield return "win7-" + arch;
                        break;
                }
            }
        }
    }
}
