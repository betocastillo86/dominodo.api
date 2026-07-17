using System.Xml.Linq;
using NUnit.Framework;
using Shouldly;

namespace Dominodo.E2E.Tests.Architecture;

/// <summary>
/// Enforces the E2E suite's physical independence from the API (see <c>tests/e2e/README.md</c> §5).
///
/// Every <c>&lt;ProjectReference&gt;</c> under <c>tests/e2e/</c> must target a <c>Dominodo.E2E.*</c>
/// project. API projects are named <c>Dominodo.&lt;Module&gt;.*</c> / <c>Dominodo.Shared.*</c> /
/// <c>Dominodo.Adapters.*</c> / <c>Dominodo.Api</c> and never contain <c>Dominodo.E2E.</c>, so any
/// reference that does NOT point at a <c>Dominodo.E2E.*</c> project is a reference into <c>src/</c>
/// (the API) — which breaks the suite's core property.
///
/// This lives inside the E2E suite itself and inspects the <c>.csproj</c> files directly rather than
/// loading assemblies — referencing an API project just to test it would be the very coupling it
/// guards against.
/// </summary>
[TestFixture]
public sealed class E2EIndependenceTests
{
    private const string E2EPrefix = "Dominodo.E2E.";

    [Test]
    public void Every_project_reference_targets_only_the_E2E_suite()
    {
        var e2eRoot = FindE2ERoot();

        var projects = Directory.GetFiles(e2eRoot, "*.csproj", SearchOption.AllDirectories);
        projects.ShouldNotBeEmpty("the independence check must actually find E2E projects, not silently pass");

        var violations = new List<string>();

        foreach (var project in projects)
        {
            var document = XDocument.Parse(File.ReadAllText(project));

            var references = document.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => include!);

            foreach (var include in references)
            {
                var referencedProject = Path.GetFileName(include.Replace('\\', '/'));
                if (!referencedProject.StartsWith(E2EPrefix, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(project)} -> {include}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "a tests/e2e project references a non-E2E (API src/) project, breaking the suite's "
                + "independence (see tests/e2e/README.md §5):\n   "
                + string.Join("\n   ", violations));
    }

    /// <summary>Walks up from the test binaries until it finds <c>Dominodo.E2E.sln</c>; that folder is the suite root.</summary>
    private static string FindE2ERoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dominodo.E2E.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the E2E suite root (no 'Dominodo.E2E.sln' found walking up from "
                + AppContext.BaseDirectory + ").");
    }
}
