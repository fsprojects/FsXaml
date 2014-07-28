namespace WpfSimpleDrawingApplication

[<StructuredFormatDisplay("{X}:{Y}")>]
type Point  = 
    { X: float; Y: float }
    override x.ToString() = sprintf "(%f : %f)" x.X x.Y

type PointPair = { Start : Point; End : Point }

type CaptureStatus =
    | Captured
    | Released
type MoveEvent =
    | CaptureChanged of status:CaptureStatus
    | PositionChanged of status:CaptureStatus * position:Point
