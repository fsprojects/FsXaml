open System
open FsXaml

type App = XAML<"App.xaml">
type Res = XAML<"ApplicationResources.xaml">

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let app = App();
    let res = Res()
    let conv = res.["validationConverter"]
    app.Run()
