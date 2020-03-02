namespace OpenXR.ClangSharpGenerator {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using ClangSharp;
    using ClangSharp.Interop;

    class Program {
        static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static readonly string NugetPackagesRoot = Path.Combine(UserProfile, ".nuget", "packages");

        static int Main() {
            SetupPath();

            string includeRoot =
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "include");

            var config = new PInvokeGeneratorConfiguration(
                libraryPath: "openxr_loader",
                namespaceName: "OpenXR.PInvoke",
                options: PInvokeGeneratorConfigurationOptions.GenerateMultipleFiles,
                outputLocation: "gen",
                methodClassName: "XR",
                methodPrefixToStrip: "xr"
            );

            string[] files = new[] { "openxr/openxr.h" }
                .Select(name => Path.Combine(includeRoot, name))
                .ToArray();
            int exitCode = 0;

            var translationFlags = CXTranslationUnit_Flags.CXTranslationUnit_None;

            translationFlags |= CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes;               // Include attributed types in CXType
            translationFlags |= CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes;              // Implicit attributes should be visited

            var clangCommandLineArgs = new List<string> {
                "--include-directory=" + includeRoot,
                "--include-directory=" + Path.Combine(NugetPackagesRoot,
                    "cppsharp", "0.10.1", "output", "lib", "clang", "9.0.0","include"),
                "--language=c++",
            };

            using (var generator = new PInvokeGenerator(config)) {
                foreach (string file in files) {
                    var translationUnitError = CXTranslationUnit.TryParse(generator.IndexHandle,
                        file,
                        clangCommandLineArgs.ToArray().AsSpan(),
                        Array.Empty<CXUnsavedFile>(),
                        translationFlags,
                        out CXTranslationUnit handle);
                    bool skipProcessing = false;

                    if (translationUnitError != CXErrorCode.CXError_Success) {
                        Console.WriteLine($"Error: Parsing failed for '{file}' due to '{translationUnitError}'.");
                        skipProcessing = true;
                    } else if (handle.NumDiagnostics != 0) {
                        Console.WriteLine($"Diagnostics for '{file}':");

                        for (uint i = 0; i < handle.NumDiagnostics; ++i) {
                            using var diagnostic = handle.GetDiagnostic(i);

                            Console.Write("    ");
                            Console.WriteLine(diagnostic.Format(CXDiagnostic.DefaultDisplayOptions).ToString());

                            skipProcessing |= (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error);
                            skipProcessing |= (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Fatal);
                        }
                    }

                    if (skipProcessing) {
                        Console.WriteLine($"Skipping '{file}' due to one or more errors listed above.");
                        Console.WriteLine();

                        exitCode = -1;
                        continue;
                    }

                    using var translationUnit = TranslationUnit.GetOrCreate(handle);

                    Console.WriteLine($"Processing '{file}'");
                    generator.GenerateBindings(translationUnit);
                }

                if (generator.Diagnostics.Count != 0) {
                    Console.WriteLine("Diagnostics for binding generation:");

                    foreach (var diagnostic in generator.Diagnostics) {
                        Console.Write("    ");
                        Console.WriteLine(diagnostic);

                        if (diagnostic.Level == DiagnosticLevel.Warning) {
                            if (exitCode >= 0) {
                                exitCode++;
                            }
                        } else if (diagnostic.Level == DiagnosticLevel.Error) {
                            if (exitCode >= 0) {
                                exitCode = -1;
                            } else {
                                exitCode--;
                            }
                        }
                    }
                }
            }

            return exitCode;
        }

        static void SetupPath() {
            string clangBinDir = Path.Combine(NugetPackagesRoot,
                "libclang.runtime.win-x64", "9.0.0",
                "runtimes", "win-x64", "native");
            string clangSharpBinDir = Path.Combine(NugetPackagesRoot,
                "libclangsharp.runtime.win-x64", "9.0.0-beta1",
                "runtimes", "win-x64", "native");
            Environment.SetEnvironmentVariable("PATH",
                string.Join(Path.PathSeparator,
                    Environment.GetEnvironmentVariable("PATH"),
                    clangBinDir,
                    clangSharpBinDir));
        }
    }
}
