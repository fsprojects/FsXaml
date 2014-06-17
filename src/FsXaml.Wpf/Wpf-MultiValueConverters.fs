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
            match values.[0] with
            | v when v = DependencyProperty.UnsetValue ->
                null
            | _ ->
                let collection = values.[0] :?> ReadOnlyObservableCollection<ValidationError>
                let values = 
                    collection
                    |> Seq.map (fun v -> v.ErrorContent :?> string) 
                String.Join(Environment.NewLine, values) :> obj                        
        override this.ConvertBack(value, targetType, parameter, culture) =
            raise(NotImplementedException())
