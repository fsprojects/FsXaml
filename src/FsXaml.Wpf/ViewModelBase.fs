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

type internal NotifyingValueBackingField<'a> (propertyName, raisePropertyChanged, defaultValue : 'a) =
    inherit NotifyingValue<'a>()
    let propertyName = propertyName
    let mutable value = defaultValue
    override this.Value 
        with get() = value 
        and set(v) = 
            if (not (EqualityComparer<'a>.Default.Equals(value, v))) then
                value <- v
                raisePropertyChanged propertyName                

type internal NotifyingValueFuncs<'a> (propertyName, raisePropertyChanged, getter, setter) =
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


type ViewModelPropertyFactory(raisePropertyChanged, propertyChanged : IObservable<PropertyChangedEventArgs>) = 
    let syncContext = SynchronizationContext.Current
    let trackingDictionary = Dictionary<_,_>()
    let handleTrackingActions (args : PropertyChangedEventArgs) : unit =
        if trackingDictionary.ContainsKey(args.PropertyName) then
            syncContext.Post((fun _ -> List.iter (fun i -> i()) trackingDictionary.[args.PropertyName]), null)

    let addTrackingAction action name =
        if trackingDictionary.ContainsKey(name) then
            let existing = trackingDictionary.[name]
            trackingDictionary.[name] <- action :: existing
        else
            trackingDictionary.Add(name,[action])        

    let addTrackingActions action exprs =
        exprs
        |> List.map getPropertyNameFromExpression
        |> List.iter (addTrackingAction action)

    do
        propertyChanged.Subscribe(handleTrackingActions) |> ignore

    member private this.addRaiseCE command propsToTrigger =
        match propsToTrigger with
        | Some exprs -> this.RaiseCanExecuteChangedByProps(command, exprs)
        | None -> ()
        
    member private this.addNotifications prop propsToTrigger =
        match propsToTrigger with
        | Some exprs -> this.SetPropertyDependencies(prop, exprs)
        | None -> ()

    member this.Backing (prop : Expr, defaultValue) =
        NotifyingValueBackingField<'a>(getPropertyNameFromExpression(prop), raisePropertyChanged, defaultValue) :> NotifyingValue<'a>

    member this.FromFuncs (prop : Expr, getter, setter) =
        NotifyingValueFuncs<'a>(getPropertyNameFromExpression(prop), raisePropertyChanged, getter, setter) :> NotifyingValue<'a>

    member this.CommandAsync(asyncWorkflow) =
        let cmd = Commands.createAsyncInternal asyncWorkflow (fun () -> true)
        cmd

    member this.CommandAsyncChecked(asyncWorkflow, canExecute, ?dependentProperties: Expr list) =
        let cmd = Commands.createAsyncInternal asyncWorkflow canExecute
        this.addRaiseCE cmd dependentProperties
        cmd

    member this.CommandAsyncParam(asyncWorkflow) =
        let cmd = Commands.createAsyncParamInternal asyncWorkflow (fun _ -> true)
        cmd

    member this.CommandAsyncParamChecked(asyncWorkflow, canExecute, ?dependentProperties: Expr list) =
        let cmd = Commands.createAsyncParamInternal asyncWorkflow canExecute
        this.addRaiseCE cmd dependentProperties
        cmd

    member this.CommandSync(execute) =
        let cmd = Commands.createSyncInternal execute (fun () -> true) false
        cmd

    member this.CommandSyncChecked(execute, canExecute, ?dependentProperties: Expr list) =
        let cmd = Commands.createSyncInternal execute canExecute  false
        this.addRaiseCE cmd dependentProperties
        cmd

    member this.CommandSyncCheckedUsingCommandManager(execute, canExecute) =
        Commands.createSyncInternal execute canExecute true

    member this.CommandSyncParam(execute) =
        let cmd = Commands.createSyncParamInternal execute (fun _ -> true) false
        cmd

    member this.CommandSyncCheckedParam(execute, canExecute, ?dependentProperties: Expr list) =
        let cmd = Commands.createSyncParamInternal execute canExecute false
        this.addRaiseCE cmd dependentProperties
        cmd

    member this.CommandSyncCheckedUsingCommandManager(execute, canExecute) =
        Commands.createSyncParamInternal execute canExecute true :> ICommand

    member this.SetPropertyDependencies (prop : Expr, exprs: Expr list) =
        let name = getPropertyNameFromExpression prop
        let updCommand = fun () -> raisePropertyChanged(name)
        addTrackingActions updCommand exprs
    
    member this.RaiseCanExecuteChangedByProps (command, exprs: Expr list) =
        let updCommand = (fun () -> command.RaiseCanExecuteChanged())
        addTrackingActions updCommand exprs

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


    