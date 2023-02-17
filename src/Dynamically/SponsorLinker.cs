using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Devlooped.Dynamically;

[Generator]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
class SponsorLinker : SponsorLink
{
    public SponsorLinker() : base(SponsorLinkSettings.Create(
        "devlooped", "Dynamically",
        packageId: ThisAssembly.Project.PackageId,
        version: new Version(ThisAssembly.Info.Version).ToString(2)
#if DEBUG
        , quietDays: 0
#endif
        ))
    { }
}