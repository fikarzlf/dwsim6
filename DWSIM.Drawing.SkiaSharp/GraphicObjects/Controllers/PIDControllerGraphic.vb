﻿Namespace GraphicObjects.Shapes

    Public Class PIDControllerGraphic

        Inherits ShapeGraphic

        Private Image As SKImage

        Public Property ConnectedToMv() As GraphicObject

        Public Property ConnectedToRv() As GraphicObject

        Public Property ConnectedToCv() As GraphicObject

#Region "Constructors"

        Public Sub New()
            Me.ObjectType = DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.Controller_PID
            Me.Description = "PID Controller"
        End Sub

        Public Sub New(ByVal graphicPosition As SKPoint)
            Me.New()
            Me.SetPosition(graphicPosition)
        End Sub

        Public Sub New(ByVal posX As Integer, ByVal posY As Integer)
            Me.New(New SKPoint(posX, posY))
        End Sub

        Public Sub New(ByVal graphicPosition As SKPoint, ByVal graphicSize As SKSize)
            Me.New(graphicPosition)
            Me.SetSize(graphicSize)
        End Sub

        Public Sub New(ByVal posX As Integer, ByVal posY As Integer, ByVal graphicSize As SKSize)
            Me.New(New SKPoint(posX, posY), graphicSize)
        End Sub

        Public Sub New(ByVal posX As Integer, ByVal posY As Integer, ByVal width As Integer, ByVal height As Integer)
            Me.New(New SKPoint(posX, posY), New SKSize(width, height))
        End Sub

#End Region

        Public Overrides Sub CreateConnectors(InCount As Integer, OutCount As Integer)

            Me.EnergyConnector.Active = False

        End Sub

        Public Overrides Sub PositionConnectors()

            CreateConnectors(0, 0)

        End Sub

        Public Overrides Sub Draw(ByVal g As Object)

            Dim canvas As SKCanvas = DirectCast(g, SKCanvas)

            CreateConnectors(0, 0)

            UpdateStatus()

            MyBase.Draw(g)

            Dim aPen As New SKPaint()
            With aPen
                .Color = SKColors.Blue
                .StrokeWidth = LineWidth
                .IsStroke = True
                .IsAntialias = GlobalSettings.Settings.DrawingAntiAlias
                .PathEffect = SKPathEffect.CreateDash(New Single() {10.0F, 5.0F, 2.0F, 5.0F}, 2.0F)
            End With

            If Not Me.ConnectedToMv Is Nothing Then
                canvas.DrawPoints(SKPointMode.Polygon, New SKPoint() {New SKPoint(Me.X + Me.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToMv.X + Me.ConnectedToMv.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToMv.X + Me.ConnectedToMv.Width / 2, Me.ConnectedToMv.Y + Me.ConnectedToMv.Height / 2)}, aPen)
            End If
            If Not Me.ConnectedToCv Is Nothing Then
                canvas.DrawPoints(SKPointMode.Polygon, New SKPoint() {New SKPoint(Me.X + Me.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToCv.X + Me.ConnectedToCv.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToCv.X + Me.ConnectedToCv.Width / 2, Me.ConnectedToCv.Y + Me.ConnectedToCv.Height / 2)}, aPen)
            End If
            If Not Me.ConnectedToRv Is Nothing Then
                canvas.DrawPoints(SKPointMode.Polygon, New SKPoint() {New SKPoint(Me.X + Me.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToRv.X + Me.ConnectedToRv.Width / 2, Me.Y + Me.Height / 2), New SKPoint(Me.ConnectedToRv.X + Me.ConnectedToRv.Width / 2, Me.ConnectedToRv.Y + Me.ConnectedToRv.Height / 2)}, aPen)
            End If

            If Image Is Nothing Then

                Dim assm = Me.GetType.Assembly
                Using filestr As IO.Stream = assm.GetManifestResourceStream("DWSIM.Drawing.SkiaSharp.control_panel.png")
                    Using bitmap = SKBitmap.Decode(filestr)
                        Image = SKImage.FromBitmap(bitmap)
                    End Using
                End Using

            End If

            Using p As New SKPaint With {.IsAntialias = GlobalSettings.Settings.DrawingAntiAlias, .FilterQuality = SKFilterQuality.High}
                canvas.DrawImage(Image, New SKRect(X, Y, X + Width, Y + Height), p)
            End Using

        End Sub

    End Class

End Namespace
