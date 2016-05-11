open System
open FsXaml

type App = XAML<"App.xaml">

[<STAThread>]
[<EntryPoint>]
let main argv = 
    Wpf.installSynchronizationContext()
    App().Run(Views.MainWindow())
