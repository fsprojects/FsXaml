open System
open FsXaml

type App = XAML<"App.xaml">

[<STAThread>]
[<EntryPoint>]
let main argv = 
    Wpf.installSynchronizationContext ()
    Wpf.installBlendSupport ()
    Views.MainWindow()
    |> App().Run 
