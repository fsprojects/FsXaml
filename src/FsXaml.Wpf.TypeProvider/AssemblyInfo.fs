namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsXaml.Wpf.TypeProvider")>]
[<assembly: AssemblyProductAttribute("FsXaml")>]
[<assembly: AssemblyDescriptionAttribute("F# Tools for working with XAML Projects")>]
[<assembly: AssemblyVersionAttribute("2.1.0")>]
[<assembly: AssemblyFileVersionAttribute("2.1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "2.1.0"
    let [<Literal>] InformationalVersion = "2.1.0"
