namespace WpfSimpleDrawingApplication

open System
open System.Collections.ObjectModel
open ViewModule
open ViewModule.FSharp

type MainViewModel() as me =
    inherit EventViewModelBase<MoveEvent>()

    let lines = ObservableCollection<PointPair>()
    // Create a event value commands which push through MoveEvent from our binding converters
    let mouseCommand = me.Factory.EventValueCommand()
        
    let handleMove = function
        | PositionChanged(CaptureStatus.Captured, pt), PositionChanged(_, last) -> 
            lines.Add({ Start = last; End = pt } )
        | _, _ -> ()

    // Our drawing mechanism - listen to the events, and handle them
    do          
        me.EventStream
        |> Observable.pairwise
        |> Observable.subscribe handleMove
        |> ignore
    
    member this.MouseCommand = mouseCommand
    // Our lines for visual binding
    member __.Lines = lines    
