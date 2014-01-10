namespace WpfSimpleDrawingApplication

open System
open System.Collections.ObjectModel
open System.ComponentModel

open FsXaml

type PointPair = { Start : Point; End : Point }

type MainViewModel() as me =
    inherit ViewModelBase()

    let ptEvent = Event<Point>()
    let lines = ObservableCollection<PointPair>()
    
    let mutable position = { X = 0.0; Y = 0.0 }
    let mutable mouseButtonState = Released

    // Handles mouse capture for drawing
    let mouseDownEvent args =
        mouseButtonState <- fst args
        position <- snd args

    // Our drawing mechanism
    let draw =
        ptEvent.Publish :> IObservable<Point>
        |> Observable.filter (fun pt -> mouseButtonState = Captured)
        |> Observable.subscribe 
            (fun pt ->                 
                lines.Add({ Start = position; End = pt } )
                position <- pt
                )           

    // Create a synchronous command which works with an argument
    let mouseDownCmd = me.Factory.CommandSyncParam(mouseDownEvent)

    // Our actual properties (for data binding in XAML)
    member this.MouseDownCommand = mouseDownCmd
    member this.MoveEvent = ptEvent
    member this.Lines = lines    
