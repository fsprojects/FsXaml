namespace FsXaml

open System
open System.Windows
open System.Windows.Input
open System.Windows.Interactivity

// TODO: Should I add a markup extension to map the binding for an ICommand -> EventToFSharpEvent?

type EventToFSharpEvent() as self =
    inherit TriggerAction<DependencyObject>()

    let getFSharpEvent() =
        match self.FSharpEvent with
        | null -> None
        | notNull -> Some notNull

    let getParameter (param : obj) =
        match self.FSharpEventParameter, self.PassEventArgsToFSharpEvent with
        | (null, true) -> self.EventArgsConverter.Convert (param :?> EventArgs) self.EventArgsConverterParameter
        | param -> param :> obj            

    // Dependency Properties
    static let fSharpEventProperty : DependencyProperty = DependencyProperty.Register("FSharpEvent", typeof<obj>, typeof<EventToFSharpEvent>, UIPropertyMetadata(null))
    static let fSharpEventParameterProperty : DependencyProperty = DependencyProperty.Register("FSharpEventParameter", typeof<obj>, typeof<EventToFSharpEvent>, UIPropertyMetadata(null))
    static let eventArgsConverterParameterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof<obj>, typeof<EventToFSharpEvent>, UIPropertyMetadata(null))
    static let eventArgsConverterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverter", typeof<IEventArgsConverter>, typeof<EventToFSharpEvent>, UIPropertyMetadata(Utilities.defaultEventArgsConverter))
    

    let mutable toggleIsEnabled = false

    // Dependency Property as properties - note that these should be static fields, but that's not possible with F#

    /// The MailboxProcessor<'a>
    static member FSharpEventProperty with get() = fSharpEventProperty
    
    /// The paramter passed to the MailboxProcessor<'a>.  If this is set, the EventArgs are ignored
    static member FSharpEventParameterProperty with get() = fSharpEventParameterProperty

    /// The converter used to map the EventArgs of the event to the VM
    static member EventArgsConverterProperty with get() = eventArgsConverterProperty

    /// The converter parameter used to map the EventArgs of the event to the VM
    static member EventArgsConverterParameterProperty with get() = eventArgsConverterParameterProperty

    // Dependency Property properties

    /// The MailboxProcessor
    member this.FSharpEvent
        with get() = this.GetValue(EventToFSharpEvent.FSharpEventProperty)
        and set(v : obj) = this.SetValue(EventToFSharpEvent.FSharpEventProperty, v)

    /// The paramter passed to the MailboxProcessor.  If this is set, the EventArgs are ignored
    member this.FSharpEventParameter
        with get() = this.GetValue(EventToFSharpEvent.FSharpEventParameterProperty)
        and set(v) = this.SetValue(EventToFSharpEvent.FSharpEventParameterProperty, v)

    /// Boolean indicating whether the EventArgs passed to the
    /// event handler will be forwarded to the event's Trigger method
    /// when the event is fired.  The EventArgsConverterParameter will get used 
    /// to map the types across as needed
    member val PassEventArgsToFSharpEvent = true with get, set

    /// The optional converter used to convert the EventArgs into another type
    /// for passing to the FSharpEvent's parameter.  This allows strong typed conversion
    /// while preventing the ViewModel from requiring a hard binding to WPF
    member this.EventArgsConverter
        with get() : IEventArgsConverter = this.GetValue(EventToFSharpEvent.EventArgsConverterProperty) :?> IEventArgsConverter
        and set(v : IEventArgsConverter) = this.SetValue(EventToFSharpEvent.EventArgsConverterProperty, v :> obj)

    /// The converter used to map the EventArgs of the event to the VM
    member this.EventArgsConverterParameter
        with get() = this.GetValue(EventToFSharpEvent.EventArgsConverterParameterProperty)
        and set(v) = this.SetValue(EventToFSharpEvent.EventArgsConverterParameterProperty, v)

    member private this.AssociatedFrameworkElement =
        match this.AssociatedObject with
        | :? FrameworkElement as fe -> Some fe
        | _ -> None


    override this.Invoke param =
        let event = this.FSharpEvent
        let ass = this.AssociatedObject           
        let associatedElementDisabled =
            match this.AssociatedFrameworkElement with
            | Some fe -> not fe.IsEnabled
            | _ -> true

        match associatedElementDisabled, event with
        | (true, _) -> ()
        | (_, null) -> ()
        | (false, someEvent) ->
            let parameter = getParameter param

            let eventType = someEvent.GetType()
            let triggerMember = eventType.GetMethod("Trigger")
            try
                triggerMember.Invoke(someEvent, [|parameter|]) |> ignore
            with
            | e -> System.Diagnostics.Debug.WriteLine(sprintf "EventToFSharpEvent failed to Trigger: %s" e.Message) // Swallow exceptions on push

    override this.OnAttached() =
        base.OnAttached()