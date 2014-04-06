namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("")>]
[<assembly: AssemblyProductAttribute("")>]
[<assembly: AssemblyDescriptionAttribute("A short summary of your project.")>]
[<assembly: AssemblyVersionAttribute("0.0.0")>]
[<assembly: AssemblyFileVersionAttribute("0.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.0"
