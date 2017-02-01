#r "System.Xaml"
#r "System.Xml"
#r "PresentationCore"
#r "PresentationFramework"
#r "WindowsBase"

open System
open System.Collections.Generic
open System.IO
open System.Threading
open System.Windows
open System.Windows.Markup
open System.Windows.Threading
open System.Xaml

type RootNodeType =
    | FrameworkElement
    | ResourceDictionary
    | Application

type XamlInfo = { RootType : XamlType ; RootNodeType : RootNodeType ; Members : (string*XamlType) list ; Events : (string*XamlType) list }

let parseXaml (strm : Stream) =
    use reader = new XamlXmlReader(strm, XamlReader.GetWpfSchemaContext())

    let mutable root : (XamlType * RootNodeType) option = None
    let mutable namedMembers : (string*XamlType) list = []
    let mutable eventHandlers : (string*XamlType) list = []

    let rec moveToObject (reader : XamlXmlReader) =
        match reader.NodeType with
        | XamlNodeType.StartObject -> Some reader
        | _ -> 
            if not(reader.Read()) then
                None
            else
                moveToObject reader

    let rec moveToMember (reader : XamlXmlReader) =
        match reader.NodeType with
        | XamlNodeType.StartMember -> Some reader
        | XamlNodeType.StartObject -> None
        | XamlNodeType.EndObject -> None
        | _ -> 
            if not(reader.Read()) then
                None
            else
                moveToMember reader


    let mutable currentObject : XamlType = null

    let rec processMember reader rootType =
        let t = moveToMember reader

        match t with
        | None -> 
            false
        | Some t ->
            match rootType, t.Member.IsDirective, t.Member.Name, t.Member.IsEvent with
            | RootNodeType.FrameworkElement, true, "Name", false 
            | RootNodeType.Application, true, "Key", false 
            | RootNodeType.ResourceDictionary, true, "Key", false -> 
                if reader.Read() then
                    let v = string reader.Value
                    if not(String.IsNullOrWhiteSpace(v)) then
                        namedMembers <- (v, currentObject) :: namedMembers                    
            | RootNodeType.FrameworkElement, false, _, true ->
                let xt = t.Member.Type
                if reader.Read() then
                    let v = string reader.Value
                    if not(String.IsNullOrWhiteSpace(v)) then
                        eventHandlers <- (v, xt) :: eventHandlers            
            | _ -> ()

            reader.Read() |> ignore
            true

    while (Option.isSome <| moveToObject reader) do    
        currentObject <- reader.Type    

        // Check and set our root element
        match root with 
        | None -> 
            try             
                let nodeType = 
                    match currentObject.UnderlyingType with
                    | t when typeof<Application>.IsAssignableFrom(t) -> RootNodeType.Application
                    | t when typeof<ResourceDictionary>.IsAssignableFrom(t) -> RootNodeType.ResourceDictionary
                    | t when typeof<FrameworkElement>.IsAssignableFrom(t) -> RootNodeType.FrameworkElement
                    | _ -> failwith "Unknown"
                root <- Some (currentObject, nodeType)
            with 
            | _ -> ()
        | _ -> ()

        reader.Read() |> ignore
        match root with
        | Some(_,t) ->
            while processMember reader t do        
                ()
        | None -> ()
    
    let root = Option.get root
    { RootType = fst root ; RootNodeType = snd root ; Members = namedMembers ; Events = eventHandlers }

