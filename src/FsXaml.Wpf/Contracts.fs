namespace FsXaml

open System
open System.ComponentModel
open System.Windows.Input
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

/// Extension of ICommand to allow RaiseCanExecuteChanged to be called explicitly on the command
type public INotifyCommand =
    inherit ICommand
    /// Raises the CanExecuteChanged event explicitly
    abstract member RaiseCanExecuteChanged : unit -> unit

/// Used for one-way conversion from EventArgs -> other types when used with EventToCommand 
type public IEventArgsConverter = 
    abstract member Convert : EventArgs -> obj -> obj

