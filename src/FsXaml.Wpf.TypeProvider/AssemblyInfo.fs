namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsXaml.Wpf.TypeProvider")>]
[<assembly: AssemblyProductAttribute("FsXaml")>]
[<assembly: AssemblyDescriptionAttribute("F# Tools for working with XAML Projects")>]
[<assembly: AssemblyVersionAttribute("0.0.0")>]
[<assembly: AssemblyFileVersionAttribute("0.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.0"
    let [<Literal>] InformationalVersion = "0.0.0"
