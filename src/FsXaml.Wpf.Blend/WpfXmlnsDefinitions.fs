namespace FsXaml

open System.Reflection
open System.Windows.Markup

[<assembly: XmlnsPrefix("http://github.com/fsprojects/FsXaml", "fsxaml")>]
[<assembly: XmlnsDefinition("http://github.com/fsprojects/FsXaml", "FsXaml")>]
[<assembly: AssemblyKeyFile(@"..\..\FsXaml.snk")>]
do()
