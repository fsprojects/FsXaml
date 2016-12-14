namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsXaml.Wpf.Blend")>]
[<assembly: AssemblyProductAttribute("FsXaml")>]
[<assembly: AssemblyDescriptionAttribute("F# Tools for working with XAML Projects")>]
[<assembly: AssemblyVersionAttribute("2.2.0")>]
[<assembly: AssemblyFileVersionAttribute("2.2.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "2.2.0"
    let [<Literal>] InformationalVersion = "2.2.0"
