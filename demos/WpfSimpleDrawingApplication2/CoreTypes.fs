namespace WpfSimpleDrawingApplication2

open System

type Point = { X: float; Y: float }
type PointPair = { Start : Point; End : Point }

type RelayObserver<'t>(onNext) =
    let onNext = onNext
    interface IObserver<'t> with
        member __.OnNext value = onNext value
        member __.OnError error = ()
        member __.OnCompleted() = ()

