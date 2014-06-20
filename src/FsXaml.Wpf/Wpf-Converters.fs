namespace FsXaml

open System
open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Input

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
    inherit FsXaml.ConverterBase
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