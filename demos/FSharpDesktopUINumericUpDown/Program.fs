open System
open System.Windows
open System.Windows.Data
open System.Windows.Input

open FSharp.Desktop.UI

// This is a port of the FSharp.Desktop.UI NumericUpDown sample
// which uses FsXaml to load the UI instead of building
// via code directly - Based on code from:
//     http://fsprojects.github.io/FSharp.Desktop.UI/tutorial_numeric_up_down.html
//     https://github.com/fsprojects/FSharp.Desktop.UI/blob/master/samples/NumericUpDown/Program.fs

type App = FsXaml.XAML<"App.xaml">
// Pass true to expose all named properties as public properties of MainWindow
type MainWindow = FsXaml.XAML<"MainWindow.xaml", true>

[<AbstractClass>]
type NumericUpDownModel() = 
    inherit Model()

    abstract Value: int with get, set

type NumericUpDownEvents = Up | Down

type NumericUpDownView(root : MainWindow) = 
    inherit View<NumericUpDownEvents, NumericUpDownModel, Window>(root.Root)        
    
    //View implementation 
    override this.EventStreams = [        
        root.upButton.Click |> Observable.map (fun _ -> Up)
        root.downButton.Click |> Observable.map (fun _ -> Down)

        root.input.KeyUp |> Observable.choose (fun args -> 
            match args.Key with 
            | Key.Up -> Some Up  
            | Key.Down -> Some Down
            | _ ->  None
        )

        root.input.MouseWheel |> Observable.map (fun args -> if args.Delta > 0 then Up else Down)
    ]

    override this.SetBindings model =   
        let root = MainWindow(this.Root)
        Binding.OfExpression 
            <@
                root.input.Text <- coerce model.Value
                //'coerce' means "use WPF default conversions"
            @> 

let eventHandler event (model: NumericUpDownModel) =
    match event with
    | Up -> model.Value <- model.Value + 1
    | Down -> model.Value <- model.Value - 1

let controller = Controller.Create eventHandler

[<STAThread>]
[<EntryPoint>]
let main _ = 
    let model = NumericUpDownModel.Create()

    // Create App() first so global styles are available
    let app = App().Root
    let view = NumericUpDownView(MainWindow())

    let mvc = Mvc(model, view, controller)
    use __ = mvc.Start()
    app.Run(view.Root) 
