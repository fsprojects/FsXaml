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
