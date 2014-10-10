namespace FsXaml

open System.Collections.Generic
open System.Windows

/// Provides access to named children of a FrameworkElement
type XamlFileAccessor(root : FrameworkElement) =
    let dict = new Dictionary<_,_>()
        
    /// Creates an accessor directly from a resource location, loading the resource    
    new(resourceLocation: string) as self =
        let root : FrameworkElement = LoadXaml.from resourceLocation (self.GetType())
        XamlFileAccessor(root)    

    /// Gets a named child element by name
    member this.GetChild name = 
        match dict.TryGetValue name with
        | true,element -> element
        | false,element -> 
            let element = root.FindName name
            let element' = 
                match element with
                | null ->
                    // Fallback to searching the logical tree if our template hasn't been applied
                    LogicalTreeHelper.FindLogicalNode(root, name) :> obj
                | _ -> 
                    element
            dict.[name] <- element'
            element'

    /// The root element of the XAML document
    member this.Root = root

/// Provides access to keyed children of a ResourceDictionary
type XamlResourceAccessor(root : ResourceDictionary) =
    let dict = new Dictionary<_,_>()
    
    /// Creates an accessor directly from a resource location, loading the resource    
    new(resourceLocation: string) as self =
        let root : ResourceDictionary = LoadXaml.from resourceLocation (self.GetType())
        XamlResourceAccessor(root)    

    /// Gets a resource by name
    member this.GetResource name = 
        match dict.TryGetValue name with
        | true,element -> element
        | false,element -> 
            let element = root.[name]
            dict.[name] <- element
            element

    /// The root element of the XAML document
    member this.Root = root

module internal FactoryUtilities =
    let loadChildIfNull root resourceFile self =
        match root with
        | null -> LoadXaml.from resourceFile (self.GetType())
        | _ -> root

/// Creates the root element of a XAML type defined within a resource file location
[<AbstractClass>]
type XamlAppFactory(root: Application, resourceFile: string) as self =
    let child = FactoryUtilities.loadChildIfNull root resourceFile self
    let accessor = XamlResourceAccessor(child.Resources)
    member __.Accessor = accessor    
    member __.Root : Application = child

/// Creates the root element of a XAML type defined within a resource file location
[<AbstractClass>]
type XamlResourceFactory(root: ResourceDictionary, resourceFile: string) as self =
    let child = FactoryUtilities.loadChildIfNull root resourceFile self
    let accessor = XamlResourceAccessor(child)
    member __.Accessor = accessor    
    member __.Root = child

/// Creates the root element of a XAML type defined within a resource file location
[<AbstractClass>]
type XamlTypeFactory<'T when 'T :> FrameworkElement and 'T : null>(root: 'T, resourceFile: string) as self =
    let child = FactoryUtilities.loadChildIfNull root resourceFile self
    let accessor = XamlFileAccessor(child)
    member __.Accessor = accessor    
    member __.Root : 'T = child

/// Creates a container for a given XAML element (UserControl or similar) defined within a resource file location
[<AbstractClass>]
type XamlContainer(content : FrameworkElement, resourceFile) as self =
    inherit System.Windows.Controls.ContentControl()

    let child = FactoryUtilities.loadChildIfNull content resourceFile self
    let accessor = XamlFileAccessor(child)
    do                
        self.Content <- child

    member __.Accessor = accessor
    member __.Root = child