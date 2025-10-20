using System.Text;
using System.Text.RegularExpressions;

using Markdig;
using Markdig.Syntax;

using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;

namespace MarkdownCodeVerifier;

/// <summary>
/// Verifies C# code blocks in Markdown files compile correctly.
/// </summary>
public class Program
{
    private static readonly string[] DefaultUsings =
    [
        "using System;",
        "using System.Linq;",
        "using System.Net;",
        "using System.Net.Http;",
        "using System.Text;",
        "using System.Threading;",
        "using System.Threading.Tasks;",
        "using System.Collections.Generic;",
        "using Microsoft.Extensions.Logging;",
        "using Newtonsoft.Json.Linq;",
        "using Octokit;",
        "using ProbotSharp.Application.Abstractions;",
        "using ProbotSharp.Application.Abstractions.Events;",
        "using ProbotSharp.Application.Abstractions.Commands;",
        "using ProbotSharp.Application.Extensions;",
        "using ProbotSharp.Application.Ports.Outbound;",
        "using ProbotSharp.Application.Services;",
        "using ProbotSharp.Domain.Attachments;",
        "using ProbotSharp.Domain.Commands;",
        "using ProbotSharp.Domain.Context;",
    ];

    /// <summary>
    /// Main entry point for the Markdown code verifier tool.
    /// </summary>
    /// <param name="args">Command line arguments: [root-path] [exclude-patterns] [--verbose].</param>
    /// <returns>Exit code: 0 for success, 1 if any code blocks fail to compile.</returns>
    public static async Task<int> Main(string[] args)
    {
        var rootPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var excludePatterns = args.Length > 1 ? args[1].Split(',') : new[] { "node_modules/", ".git/", ".aidocs/", "TEST-MARKDOWN-VERIFIER.md" };
        var verbose = args.Contains("--verbose") || args.Contains("-v");

        Console.WriteLine("📚 Markdown C# Code Verifier");
        Console.WriteLine($"Root: {rootPath}");
        Console.WriteLine($"Exclude: {string.Join(", ", excludePatterns)}");
        Console.WriteLine();

        var markdownFiles = FindMarkdownFiles(rootPath, excludePatterns);
        Console.WriteLine($"Found {markdownFiles.Count} markdown files");

        var codeBlocks = new List<(string FilePath, int LineNumber, string Code)>();
        foreach (var file in markdownFiles)
        {
            var blocks = await ExtractCSharpCodeBlocksAsync(file).ConfigureAwait(false);
            codeBlocks.AddRange(blocks);
        }

        Console.WriteLine($"Found {codeBlocks.Count} C# code blocks");
        Console.WriteLine();

        var references = GetMetadataReferences();
        var failures = new List<(string File, int Line, string Error)>();
        int verified = 0;

        foreach (var (filePath, lineNumber, code) in codeBlocks)
        {
            var relPath = Path.GetRelativePath(rootPath, filePath);

            if (verbose)
            {
                Console.Write($"Verifying {relPath}:{lineNumber}... ");
            }

            var result = CompileCode(code, references);

            if (result.Success)
            {
                verified++;
                if (verbose)
                {
                    Console.WriteLine("✓");
                }
            }
            else
            {
                failures.Add((relPath, lineNumber, result.Errors));
                Console.WriteLine(verbose ? "✗" : $"✗ {relPath}:{lineNumber}");
                if (!verbose)
                {
                    Console.WriteLine($"  {result.Errors}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"Total: {codeBlocks.Count}");
        Console.WriteLine($"✓ Verified: {verified}");
        Console.WriteLine($"✗ Failed: {failures.Count}");
        Console.WriteLine("========================================");

        if (failures.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Failed code blocks:");
            foreach (var (file, line, error) in failures)
            {
                Console.WriteLine($"  {file}:{line}");
                Console.WriteLine($"    {error}");
            }

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("✓ All C# code blocks verified successfully!");
        return 0;
    }

    private static List<string> FindMarkdownFiles(string rootPath, string[] excludePatterns)
    {
        return Directory.GetFiles(rootPath, "*.md", SearchOption.AllDirectories)
            .Where(f => !excludePatterns.Any(p => f.Contains(p)))
            .ToList();
    }

    private static async Task<List<(string FilePath, int LineNumber, string Code)>> ExtractCSharpCodeBlocksAsync(string filePath)
    {
        var blocks = new List<(string, int, string)>();
        var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var lines = content.Split('\n');

        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(content, pipeline);

        foreach (var block in document.Descendants<FencedCodeBlock>())
        {
            var info = block.Info?.ToLowerInvariant() ?? "";
            if (info == "csharp" || info == "c#" || info == "cs")
            {
                var code = block.Lines.ToString();
                var lineNumber = block.Line + 1; // 1-based line numbers
                blocks.Add((filePath, lineNumber, code));
            }
        }

        return blocks;
    }

    private static (bool Success, string Errors) CompileCode(string code, List<MetadataReference> references)
    {
        // Wrap code if it's not a complete compilation unit
        var wrappedCode = WrapCodeIfNeeded(code);

        var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);

        var compilation = CSharpCompilation.Create(
            "MarkdownCodeVerification",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (result.Success)
        {
            return (true, string.Empty);
        }

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.GetMessage())
            .ToList();

        return (false, string.Join("; ", errors));
    }

    private static string WrapCodeIfNeeded(string code)
    {
        var trimmed = code.Trim();

        // If it already looks like a complete file (has namespace or top-level statements), use as-is
        if (trimmed.StartsWith("namespace ") ||
            trimmed.StartsWith("using ") ||
            trimmed.Contains("class Program"))
        {
            return code;
        }

        // If it's a class, interface, or attribute, wrap it in a namespace
        // Also check for comments before class definitions
        if (Regex.IsMatch(trimmed, @"^\s*(public\s+|internal\s+|private\s+)?(class|interface|record|struct|enum)\s+\w+") ||
            trimmed.StartsWith("[") ||
            Regex.IsMatch(trimmed, @"^//.*\n.*\[.*\]") ||
            Regex.IsMatch(trimmed, @"^//.*\n.*?\b(class|interface|record|struct|enum)\s+\w+"))
        {
            var sb = new StringBuilder();
            foreach (var u in DefaultUsings)
            {
                sb.AppendLine(u);
            }
            sb.AppendLine();
            sb.AppendLine("namespace MarkdownCodeVerification");
            sb.AppendLine("{");
            sb.AppendLine("    " + code.Replace("\n", "\n    "));
            sb.AppendLine("}");
            return sb.ToString();
        }

        // If it's method-level code, wrap in a class
        var fullWrapper = new StringBuilder();
        foreach (var u in DefaultUsings)
        {
            fullWrapper.AppendLine(u);
        }
        fullWrapper.AppendLine();
        fullWrapper.AppendLine("namespace MarkdownCodeVerification");
        fullWrapper.AppendLine("{");
        fullWrapper.AppendLine("    public class GeneratedCode");
        fullWrapper.AppendLine("    {");
        fullWrapper.AppendLine("        public void Execute()");
        fullWrapper.AppendLine("        {");
        fullWrapper.AppendLine("            " + code.Replace("\n", "\n            "));
        fullWrapper.AppendLine("        }");
        fullWrapper.AppendLine("    }");
        fullWrapper.AppendLine("}");
        return fullWrapper.ToString();
    }

    private static List<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>();

        // Add core .NET assemblies
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.ComponentModel.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Net.Http.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Net.Primitives.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.Uri.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")));
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));

        // Add common dependencies
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location));

