namespace FsXaml

open System
open System.ComponentModel
open System.Windows
open System.Windows.Data
open System.Windows.Input


type ConverterParams = { Parameter : obj; CultureInfo : Globalization.CultureInfo }

/// Base class for standard WPF style converters, mapped to curried forms for the convert and convert back methods
type ConverterBase(convertFunction, convertBackFunction) =    
    /// constructor take nullFunction as inputs
    new(convertFunction) = ConverterBase(convertFunction, ConverterBase.NotImplementedConverter)

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
        match targetType with
        | x when x = typeof<'b> ->
            match a with
            | None -> defaultConvertOnFailure :> obj
            | Some v -> convertFunction v param :> obj
        | _ -> defaultConvertOnFailure :> obj
    let fWrappedBack (value : obj) (targetType : Type) (param : obj) (culture : Globalization.CultureInfo) = 
        let param = { Parameter = param ; CultureInfo = culture }        
        let a = FsXaml.Utilities.downcastAndCreateOption<'b>(value)      
        match targetType with
        | x when x = typeof<'a> ->
            match a with
            | None -> defaultConvertBackOnFailure :> obj
            | Some v -> convertBackFunction v param :> obj
        | _ -> defaultConvertBackOnFailure :> obj

    do
        self.Convert <- fWrapped
        self.ConvertBack <- fWrappedBack

    new (convertFunction : ('a -> ConverterParams -> 'b), defaultConvertOnFailure : 'b) =
        Converter(convertFunction, defaultConvertOnFailure, Converter.NotImplementedBackConverter, Unchecked.defaultof<'a>)

    static member val NotImplementedForwardConverter = fun (value : 'a) (p : ConverterParams) -> raise(NotImplementedException())
    static member val NotImplementedBackConverter = fun (value : 'b) (p : ConverterParams) -> raise(NotImplementedException())
