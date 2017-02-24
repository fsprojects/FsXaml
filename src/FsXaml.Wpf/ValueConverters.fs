namespace FsXaml

open System
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Controls
open System.Windows.Data


[<RequireQualifiedAccess>]
module DefaultConverters =
    let eventArgsIdConverter =
        {new IEventArgsConverter with
            member __.Convert (args : EventArgs) param =
                args :> obj }


type ConverterParams = { Parameter : obj; CultureInfo : Globalization.CultureInfo }

/// Base class for standard WPF style converters, mapped to curried forms for the convert and convert back methods
type ConverterBase(convertFunction, convertBackFunction) =    
    /// constructor take nullFunction as inputs
    new(convertFunction : (obj -> Type -> obj -> Globalization.CultureInfo -> obj)) = ConverterBase(convertFunction, ConverterBase.NotImplementedConverter)

    static member val NotImplementedConverter = fun (value : obj) (target : Type) (param : obj) (culture : Globalization.CultureInfo) -> raise(NotImplementedException())

    member val Convert : (obj -> Type -> obj -> Globalization.CultureInfo -> obj) = convertFunction with get, set 
    member val ConvertBack  : (obj -> Type -> obj -> Globalization.CultureInfo -> obj) = convertBackFunction with get, set 

    // implement the IValueConverter
    interface IValueConverter with
        /// convert a value to new value
        override this.Convert(value, targetType, parameter, culture) =
            this.Convert value targetType parameter culture

        /// convert a value back
        override this.ConvertBack(value, targetType, parameter, culture) =
            this.ConvertBack value targetType parameter culture

type Converter<'a,'b>(convertFunction : ('a -> ConverterParams -> 'b), defaultConvertOnFailure : 'b, convertBackFunction : ('b -> ConverterParams -> 'a), defaultConvertBackOnFailure : 'a) as self =
    inherit ConverterBase(ConverterBase.NotImplementedConverter, ConverterBase.NotImplementedConverter)

    let fWrapped (value : obj) (targetType : Type) (param : obj) (culture : Globalization.CultureInfo) = 
        let param = { Parameter = param ; CultureInfo = culture }        
        let a = FsXaml.Utilities.downcastAndCreateOption<'a>(value)
        let convType = typeof<System.IConvertible>
        match (targetType,typeof<'b>) with
        | x,b when x.IsAssignableFrom(b) ->
            match a with
            | None -> box defaultConvertOnFailure
            | Some v -> box(convertFunction v param)
        | x,b when convType.IsAssignableFrom(b) && convType.IsAssignableFrom(x) ->
            match a with
            | None -> box defaultConvertOnFailure
            | Some v -> Convert.ChangeType(box(convertFunction v param), x)
        | _,_ -> box defaultConvertOnFailure
    let fWrappedBack (value : obj) (targetType : Type) (param : obj) (culture : Globalization.CultureInfo) = 
        let param = { Parameter = param ; CultureInfo = culture }        
        let a = FsXaml.Utilities.downcastAndCreateOption<'b>(value)      
        let convType = typeof<System.IConvertible>
        match (targetType,typeof<'a>) with
        | x,b when x.IsAssignableFrom(b) ->
            match a with
            | None -> box defaultConvertBackOnFailure
            | Some v -> box(convertBackFunction v param)
        | x,b when convType.IsAssignableFrom(b) && convType.IsAssignableFrom(x) ->
            match a with
            | None -> box defaultConvertBackOnFailure
            | Some v -> Convert.ChangeType(box(convertBackFunction v param), x)
        | _,_ -> box defaultConvertBackOnFailure

    static let notImplementedForward (value : 'a) (p : ConverterParams) : 'b  = 
        raise(NotImplementedException())
    static let notImplementedBack (value : 'b) (p : ConverterParams) : 'a  = 
        raise(NotImplementedException())

    do
        self.Convert <- fWrapped
        self.ConvertBack <- fWrappedBack

    new (convertFunction : ('a -> ConverterParams -> 'b), defaultConvertOnFailure : 'b) =
        Converter(convertFunction, defaultConvertOnFailure, Converter.NotImplementedBackConverter, Unchecked.defaultof<'a>)

    static member val NotImplementedForwardConverter = notImplementedForward
    static member val NotImplementedBackConverter = notImplementedBack

type EventArgsConverterBase(convertFun) =
    interface IEventArgsConverter with
        member __.Convert args param = convertFun args param

type EventArgsConverter<'a, 'b when 'a :> EventArgs>(convertFun, defaultOnFailure : 'b) =
    inherit EventArgsConverterBase((fun value _ ->
            let a = FsXaml.Utilities.downcastAndCreateOption<'a>(value)
            let b = 
                match a with
                | None -> defaultOnFailure
                | Some(v) -> convertFun(v)
            box b))

type EventArgsParamConverter<'a, 'b, 'c when 'a :> EventArgs>(convertFun, defaultOnFailure : 'c) =
    inherit EventArgsConverterBase((fun value param ->
            let a = FsXaml.Utilities.downcastAndCreateOption<'a>(value)
            let b = FsXaml.Utilities.downcastAndCreateOption<'b>(param)
            let c = 
                match a, b with
                | Some(v), Some(p) -> convertFun v p
                | _, _ -> defaultOnFailure
            box c))

type ValidationErrorsToStringConverter() =
    interface System.Windows.Data.IMultiValueConverter with
        override this.Convert(values, targetType, parameter, culture) =
            match values.Length with
            | 0 -> null
            | _ ->
                match values.[0] with
                | :? ReadOnlyObservableCollection<ValidationError> as collection ->
                    let values = 
                        collection
                        |> Seq.map (fun v -> v.ErrorContent :?> string) 
                    String.Join(Environment.NewLine, values) :> obj                        
                | _ -> null
        override this.ConvertBack(value, targetType, parameter, culture) =
            raise(NotImplementedException())

type BooleanConverter<'a when 'a : equality>(trueValue : 'a, falseValue: 'a) =
    inherit ConverterBase
        ((fun b _ _ _ ->
            try
                let value : bool = unbox b 
                match value with
                | true -> box trueValue
                | false -> box falseValue
            with
            | _ -> DependencyProperty.UnsetValue),
        (fun v _ _ _ -> 
            try
                match v with
                | value when unbox value = trueValue -> box true
                | _ -> box false
            with
            | _ -> box false))
    
type BooleanToVisibilityConverter() =
    inherit BooleanConverter<Visibility>(Visibility.Visible, Visibility.Collapsed)

type BooleanToVisibilityOrHiddenConverter() =
    inherit BooleanConverter<Visibility>(Visibility.Visible, Visibility.Hidden)

type BooleanToCollapsedConverter() =
    inherit BooleanConverter<Visibility>(Visibility.Collapsed, Visibility.Visible)

type BooleanToInverseConverter() =
    inherit BooleanConverter<bool>(false, true)        