        // Add project assemblies and dependencies from the same directory as the executing assembly
        var executingAssemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

        // Add ProbotSharp assemblies
        foreach (var dll in Directory.GetFiles(executingAssemblyPath, "ProbotSharp.*.dll"))
        {
            references.Add(MetadataReference.CreateFromFile(dll));
        }

        // Add commonly used dependencies
        var octokitDll = Path.Combine(executingAssemblyPath, "Octokit.dll");
        if (File.Exists(octokitDll))
        {
            references.Add(MetadataReference.CreateFromFile(octokitDll));
        }

        var newtonsoftDll = Path.Combine(executingAssemblyPath, "Newtonsoft.Json.dll");
        if (File.Exists(newtonsoftDll))
        {
            references.Add(MetadataReference.CreateFromFile(newtonsoftDll));
        }

        // Add Microsoft.Extensions assemblies
        var configurationDll = Path.Combine(executingAssemblyPath, "Microsoft.Extensions.Configuration.Abstractions.dll");
        if (File.Exists(configurationDll))
        {
            references.Add(MetadataReference.CreateFromFile(configurationDll));
        }

        var diDll = Path.Combine(executingAssemblyPath, "Microsoft.Extensions.DependencyInjection.Abstractions.dll");
        if (File.Exists(diDll))
        {
            references.Add(MetadataReference.CreateFromFile(diDll));
        }

