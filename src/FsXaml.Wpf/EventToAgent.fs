namespace FsXaml

open System
open System.Windows
open System.Windows.Input
open System.Windows.Interactivity

// TODO: Should I add a markup extension to map the binding for an ICommand -> EventToAgent?

type EventToAgent() as self =
    inherit TriggerAction<DependencyObject>()

    let getAgent() =
        match self.Agent with
        | null -> None
        | notNull -> Some notNull

    let getParameter (param : obj) =
        match self.AgentParameter, self.PassEventArgsToAgent with
        | (null, true) -> self.EventArgsConverter.Convert (param :?> EventArgs) self.EventArgsConverterParameter
        | param -> param :> obj            

    // Dependency Properties
    static let agentProperty : DependencyProperty = DependencyProperty.Register("Agent", typeof<obj>, typeof<EventToAgent>, UIPropertyMetadata(null))
    static let agentParameterProperty : DependencyProperty = DependencyProperty.Register("AgentParameter", typeof<obj>, typeof<EventToAgent>, UIPropertyMetadata(null))
    static let eventArgsConverterParameterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof<obj>, typeof<EventToAgent>, UIPropertyMetadata(null))
    static let eventArgsConverterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverter", typeof<IEventArgsConverter>, typeof<EventToAgent>, UIPropertyMetadata(Utilities.defaultEventArgsConverter))
    

    let mutable toggleIsEnabled = false

    // Dependency Property as properties - note that these should be static fields, but that's not possible with F#

    /// The MailboxProcessor<'a>
    static member AgentProperty with get() = agentProperty
    
    /// The paramter passed to the MailboxProcessor<'a>.  If this is set, the EventArgs are ignored
    static member AgentParameterProperty with get() = agentParameterProperty

    /// The converter used to map the EventArgs of the agent to the VM
    static member EventArgsConverterProperty with get() = eventArgsConverterProperty

    /// The converter parameter used to map the EventArgs of the agent to the VM
    static member EventArgsConverterParameterProperty with get() = eventArgsConverterParameterProperty

    // Dependency Property properties

    /// The MailboxProcessor
    member this.Agent
        with get() = this.GetValue(EventToAgent.AgentProperty)
        and set(v : obj) = this.SetValue(EventToAgent.AgentProperty, v)

    /// The paramter passed to the MailboxProcessor.  If this is set, the EventArgs are ignored
    member this.AgentParameter
        with get() = this.GetValue(EventToAgent.AgentParameterProperty)
        and set(v) = this.SetValue(EventToAgent.AgentParameterProperty, v)

    /// Boolean indicating whether the EventArgs passed to the
    /// event handler will be forwarded to the Agent's Post method
    /// when the event is fired.  The EventArgsConverterParameter will get used 
    /// to map the types across as needed
    member val PassEventArgsToAgent = true with get, set

    /// The optional converter used to convert the EventArgs into another type
    /// for passing to the Agent's parameter.  This allows strong typed conversion
    /// while preventing the ViewModel from requiring a hard binding to WPF
    member this.EventArgsConverter
        with get() : IEventArgsConverter = this.GetValue(EventToAgent.EventArgsConverterProperty) :?> IEventArgsConverter
        and set(v : IEventArgsConverter) = this.SetValue(EventToAgent.EventArgsConverterProperty, v :> obj)

    /// The converter used to map the EventArgs of the agent to the VM
    member this.EventArgsConverterParameter
        with get() = this.GetValue(EventToAgent.EventArgsConverterParameterProperty)
        and set(v) = this.SetValue(EventToAgent.EventArgsConverterParameterProperty, v)

    member private this.AssociatedFrameworkElement =
        match this.AssociatedObject with
        | :? FrameworkElement as fe -> Some fe
        | _ -> None


    override this.Invoke param =
        let agent = this.Agent 
        let ass = this.AssociatedObject           
        let associatedElementDisabled =
            match this.AssociatedFrameworkElement with
            | Some fe -> not fe.IsEnabled
            | _ -> true

        match associatedElementDisabled, agent with
        | (true, _) -> ()
        | (_, null) -> ()
        | (false, someAgent) ->
            let parameter = getParameter param

            let agentType = someAgent.GetType()
            let postMember = agentType.GetMethod("Post")
            try
                postMember.Invoke(someAgent, [|parameter|]) |> ignore
            with
            | e -> System.Diagnostics.Debug.WriteLine(sprintf "EventToAgent failed to Post: %s" e.Message) // Swallow exceptions on push

    override this.OnAttached() =
        base.OnAttached()