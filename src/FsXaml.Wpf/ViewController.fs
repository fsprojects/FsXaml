namespace FsXaml

open System
open System.Windows
open System.Windows.Controls

type public ViewController() =
    static let CustomChanged(target : DependencyObject) (eventArgs : DependencyPropertyChangedEventArgs) : unit =        
        let fe = Utilities.castAs<FrameworkElement> target
        let behaviorType = Utilities.castAs<Type> eventArgs.NewValue
        if fe <> null && behaviorType <> null then
            if not(System.ComponentModel.DesignerProperties.GetIsInDesignMode(fe)) then
                let vc = Activator.CreateInstance behaviorType
                let controller = vc :?> IViewController
                fe.Initialized.Add(fun _ -> controller.Initialized fe)
                fe.Loaded.Add(fun _ -> controller.Loaded fe)
                fe.Unloaded.Add(fun _ -> controller.Unloaded fe)

    static let CustomProperty : DependencyProperty = DependencyProperty.RegisterAttached("Custom", typeof<Type>, typeof<ViewController>, new UIPropertyMetadata(null, new PropertyChangedCallback(CustomChanged)))

    static member SetCustom(obj : DependencyObject, behaviorType: Type) =
        obj.SetValue(CustomProperty, behaviorType)

    static member GetCustom(obj : DependencyObject) =
        obj.GetValue(CustomProperty) :?> Type



[<AbstractClass>]
type public ViewControllerBase<'T, 'U when 'U :> FrameworkElement>() =
    let subscriptions = 
        ResizeArray<System.IDisposable>()

    /// Called when the item and its children have been initialized
    abstract member OnInitialized : 'T -> unit
    default __.OnInitialized _ = ()

    /// Called when the item is loaded to the visual tree
    abstract member OnLoaded : 'T -> unit
    default __.OnLoaded _ = ()

    /// Called when the item is unloaded from the visual tree
    abstract member OnUnloaded : 'T -> unit
    default __.OnUnloaded _ = ()

    /// Adds an IDisposable to the list of objects which will get Dispose called when the item unloads from the visual tree
    member __.DisposeOnUnload idisposable =
        subscriptions.Add(idisposable)

    interface IViewController with
        member this.Initialized fe =
            match fe with
            | :? 'U as typed -> 
                let t = System.Activator.CreateInstance(typeof<'T>, typed) :?> 'T
                this.OnInitialized(t)
            | _ -> ()
        member this.Loaded fe =
            match fe with
            | :? 'U as typed -> 
                let t = System.Activator.CreateInstance(typeof<'T>, typed) :?> 'T
                this.OnLoaded(t)
            | _ -> ()
        member this.Unloaded fe =
            // Clear our subscriptions prior to unloading
            subscriptions |> Seq.iter (fun x -> x.Dispose())
            subscriptions.Clear()

            match fe with
            | :? 'U as typed -> 
                let t = System.Activator.CreateInstance(typeof<'T>, typed) :?> 'T
                this.OnUnloaded(t)
            | _ -> ()

[<AbstractClass>]
/// Strongly typed base class for Window ViewControllers
type public WindowViewController<'T when 'T :> XamlTypeFactory<Window>>() =
    inherit ViewControllerBase<'T, Window>()

[<AbstractClass>]
/// Strongly typed base class for UserControl ViewControllers
type public UserControlViewController<'T when 'T :> XamlContainer>() =
    inherit ViewControllerBase<'T, UserControl>()

