namespace FsXaml

open System
open System.Windows
open System.Windows.Input
open System.Windows.Interactivity

type EventToMailbox() as self =
    inherit TriggerAction<DependencyObject>()

    let getMailbox() =
        match self.Mailbox with
        | null -> None
        | notNull -> Some notNull

    let getParameter (param : obj) =
        match self.MailboxParameter, self.PassEventArgsToMailbox with
        | (null, true) -> self.EventArgsConverter.Convert (param :?> EventArgs) self.EventArgsConverterParameter
        | param, _ -> param :> obj            

    // Dependency Properties
    static let mailboxProperty : DependencyProperty = DependencyProperty.Register("Mailbox", typeof<obj>, typeof<EventToMailbox>, UIPropertyMetadata(null))
    static let mailboxParameterProperty : DependencyProperty = DependencyProperty.Register("MailboxParameter", typeof<obj>, typeof<EventToMailbox>, UIPropertyMetadata(null))
    static let eventArgsConverterParameterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverterParameter", typeof<obj>, typeof<EventToMailbox>, UIPropertyMetadata(null))
    static let eventArgsConverterProperty : DependencyProperty = DependencyProperty.Register("EventArgsConverter", typeof<IEventArgsConverter>, typeof<EventToMailbox>, UIPropertyMetadata(Utilities.defaultEventArgsConverter))
    

    let mutable toggleIsEnabled = false

    // Dependency Property as properties - note that these should be static fields, but that's not possible with F#

    /// The MailboxProcessor<'a>
    static member MailboxProperty with get() = mailboxProperty
    
    /// The paramter passed to the MailboxProcessor<'a>.  If this is set, the EventArgs are ignored
    static member MailboxParameterProperty with get() = mailboxParameterProperty

    /// The converter used to map the EventArgs of the mailbox to the VM
    static member EventArgsConverterProperty with get() = eventArgsConverterProperty

    /// The converter parameter used to map the EventArgs of the mailbox to the VM
    static member EventArgsConverterParameterProperty with get() = eventArgsConverterParameterProperty

    // Dependency Property properties

    /// The MailboxProcessor
    member this.Mailbox
        with get() = this.GetValue(EventToMailbox.MailboxProperty)
        and set(v : obj) = this.SetValue(EventToMailbox.MailboxProperty, v)

    /// The paramter passed to the MailboxProcessor.  If this is set, the EventArgs are ignored
    member this.MailboxParameter
        with get() = this.GetValue(EventToMailbox.MailboxParameterProperty)
        and set(v) = this.SetValue(EventToMailbox.MailboxParameterProperty, v)

    /// Boolean indicating whether the EventArgs passed to the
    /// event handler will be forwarded to the Mailbox's Post method
    /// when the event is fired.  The EventArgsConverterParameter will get used 
    /// to map the types across as needed
    member val PassEventArgsToMailbox = true with get, set

    /// The optional converter used to convert the EventArgs into another type
    /// for passing to the Mailbox's parameter.  This allows strong typed conversion
    /// while preventing the ViewModel from requiring a hard binding to WPF
    member this.EventArgsConverter
        with get() : IEventArgsConverter = this.GetValue(EventToMailbox.EventArgsConverterProperty) :?> IEventArgsConverter
        and set(v : IEventArgsConverter) = this.SetValue(EventToMailbox.EventArgsConverterProperty, v :> obj)

    /// The converter used to map the EventArgs of the mailbox to the VM
    member this.EventArgsConverterParameter
        with get() = this.GetValue(EventToMailbox.EventArgsConverterParameterProperty)
        and set(v) = this.SetValue(EventToMailbox.EventArgsConverterParameterProperty, v)

    member private this.AssociatedFrameworkElement =
        match this.AssociatedObject with
        | :? FrameworkElement as fe -> Some fe
        | _ -> None


    override this.Invoke param =
        let mailbox = this.Mailbox 
        let ass = this.AssociatedObject           
        let associatedElementDisabled =
            match this.AssociatedFrameworkElement with
            | Some fe -> not fe.IsEnabled
            | _ -> true

        match associatedElementDisabled, mailbox with
        | (true, _) -> ()
        | (_, null) -> ()
        | (false, someMailbox) ->
            let parameter = getParameter param

            let mailboxType = someMailbox.GetType()
            let postMember = mailboxType.GetMethod("Post")
            try
                postMember.Invoke(someMailbox, [|parameter|]) |> ignore
            with
            | e -> System.Diagnostics.Debug.WriteLine(sprintf "EventToMailbox failed to Post: %s" e.Message) // Swallow exceptions on push

    override this.OnAttached() =
        base.OnAttached()