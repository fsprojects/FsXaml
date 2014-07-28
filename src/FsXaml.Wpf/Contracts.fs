namespace FsXaml

open System
open System.ComponentModel
open System.Windows.Input
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

open System.Reflection

[<assembly:AssemblyVersion("0.9.8.0")>]
[<assembly:AssemblyFileVersion("0.9.8.0")>]
do()

/// Used for one-way conversion from EventArgs -> other types when used with EventToCommand 
type public IEventArgsConverter = 
    abstract member Convert : EventArgs -> obj -> obj

