namespace FsXaml

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup

type public ViewController() =
    static let CustomChanged(target : DependencyObject) (eventArgs : DependencyPropertyChangedEventArgs) : unit =
        let fe = Utilities.castAs<FrameworkElement> target
        let behaviorType = Utilities.castAs<Type> eventArgs.NewValue
        if fe <> null && behaviorType <> null then
            let controller = Utilities.castAs<IViewController> <| Activator.CreateInstance behaviorType
            if controller <> null then
                fe.Loaded.Add(fun a -> controller.Attach fe)

    static let CustomProperty : DependencyProperty = DependencyProperty.RegisterAttached("Custom", typeof<Type>, typeof<ViewController>, new UIPropertyMetadata(null, new PropertyChangedCallback(CustomChanged)))

    static member SetCustom(obj : DependencyObject, behaviorType: Type) =
        obj.SetValue(CustomProperty, behaviorType)

    static member GetCustom(obj : DependencyObject) =
        obj.GetValue(CustomProperty) :?> Type




