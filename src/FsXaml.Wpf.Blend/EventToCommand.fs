namespace FsXaml

open System
open System.Windows
open System.Windows.Input
open System.Windows.Interactivity

type EventToCommand() as self =
    inherit TriggerAction<DependencyObject>()

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
    static let eventArgsConverterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverter", typeof<IEventArgsConverter>, typeof<EventToCommand>, UIPropertyMetadata(Utilities.defaultEventArgsConverter))
    

    let mutable toggleIsEnabled = false

    // Dependency Property as properties - note that these should be static fields, but that's not possible with F#

    /// The ICommand
    static member CommandProperty with get() = commandProperty
    
    /// The paramter passed to the ICommand.  If this is set, the EventArgs are ignored
    static member CommandParameterProperty with get() = commandParameterProperty

    /// The converter used to map the EventArgs of the command to the VM
    static member EventArgsConverterProperty with get() = eventArgsConverterProperty

    /// The converter parameter used to map the EventArgs of the command to the VM
    static member EventArgsConverterParameterProperty with get() = eventArgsConverterParameterProperty

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


    override this.Invoke param =
        let command = this.Command 
        let ass = this.AssociatedObject           
        let associatedElementDisabled =
            match this.AssociatedFrameworkElement with
            | Some fe -> not fe.IsEnabled
            | _ -> true

        match associatedElementDisabled, command with
        | (true, _) -> ()
        | (_, null) -> ()
        | (false, someCommand) ->
            let parameter = getParameter param
            if someCommand.CanExecute(parameter) then 
                someCommand.Execute(parameter)

    override this.OnAttached() =
        base.OnAttached()
        this.EnableDisableElement()