namespace FsXaml

open System
open System.ComponentModel
open System.Collections.Generic
open System.Threading
open System.Windows.Input
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

open FsXaml.Internal

[<AbstractClass>]
type ViewModelBase() as self =
    let propertyChanged = new Event<_, _>()
    let vmf = ViewModelPropertyFactory((fun (pn : string) -> self.RaisePropertyChanged(pn)), propertyChanged.Publish)        
    
    member this.Factory with get() = vmf                    
    
    member this.RaisePropertyChanged(propertyName : string) =
        propertyChanged.Trigger(this, new PropertyChangedEventArgs(propertyName))
        
    member this.RaisePropertyChanged(expr : Expr) =
        let propName = getPropertyNameFromExpression(expr)
        this.RaisePropertyChanged(propName)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish


    