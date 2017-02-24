namespace FsXaml

open System
open System.Windows
open System.Windows.Input
open System.Windows.Interactivity

open System.Reflection
open Microsoft.FSharp.Quotations.Patterns

type EventToCommand() as self =
    inherit TriggerAction<DependencyObject>()

    let getModuleType = function
        | Call (_, methodInfo, _) -> methodInfo.DeclaringType
        | _ -> failwith "Expression is not a method."
    let optionModuleType = getModuleType <@ Option.isSome None @>

    let getCommand() =
        match self.Command with
        | null -> None
        | notNull -> Some notNull

    let enableDisableElement() =        
        match self.AssociatedFrameworkElement, self.ToggleIsEnabled, getCommand() with
        | (Some fe : FrameworkElement option, true, Some command) ->
            let enabled = command.CanExecute(self.CommandParameter)
            fe.IsEnabled <- enabled
        | _ -> ()

    let getParameter (param : obj) =
        match self.CommandParameter, self.PassEventArgsToCommand with
        | (null, true) -> self.EventArgsConverter.Convert (param :?> EventArgs) self.EventArgsConverterParameter
        | param -> param :> obj            

    let onCommandCanExecuteChanged sender handler =
        enableDisableElement()

    static let onCommandChanged (s : DependencyObject) (e : DependencyPropertyChangedEventArgs) =        
        let sender = s :?> EventToCommand
        match e.OldValue with
        | :? ICommand as ec -> ec.CanExecuteChanged.RemoveHandler(sender.OnCommandCanExecuteChanged)
        | _ -> ()

        match e.NewValue with
        | :? ICommand as ec -> ec.CanExecuteChanged.AddHandler(sender.OnCommandCanExecuteChanged)
        | _ -> ()

        sender.EnableDisableElement()


    static let onCommandParameterChanged (s : DependencyObject) e =
        let sender = s :?> EventToCommand
        sender.EnableDisableElement()

    // Dependency Properties
    static let commandProperty : DependencyProperty = DependencyProperty.Register("Command", typeof<ICommand>, typeof<EventToCommand>, UIPropertyMetadata(PropertyChangedCallback(onCommandChanged)))
    static let commandParameterProperty : DependencyProperty = DependencyProperty.Register("CommandParameter", typeof<obj>, typeof<EventToCommand>, UIPropertyMetadata(PropertyChangedCallback(onCommandParameterChanged)))
    static let eventArgsConverterParameterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof<obj>, typeof<EventToCommand>, UIPropertyMetadata(null))
    static let eventArgsConverterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverter", typeof<IEventArgsConverter>, typeof<EventToCommand>, UIPropertyMetadata(DefaultConverters.eventArgsIdConverter))
    static let filterOptionEventArgsProperty : DependencyProperty = DependencyProperty.Register("FilterOptionEventArgs", typeof<bool>, typeof<EventToCommand>, UIPropertyMetadata(false))    

    let mutable toggleIsEnabled = false
    
    /// The ICommand
    static member CommandProperty with get() = commandProperty
    
    /// The paramter passed to the ICommand.  If this is set, the EventArgs are ignored
    static member CommandParameterProperty with get() = commandParameterProperty

    /// The converter used to map the EventArgs of the command to the VM
    static member EventArgsConverterProperty with get() = eventArgsConverterProperty

    /// The converter parameter used to map the EventArgs of the command to the VM
    static member EventArgsConverterParameterProperty with get() = eventArgsConverterParameterProperty

    /// Option which allows event args to be set to None to prevent command execution
    static member FilterOptionEventArgsProperty with get () = filterOptionEventArgsProperty

    // Dependency Property properties

    /// The ICommand
    member this.Command
        with get() : ICommand = this.GetValue(EventToCommand.CommandProperty) :?> ICommand
        and set(v : ICommand) = this.SetValue(EventToCommand.CommandProperty, v :> obj)

    /// The paramter passed to the ICommand.  If this is set, the EventArgs are ignored
    member this.CommandParameter
        with get() = this.GetValue(EventToCommand.CommandParameterProperty)
        and set(v) = this.SetValue(EventToCommand.CommandParameterProperty, v)

    /// Boolean indicating whether the EventArgs passed to the
    /// event handler will be forwarded to the ICommand's Execute method
    /// when the event is fired.  The EventArgsConverterParameter will get used 
    /// to map the types across as needed
    member val PassEventArgsToCommand = true with get, set

    /// The optional converter used to convert the EventArgs into another type
    /// for passing to the ICommand's parameter.  This allows strong typed conversion
    /// while preventing the ViewModel from requiring a hard binding to WPF
    member this.EventArgsConverter
        with get() : IEventArgsConverter = this.GetValue(EventToCommand.EventArgsConverterProperty) :?> IEventArgsConverter
        and set(v : IEventArgsConverter) = this.SetValue(EventToCommand.EventArgsConverterProperty, v :> obj)

    /// The converter used to map the EventArgs of the command to the VM
    member this.EventArgsConverterParameter
        with get() = this.GetValue(EventToCommand.EventArgsConverterParameterProperty)
        and set(v) = this.SetValue(EventToCommand.EventArgsConverterParameterProperty, v)

    /// When set to true, event args that evaluate to option types are unwrapped and used as a filter
    member this.FilterOptionEventArgs 
        with get() : bool = this.GetValue(EventToCommand.FilterOptionEventArgsProperty) :?> _
        and set(v : bool) = this.SetValue(EventToCommand.FilterOptionEventArgsProperty, v)

    /// Boolean indicating whether to toggle the enabled value of the associated control
    /// based on the ICommand's CanExecute state
    member this.ToggleIsEnabled
        with get() = toggleIsEnabled
        and set(v) = 
            toggleIsEnabled <- v
            this.EnableDisableElement()

    member private this.EnableDisableElement = enableDisableElement
    member private this.OnCommandCanExecuteChanged = EventHandler(onCommandCanExecuteChanged)
    member private this.AssociatedFrameworkElement =
        match this.AssociatedObject with
        | :? FrameworkElement as fe -> Some fe
        | _ -> None

    member private this.Execute (p : obj) =
        if this.Command.CanExecute(p) then 
            this.Command.Execute(p)        

    member private this.ExecuteOption<'a> (p : 'a option) =
        p |> Option.iter this.Execute 

    override this.Invoke param =
        let command = this.Command 
        let ass = this.AssociatedObject           
        let associatedElementDisabled =
            match this.AssociatedFrameworkElement with
            | Some fe -> not fe.IsEnabled
            | _ -> true
        
        let unwrapOptionAndExecute p =
            if this.FilterOptionEventArgs then
                if p <> null then
                    let t = p.GetType ()
                    if t.IsGenericType && t.GetGenericTypeDefinition () = typedefof<Option<_>> then     
                        let args = t.GetGenericArguments ()
                        let exec = this.GetType().GetMethod("ExecuteOption", BindingFlags.Instance ||| BindingFlags.NonPublic)
                        let exec' = exec.MakeGenericMethod args
                        exec'.Invoke(this, [| p |]) |> ignore
                    else
                        this.Execute p
            else
                this.Execute p

        match associatedElementDisabled, command with
        | (true, _) -> ()
        | (_, null) -> ()
        | (false, _) ->
            let parameter = getParameter param
            unwrapOptionAndExecute parameter

    override this.OnAttached() =
        base.OnAttached()
        this.EnableDisableElement()