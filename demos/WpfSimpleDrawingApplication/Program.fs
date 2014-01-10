open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open FsXaml


type App = XAML<"App.xaml">

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let app = App().CreateRoot()
    app.Run()
