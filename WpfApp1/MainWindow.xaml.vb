Imports System.Data
Imports System.Data.SQLite
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions

Class MainWindow
    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        Dim querystring As String = $"SELECT bookname,chapternumber,content from search_index where search_index MATCH '""{ RemoveDiacritics(query.Text).ToLower()}""'"
        Using db As New SQLiteConnection("Data Source=btext.db")
            db.Open()
            db.EnableExtensions(True)
            db.LoadExtension("System.Data.SQLite.dll", "sqlite3_fts5_init")
            Dim datatable As New DataTable
            Dim dataadapter As New SQLiteDataAdapter(querystring, db)
            dataadapter.Fill(datatable)
            results.Clear()
            result.Items.Clear()
            For Each row As DataRow In datatable.Rows
                Dim tvi As New TreeViewItem()
                tvi.Header = $"{row("bookname")} {row("chapternumber")}"
                results(tvi.Header.ToString()) = row("content").ToString()
                result.Items.Add(tvi)
                AddHandler tvi.Selected, AddressOf Selected
            Next
            If datatable.Rows.Count = 0 Then
                MessageBox.Show("Δεν βρέθηκαν αποτελέσματα")
            End If
        End Using
    End Sub
    Dim results As New Dictionary(Of String, String)
    Private Sub Selected(sender As Object, e As RoutedEventArgs)
        Dim header As String = DirectCast(sender, TreeViewItem).Header.ToString()
        ncontent.Document.Blocks.Clear()
        ncontent.Document.Blocks.Add(New Paragraph(New Run(results(header))))
        highlightWords(query.Text.Split().Select(Function(x) RemoveDiacritics(x).ToLower()).ToList(), ncontent.Document)
    End Sub
    Shared Function RemoveDiacritics(text As String) As String
        Dim normalizedString = text.Normalize(NormalizationForm.FormD)
        Dim sb = New StringBuilder
        For Each c In normalizedString
            Dim unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)
            If unicodeCategory <> UnicodeCategory.NonSpacingMark Then
                sb.Append(c)
            End If
        Next
        Return sb.ToString().Normalize(NormalizationForm.FormC)
    End Function
    Shared Iterator Function GetAllWordRanges(document As FlowDocument) As IEnumerable(Of TextRange)
        Dim pattern = "[^\W\d](\w|[-']{1,2}(?=\w))*"
        Dim pointer = document.ContentStart
        While (pointer IsNot Nothing)
            If pointer.GetPointerContext(LogicalDirection.Forward) = TextPointerContext.Text Then
                Dim textRun = pointer.GetTextInRun(LogicalDirection.Forward)
                Dim matches = Regex.Matches(textRun, pattern)
                For Each match As Match In matches
                    Dim startindex = match.Index
                    Dim length = match.Length
                    Dim start = pointer.GetPositionAtOffset(startindex)
                    Dim [end] = start.GetPositionAtOffset(length)
                    Yield New TextRange(start, [end])
                Next
            End If
            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward)
        End While
    End Function
    Shared Sub highlightWords(words As List(Of String), doc As FlowDocument)
        Dim wordRanges = GetAllWordRanges(doc)
        For Each wordRange As TextRange In wordRanges
            If words.IndexOf(RemoveDiacritics(wordRange.Text).ToLower()) <> -1 Then
                wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue)
            End If
        Next
    End Sub

End Class
