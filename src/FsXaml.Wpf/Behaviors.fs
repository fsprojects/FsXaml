namespace FsXaml

open System
open System.Windows
open System.Windows.Controls.Primitives
open System.Windows.Input

type AutoSelectText() =    
    static let onLoadedChanged (t : DependencyObject) (e : DependencyPropertyChangedEventArgs) =
        let hook = e.NewValue :?> bool

        match t, hook with
        | :? TextBoxBase as textBox, true ->            
            ignore <| 
                textBox.Loaded.Subscribe (fun t -> 
                    textBox.SelectAll()
                    textBox.Focus() |> ignore)
        | _, _ -> ()

    static let onLoadedProperty : DependencyProperty = DependencyProperty.RegisterAttached("OnLoaded", typeof<bool>, typeof<AutoSelectText>, UIPropertyMetadata(onLoadedChanged))
    static member OnLoadedProperty with get() = onLoadedProperty 

    static member GetOnLoaded (depObj : DependencyObject) =
        unbox<bool>(depObj.GetValue(AutoSelectText.OnLoadedProperty))
    
    static member SetOnLoaded (depObj : DependencyObject) (value : bool) =
        depObj.SetValue(AutoSelectText.OnLoadedProperty, box(value))


type DefaultButton() =
    static let rec getParentWindow (depObj : DependencyObject) =
        let parent = System.Windows.Media.VisualTreeHelper.GetParent(depObj)
        match parent with
        | null -> null
        | :? Window as window -> window
        | _ -> getParentWindow parent

    static let onDialogResultOnClickChanged (t : DependencyObject) (e : DependencyPropertyChangedEventArgs) =
        let value = e.NewValue :?> Nullable<bool>

        match t with
        | :? ButtonBase as button ->            
            ignore <| 
                button.Click.Subscribe (fun t -> 
                    let window = getParentWindow button
                    match window with
                    | null -> ()
                    | _ -> window.DialogResult <- value)
        | _ -> ()

    static let dialogResultOnClickProperty : DependencyProperty = DependencyProperty.RegisterAttached("DialogResultOnClick", typeof<Nullable<bool>>, typeof<AutoSelectText>, UIPropertyMetadata(onDialogResultOnClickChanged))
    static member DialogResultOnClickProperty with get() = dialogResultOnClickProperty 

    static member GetDialogResultOnClick (depObj : DependencyObject) =
        unbox<Nullable<bool>>(depObj.GetValue(DefaultButton.DialogResultOnClickProperty))
    
    static member SetDialogResultOnClick (depObj : DependencyObject) (value : Nullable<bool>) =
        depObj.SetValue(DefaultButton.DialogResultOnClickProperty, box(value))
    