        var aspnetCoreDll = Path.Combine(executingAssemblyPath, "Microsoft.AspNetCore.Http.Abstractions.dll");
        if (File.Exists(aspnetCoreDll))
        {
            references.Add(MetadataReference.CreateFromFile(aspnetCoreDll));
        }

        // Try to find missing assemblies in Bootstrap.Api output (fallback)
        var bootstrapPath = Path.Combine(executingAssemblyPath, "..", "..", "..", "..", "..", "src", "ProbotSharp.Bootstrap.Api", "bin", "Release", "net8.0");
        if (Directory.Exists(bootstrapPath))
        {
            var configAbstractions = Path.Combine(bootstrapPath, "Microsoft.Extensions.Configuration.Abstractions.dll");
            if (File.Exists(configAbstractions) && !references.Any(r => r.Display?.Contains("Configuration.Abstractions") == true))
            {
                references.Add(MetadataReference.CreateFromFile(configAbstractions));
            }

            var aspnetAbstractions = Path.Combine(bootstrapPath, "Microsoft.AspNetCore.Http.Abstractions.dll");
            if (File.Exists(aspnetAbstractions) && !references.Any(r => r.Display?.Contains("AspNetCore.Http.Abstractions") == true))
            {
                references.Add(MetadataReference.CreateFromFile(aspnetAbstractions));
            }

            var aspnetRouting = Path.Combine(bootstrapPath, "Microsoft.AspNetCore.Routing.Abstractions.dll");
            if (File.Exists(aspnetRouting) && !references.Any(r => r.Display?.Contains("AspNetCore.Routing") == true))
            {
                references.Add(MetadataReference.CreateFromFile(aspnetRouting));
            }
        }

        // Try to find test assemblies in Infrastructure.Tests output (for xUnit and Testcontainers)
        var infrastructureTestsPath = Path.Combine(executingAssemblyPath, "..", "..", "..", "..", "..", "tests", "ProbotSharp.Infrastructure.Tests", "bin", "Release", "net8.0");
        if (Directory.Exists(infrastructureTestsPath))
        {
            // xUnit
            var xunitCore = Path.Combine(infrastructureTestsPath, "xunit.core.dll");
            if (File.Exists(xunitCore))
            {
                references.Add(MetadataReference.CreateFromFile(xunitCore));
            }

            var xunitAssert = Path.Combine(infrastructureTestsPath, "xunit.assert.dll");
            if (File.Exists(xunitAssert))
            {
                references.Add(MetadataReference.CreateFromFile(xunitAssert));
            }

            var xunitAbstractions = Path.Combine(infrastructureTestsPath, "xunit.abstractions.dll");
            if (File.Exists(xunitAbstractions))
            {
                references.Add(MetadataReference.CreateFromFile(xunitAbstractions));
            }

            // Testcontainers
            var testcontainersCore = Path.Combine(infrastructureTestsPath, "Testcontainers.dll");
            if (File.Exists(testcontainersCore))
            {
                references.Add(MetadataReference.CreateFromFile(testcontainersCore));
            }

            // Testcontainers.PostgreSql might not be in Infrastructure.Tests, that's OK
            var testcontainersPostgres = Path.Combine(infrastructureTestsPath, "Testcontainers.PostgreSql.dll");
            if (File.Exists(testcontainersPostgres))
            {
                references.Add(MetadataReference.CreateFromFile(testcontainersPostgres));
            }
        }

        // Try to find ASP.NET Core testing assemblies in Bootstrap.Api.Tests output
        var bootstrapApiTestsPath = Path.Combine(executingAssemblyPath, "..", "..", "..", "..", "..", "tests", "ProbotSharp.Bootstrap.Api.Tests", "bin", "Release", "net8.0");
        if (Directory.Exists(bootstrapApiTestsPath))
        {
            // ASP.NET Core Testing
            var mvcTesting = Path.Combine(bootstrapApiTestsPath, "Microsoft.AspNetCore.Mvc.Testing.dll");
            if (File.Exists(mvcTesting))
            {
                references.Add(MetadataReference.CreateFromFile(mvcTesting));
            }

            var aspnetCoreHosting = Path.Combine(bootstrapApiTestsPath, "Microsoft.AspNetCore.Hosting.Abstractions.dll");
            if (File.Exists(aspnetCoreHosting))
            {
                references.Add(MetadataReference.CreateFromFile(aspnetCoreHosting));
            }

            var testHost = Path.Combine(bootstrapApiTestsPath, "Microsoft.AspNetCore.TestHost.dll");
            if (File.Exists(testHost))
            {
                references.Add(MetadataReference.CreateFromFile(testHost));
            }
        }

        return references;
    }
}
