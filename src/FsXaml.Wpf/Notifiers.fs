namespace FsXaml

open System
open System.ComponentModel
open System.Collections.Generic
open System.Threading
open System.Windows.Input
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

open FsXaml.Internal

/// Encapsulation of a value which handles raising property changed automatically in a clean manner
[<AbstractClass>]
type public NotifyingValue<'a>() =
    /// Extracts the current value from the backing storage
    abstract member Value : 'a with get, set

type internal NotifyingValueBackingField<'a> (propertyName, raisePropertyChanged : string -> unit, defaultValue : 'a) =
    inherit NotifyingValue<'a>()
    let propertyName = propertyName
    let mutable value = defaultValue
    override this.Value 
        with get() = value 
        and set(v) = 
            if (not (EqualityComparer<'a>.Default.Equals(value, v))) then
                value <- v
                raisePropertyChanged propertyName                

type internal NotifyingValueFuncs<'a> (propertyName, raisePropertyChanged : string -> unit, getter, setter) =
    inherit NotifyingValue<'a>()
    let propertyName = propertyName
    override this.Value 
        with get() = getter()
        and set(v) = 
            if (not (EqualityComparer<'a>.Default.Equals(getter(), v))) then
                setter v
                raisePropertyChanged propertyName

[<AutoOpen>]
module ChangeNotifierUtils =    
    let internal getPropertyNameFromExpression(expr : Expr) = 
        match expr with
        | PropertyGet(a, pi, list) -> pi.Name
        | _ -> ""
