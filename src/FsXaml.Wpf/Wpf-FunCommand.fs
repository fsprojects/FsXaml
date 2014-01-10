namespace FsXaml.Internal

open System
open System.Threading
open System.Windows
open System.Windows.Input

open FsXaml

type internal FunCommand (execute : (obj -> unit), canExecute, ?useCommandManager : bool) as self =
    let usingCM = defaultArg useCommandManager false
    let canExecuteChangedInternal = new Event<EventHandler, EventArgs>()

    let mutable em = execute

    // This is noticably ugly, but required as we need to have two potential implementations of the command targets:
    // 1) Use the CommandManager (default for RoutedEvents) or
    // 2) Use an internal, weak event manager to prevent WPF memory leaks
    // Note that this shouldn't happen as of .NET 4.5 (due to the usage of CanExecuteChangedWeakEventManager in the framework), but some
    // third party control libraries still leak, as well as any .NET 4 or earlier libs that don't reroute to 4.5's ButtonBase/MenuItem/etc
    // By handling this ourselves, we guarantee no leaks
    let canExChInternal = Event<EventHandler,EventArgs>()
    let canExecuteChanged = { new IEvent<EventHandler, EventArgs> with 
                                    member x.AddHandler(d) = 
                                        match usingCM with
                                        | true -> CommandManager.RequerySuggested.AddHandler d
                                        | false -> CanExecuteWeakEventManager.AddHandler self d
                                    member x.RemoveHandler(d) =
                                        match usingCM with
                                        | true -> CommandManager.RequerySuggested.RemoveHandler d
                                        | false -> CanExecuteWeakEventManager.RemoveHandler self d
                                    member x.Subscribe(observer) =
                                        let h = new Handler<_>(fun sender args -> observer.OnNext(args))
                                        (x :?> IEvent<_,_>).AddHandler(h)
                                        { new System.IDisposable with 
                                             member x.Dispose() = (x :?> IEvent<_,_>).RemoveHandler(h) } }

    new(asyncExecute, canExecute) as self =        
        let ui = SynchronizationContext.Current
        let executing = ref false
        let ce = (fun a -> (not !executing) && canExecute(a))
        FunCommand((fun a -> a |> ignore), ce, false)
        then
            let idg = self :> INotifyCommand
            let exec param = 
                executing := true                
                idg.RaiseCanExecuteChanged()
                async {
                    do! asyncExecute ui param
                    do! Async.SwitchToContext(ui)
                    executing := false
                    idg.RaiseCanExecuteChanged()
                } |> Async.Start
            self.executeMethod <- exec

    member internal this.CanExecuteChangedInternal = canExChInternal.Publish
    member val private executeMethod = em with get, set

    interface INotifyCommand with
        member this.RaiseCanExecuteChanged() =
            match usingCM with
            | false -> canExChInternal.Trigger(self, EventArgs.Empty)
            | true -> CommandManager.InvalidateRequerySuggested()            

    interface ICommand with
        [<CLIEvent>]
        member this.CanExecuteChanged = canExecuteChanged

        member this.CanExecute(param : obj) =
            canExecute(param)

        member this.Execute(param : obj) =
            this.executeMethod(param)

and [<AllowNullLiteral>] private CanExecuteWeakEventManager() =
    inherit WeakEventManager()

    static member CurrentManager 
        with get() =
            let mtype = typeof<CanExecuteWeakEventManager>
            let mgr = WeakEventManager.GetCurrentManager mtype

            match mgr with
            | null -> 
                let mgr = CanExecuteWeakEventManager()
                WeakEventManager.SetCurrentManager(mtype, mgr)
                mgr
            | _ -> mgr :?> CanExecuteWeakEventManager

    member private this.OnEvent (sender: obj) (args: EventArgs) =
        this.DeliverEvent(sender, args)

    static member AddHandler(source : FunCommand) (handler : EventHandler) =
        CanExecuteWeakEventManager.CurrentManager.ProtectedAddHandler(source, handler)
    static member RemoveHandler(source : FunCommand) (handler : EventHandler) =
        CanExecuteWeakEventManager.CurrentManager.ProtectedRemoveHandler(source, handler)

    override this.NewListenerList() =
        new WeakEventManager.ListenerList()

    override this.StartListening(source) =
        let cmd = source :?> FunCommand
        cmd.CanExecuteChangedInternal.AddHandler <| EventHandler(this.OnEvent)

    override this.StopListening(source) =
        let cmd = source :?> FunCommand
        cmd.CanExecuteChangedInternal.RemoveHandler <| EventHandler(this.OnEvent)

    
/// Module containing Command factory methods to create ICommand implementations
module internal Commands =
    let createSyncInternal execute canExecute useCommandManager =
        let ceWrapped = fun _ -> canExecute()
        let func = (fun _ -> execute())
        FunCommand(func, ceWrapped, useCommandManager) :> INotifyCommand    

    let createAsyncInternal (asyncWorkflow : (SynchronizationContext -> Async<unit>)) canExecute =
        FunCommand((fun ui p -> asyncWorkflow(ui)), fun o -> canExecute()) :> INotifyCommand

    let createSyncParamInternal<'a> (execute : ('a -> unit)) (canExecute : ('a -> bool)) useCommandManager =
        let ceWrapped o = 
            let a = Utilities.downcastAndCreateOption(o)            
            match a with
            | None -> false
            | Some v -> canExecute(v)

        let func o = 
            let a = Utilities.downcastAndCreateOption(o)
            match a with
            | None -> a |> ignore
            | Some v -> execute(v)

        let result = FunCommand(func, ceWrapped, useCommandManager) :> INotifyCommand

        // Note that we need to handle the fact that the arg is passed as null the first time, due to stupid data binding issues.  Let's fix that here.
        // This will cause the command to requery the CanExecute method after everything's loaded, which will then pass onto the user's canExecute function.
        // The first time things are loaded, since null will be passed, None will go through, and the method won't execute
        let rec callback = fun e -> 
            result.RaiseCanExecuteChanged()
            subscription.Dispose()
        and subscription : IDisposable = CommandManager.RequerySuggested.Subscribe(callback)

        result

    let createAsyncParamInternal<'a> (asyncWorkflow : (SynchronizationContext -> 'a -> Async<unit>)) (canExecute : ('a -> bool)) =
        let ceWrapped o = 
            let a = Utilities.downcastAndCreateOption(o)            
            match a with
            | None -> false
            | Some v -> canExecute(v)

        // Handler for when param type doesn't match - does nothing
        let emptyFunc (ui : SynchronizationContext) (a : obj) : Async<unit> = async { () }

        // Build a handler that converts the untyped param to our typed param
        let func (ui : SynchronizationContext) (o : obj) = 
            let a = Utilities.downcastAndCreateOption(o)
            match a with
            | None -> emptyFunc ui o 
            | Some v -> asyncWorkflow ui v

        let result = FunCommand(func, ceWrapped) :> INotifyCommand

        // Note that we need to handle the fact that the arg is passed as null the first time, due to stupid data binding issues.  Let's fix that here.
        // This will cause the command to requery the CanExecute method after everything's loaded, which will then pass onto the user's canExecute function.
        // The first time things are loaded, since null will be passed, None will go through, and the method won't execute
        let subscription : IDisposable ref = ref null
        subscription := CommandManager.RequerySuggested.Subscribe(fun e -> 
            result.RaiseCanExecuteChanged()
            subscription.Value.Dispose())

        result
