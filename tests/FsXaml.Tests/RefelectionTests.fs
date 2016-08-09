module FsXaml.RefelectionTests

open System
open FsXaml
open NUnit.Framework

type RelayObserver<'t>(onNext) = 
    let onNext = onNext
    interface IObserver<'t> with
        member __.OnNext value = onNext value
        member __.OnError error = ()
        member __.OnCompleted() = ()

[<Test>]
let ``invoke OnNext``() = 
    let mutable sum = 0
    let increment i = sum <- sum + i
    let observer = RelayObserver(increment)
    Reflection.invoke observer "OnNext" 2 |> ignore
    Assert.AreEqual(2, sum)