let mainViewXaml = """<UserControl     
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:fsxaml="http://github.com/fsprojects/FsXaml"
        xmlns:local="clr-namespace:ViewModels;assembly=WpfSimpleMvvmApplication"         
        xmlns:views="clr-namespace:Views;assembly=WpfSimpleMvvmApplication"         
        MinHeight="220" MinWidth="300" Height="Auto"
        x:Name="Self"
        >
    <!-- fsxaml:ViewController.Custom="{x:Type views:MainViewController}" -->
    <UserControl.Resources>
        <fsxaml:BooleanToCollapsedConverter x:Key="TrueToCollapsed" />
    </UserControl.Resources>
    <UserControl.DataContext>
        <local:MainViewModel />
    </UserControl.DataContext>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <TextBlock Margin="3" Grid.Row="0" Grid.Column="0" Text="First:" Mouse.MouseDown="MouseDownFirst" />
        <TextBox x:Name="FirstName"  Margin="3" Grid.Column="1" Grid.Row="0" FontSize="16" Text="{Binding FirstName}"/>

        <TextBlock Margin="3" Grid.Row="1" Grid.Column="0" Text="Last:" />
        <TextBox x:Name="LastName"  Margin="3" Grid.Column="1" Grid.Row="1" FontSize="16" Text="{Binding LastName}"/>

        <TextBlock Margin="3" Grid.Row="2" Grid.Column="0" Text="Full Name:" />
        <TextBox x:Name="tbFullName" IsReadOnly="true" IsTabStop="False" Foreground="Gray"  Margin="3" Grid.Column="1" Grid.Row="2" FontSize="16" Text="{Binding FullName, Mode=OneWay}"/>
        <TextBlock Grid.Row="3" Margin="3" Text="Errors:" />
        <Border            
            Margin="3" VerticalAlignment="Stretch"  Grid.Row="4" Grid.ColumnSpan="2" BorderThickness="1" BorderBrush="DarkGray">
            <!-- Forcing element name binding for test against Issue #38 -->
            <views:ErrorView DataContext="{Binding Path=DataContext, ElementName=Self}" />
        </Border>
        <Button Margin="3" Click="TestClick" Grid.Row="5" Grid.ColumnSpan="2" FontSize="16" Command="{Binding OkCommand}" CommandParameter="{Binding FullName}">Ok</Button>        
    </Grid>
</UserControl>"""

let resourceXaml = """<ResourceDictionary
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fsxaml="http://github.com/fsprojects/FsXaml"
    >
    <fsxaml:ValidationErrorsToStringConverter x:Key="validationConverter"/>
    <SolidColorBrush Color="red" x:Key="testBrush" />
    <Style TargetType="{x:Type TextBox}" x:Key="customTextBoxStyle">
        <Style.Resources>
            <Style x:Key="{x:Type ToolTip}" TargetType="{x:Type ToolTip}">
                <Setter Property="Foreground" Value="White"/>
                <Setter Property="Background" Value="Red"/>                
            </Style>
        </Style.Resources>
        <Style.Triggers>
            <Trigger Property="Validation.HasError" Value="true">
                <Setter Property="ToolTipService.ToolTip">
                    <Setter.Value>
                        <MultiBinding Converter="{StaticResource validationConverter}">
                            <Binding RelativeSource="{x:Static RelativeSource.Self}" Path="(Validation.Errors)" />
                            <Binding RelativeSource="{x:Static RelativeSource.Self}" Path="(Validation.Errors).Count" />
                        </MultiBinding>                        
                    </Setter.Value>
                </Setter>
            </Trigger>
        </Style.Triggers>        
        <Setter Property="Validation.ErrorTemplate">
            <Setter.Value>
                <ControlTemplate>
                    <Grid>
                        <AdornedElementPlaceholder Name="customAdorner" VerticalAlignment="Center" >
                            <Border BorderBrush="red" BorderThickness="1" />
                        </AdornedElementPlaceholder>
                        <Border IsHitTestVisible="false" Background="Red" HorizontalAlignment="Right"  Margin="5,0" Width="14" Height="14" CornerRadius="7">
                            <TextBlock Text="!" VerticalAlignment="center" HorizontalAlignment="Center" FontWeight="Bold" Foreground="white"/>
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
    <Style TargetType="{x:Type TextBox}" BasedOn="{StaticResource customTextBoxStyle}" />
</ResourceDictionary>
"""

let generateStreamFromString (s : string) =
    let stream = new MemoryStream()
    let writer = new StreamWriter(stream)
    writer.Write s
    writer.Flush ()
    stream.Position <- 0L
    stream

use sr = generateStreamFromString mainViewXaml

let results = parseXaml(sr)

