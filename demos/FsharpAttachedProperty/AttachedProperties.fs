namespace FsharpAttachedProperty
open System.Windows
open System.Windows.Controls
open System.Windows.Media

module Icon = 
    // http://stackoverflow.com/a/14706890/1069200
    type internal Marker = interface end

    let GeometryProperty = DependencyProperty.RegisterAttached("Geometry", typeof<Geometry>, typeof<Marker>.DeclaringType, PropertyMetadata(null :> Geometry))
    
    let SetGeometry (element: FrameworkElement, value : Geometry) = 
        element.SetValue(GeometryProperty, value)
    
    let GetGeometry (element: FrameworkElement) : Geometry = 
        element.GetValue(GeometryProperty) :?> _

