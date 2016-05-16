namespace FsXaml

open System
open System.Reflection

[<assembly:AssemblyVersion("2.0.0.0")>]
[<assembly:AssemblyFileVersion("2.0.0.0")>]
[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("FsXaml.Wpf.Blend")>]
do()

/// Used for one-way conversion from EventArgs -> other types when used with EventToCommand 
type public IEventArgsConverter = 
    abstract member Convert : EventArgs -> obj -> obj

