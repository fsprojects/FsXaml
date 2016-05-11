namespace FsXaml

open System
open System.Reflection

[<assembly:AssemblyVersion("1.9.0.0")>]
[<assembly:AssemblyFileVersion("1.9.0.0")>]
do()

/// Used for one-way conversion from EventArgs -> other types when used with EventToCommand 
type public IEventArgsConverter = 
    abstract member Convert : EventArgs -> obj -> obj

