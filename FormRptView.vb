Imports Microsoft.Reporting.WinForms
Imports System.Drawing.Imaging
Imports System.Drawing.Printing
Imports System.IO
Imports System.Runtime.CompilerServices
Public Module LocalReportExtensions
    <Extension()>
    Sub Print(ByVal report As LocalReport)
        Dim pageSettings = New PageSettings()
        pageSettings.PaperSize = report.GetDefaultPageSettings().PaperSize
        pageSettings.Landscape = report.GetDefaultPageSettings().IsLandscape
        pageSettings.Margins = report.GetDefaultPageSettings().Margins
        Print(report, pageSettings)             ' El parametro "report" es el reporte del viewer, que se llama desde la línea del load
    End Sub
    <Extension()>
    Sub Print(ByVal report As LocalReport, ByVal pageSettings As PageSettings)          ' Estos parametros supongo que entran en la llamada Print() de arriba
        Dim deviceInfo As String = $"<DeviceInfo>
                <OutputFormat>EMF</OutputFormat>
                <PageWidth>3.14in</PageWidth>           
                <PageHeight>{pageSettings.PaperSize.Height}in</PageHeight>
                <MarginTop>{pageSettings.Margins.Top}in</MarginTop>
                <MarginLeft>{pageSettings.Margins.Left}in</MarginLeft>
                <MarginRight>{pageSettings.Margins.Right}in</MarginRight>
                <MarginBottom>{pageSettings.Margins.Bottom}in</MarginBottom>
            </DeviceInfo>"          ' 3.14in = 80mm, hoja de la impresora        
        Dim warnings() As Warning
        Dim streams = New List(Of Stream)()
        Dim currentPageIndex = 0
        report.Render("Image", deviceInfo,                                      ' De acá
        Function(name, fileNameExtension, encoding, mimeType, willSeek)         '
            Dim stream = New MemoryStream()                                     '
            streams.Add(stream)                                                 '
            Return stream                                                       '
        End Function, warnings)                                                 ' a acá, es una sola línea

        For Each stream As Stream In streams
            stream.Position = 0
        Next

        'If streams Is Nothing OrElse streams.Count = 0 Then            'Funciona igual
        '    Throw New Exception("Error: no stream to print.")          ' Stream = ??
        'End If

        Dim printDocument = New PrintDocument()
        printDocument.DefaultPageSettings = pageSettings
        'printDocument.PrinterSettings.PrinterName = "EPSON XP-440 Series" ' Declarar la impresora por la que va a salir el reporte, con el nombre exacto como está en el menú de selección

        'If Not printDocument.PrinterSettings.IsValid Then
        '   Throw New Exception("Error: cannot find the default printer.")      ' Error si no encuentra la impresora declarada arriba
        'Else
        AddHandler printDocument.PrintPage,
            Sub(sender, e)
                Dim pageImage As Metafile = New Metafile(streams(currentPageIndex))
                Dim adjustedRect As Rectangle = New Rectangle(
                    e.PageBounds.Left - CInt(e.PageSettings.HardMarginX),
                    e.PageBounds.Top - CInt(e.PageSettings.HardMarginY),
                    e.PageBounds.Width,
                    e.PageBounds.Height)
                e.Graphics.FillRectangle(Brushes.White, adjustedRect)
                e.Graphics.DrawImage(pageImage, adjustedRect)
                currentPageIndex += 1
                'e.HasMorePages = False
                e.HasMorePages = (currentPageIndex < streams.Count)            'Por si hay mas de una hoja a imprimir
                'e.Graphics.DrawRectangle(Pens.Red, adjustedRect)               'Dibuja un rectangulo raro en el contorno de la hoja
            End Sub
            AddHandler printDocument.EndPrint,
            Sub(Sender, e)
                If streams IsNot Nothing Then
                    For Each stream As Stream In streams
                        stream.Close()
                    Next
                    streams = Nothing
                End If
            End Sub
            printDocument.Print()
        'End If
    End Sub
End Module
Public Class FormRptView
    Private Sub FormRptView_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.ReportViewer.RefreshReport()
        Me.ReportViewer.LocalReport.Print()
        Me.Close()
    End Sub
End Class