namespace FsXaml

open System
open System.Reflection

[<assembly:AssemblyVersion("1.9.0.3")>]
[<assembly:AssemblyFileVersion("1.9.0.3")>]
do()

/// Used for one-way conversion from EventArgs -> other types when used with EventToCommand 
type public IEventArgsConverter = 
    abstract member Convert : EventArgs -> obj -> obj

