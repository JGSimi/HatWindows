using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Markdig;
using MarkdigBlock = Markdig.Syntax.Block;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Hat.Views.Controls;

/// <summary>
/// Renders Markdown into a WPF FlowDocument with Hat's custom theme.
/// Port of HatMarkdownView.swift + MarkdownUI.Theme.hat.
/// </summary>
public partial class MarkdownViewer : UserControl
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(MarkdownViewer),
            new PropertyMetadata("", OnMarkdownChanged));

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public MarkdownViewer()
    {
        InitializeComponent();
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownViewer viewer)
            viewer.RenderMarkdown((string)e.NewValue);
    }

    private void RenderMarkdown(string markdown)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI"),
            FontSize = 12.5,
            Foreground = FindBrush("TextPrimaryBrush"),
            PagePadding = new Thickness(0),
            LineHeight = 20
        };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            RichTextContent.Document = doc;
            return;
        }

        try
        {
            var parsed = Markdig.Markdown.Parse(markdown, Pipeline);
            foreach (var block in parsed)
                ProcessBlock(block, doc.Blocks);
        }
        catch
        {
            // Fallback to plain text
            doc.Blocks.Add(new Paragraph(new Run(markdown)));
        }

        RichTextContent.Document = doc;
    }

    private void ProcessBlock(MarkdigBlock block, BlockCollection blocks)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var hPara = new Paragraph { Margin = new Thickness(0, heading.Level == 1 ? 16 : heading.Level == 2 ? 14 : 12, 0, heading.Level == 1 ? 8 : heading.Level == 2 ? 6 : 4) };
                var hSize = heading.Level switch { 1 => 20.0, 2 => 17.0, 3 => 15.0, _ => 14.0 };
                var hWeight = heading.Level <= 1 ? FontWeights.Bold : FontWeights.SemiBold;
                ProcessInlines(heading.Inline, hPara.Inlines, hSize, hWeight);
                blocks.Add(hPara);
                break;

            case ParagraphBlock para:
                var p = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                if (para.Inline != null)
                    ProcessInlines(para.Inline, p.Inlines);
                blocks.Add(p);
                break;

            case FencedCodeBlock code:
            case CodeBlock code2:
                var codeText = (block as LeafBlock)?.Lines.ToString() ?? "";
                var codePara = new Paragraph(new Run(codeText)
                {
                    FontFamily = new FontFamily("Cascadia Code, Consolas"),
                    FontSize = 12,
                    Foreground = FindBrush("TextPrimaryBrush")
                })
                {
                    Background = FindBrush("SurfaceTertiaryBrush"),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 8, 0, 8),
                    BorderBrush = FindBrush("BorderBrush"),
                    BorderThickness = new Thickness(0.5)
                };
                blocks.Add(codePara);
                break;

            case QuoteBlock quote:
                var quoteSection = new Section
                {
                    BorderBrush = new SolidColorBrush(
                        WithAlpha(((SolidColorBrush)FindBrush("AccentPrimaryBrush")).Color, 0.4)),
                    BorderThickness = new Thickness(2.5, 0, 0, 0),
                    Padding = new Thickness(12, 0, 0, 0),
                    Margin = new Thickness(0, 8, 0, 8)
                };
                foreach (var child in quote)
                    ProcessBlock(child, quoteSection.Blocks);
                blocks.Add(quoteSection);
                break;

            case ListBlock list:
                var wpfList = new System.Windows.Documents.List
                {
                    MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                    Margin = new Thickness(16, 4, 0, 4),
                    Padding = new Thickness(0)
                };
                foreach (var item in list)
                {
                    if (item is ListItemBlock listItem)
                    {
                        var li = new ListItem { Margin = new Thickness(0, 2, 0, 2) };
                        foreach (var child in listItem)
                            ProcessBlock(child, li.Blocks);
                        wpfList.ListItems.Add(li);
                    }
                }
                blocks.Add(wpfList);
                break;

            case ThematicBreakBlock:
                var hr = new Paragraph(new Run(""))
                {
                    BorderBrush = FindBrush("BorderBrush"),
                    BorderThickness = new Thickness(0, 0, 0, 0.5),
                    Margin = new Thickness(0, 12, 0, 12)
                };
                blocks.Add(hr);
                break;

            case Markdig.Extensions.Tables.Table table:
                RenderTable(table, blocks);
                break;

            default:
                // Container blocks
                if (block is ContainerBlock container)
                {
                    foreach (var child in container)
                        ProcessBlock(child, blocks);
                }
                break;
        }
    }

    private void ProcessInlines(ContainerInline? inlines, InlineCollection target,
        double? fontSize = null, FontWeight? fontWeight = null)
    {
        if (inlines == null) return;

        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    var run = new Run(literal.Content.ToString());
                    if (fontSize.HasValue) run.FontSize = fontSize.Value;
                    if (fontWeight.HasValue) run.FontWeight = fontWeight.Value;
                    target.Add(run);
                    break;

                case EmphasisInline emphasis:
                    var emphSpan = new Span();
                    if (emphasis.DelimiterCount == 2 || emphasis.DelimiterChar == '*' && emphasis.DelimiterCount >= 2)
                        emphSpan.FontWeight = FontWeights.SemiBold;
                    else
                        emphSpan.FontStyle = FontStyles.Italic;
                    ProcessInlines(emphasis, emphSpan.Inlines, fontSize, fontWeight);
                    target.Add(emphSpan);
                    break;

                case CodeInline codeInline:
                    target.Add(new Run(codeInline.Content)
                    {
                        FontFamily = new FontFamily("Cascadia Code, Consolas"),
                        FontSize = 12,
                        Background = FindBrush("SurfaceTertiaryBrush"),
                        Foreground = FindBrush("TextPrimaryBrush")
                    });
                    break;

                case LinkInline link:
                    var hyperlink = new Hyperlink
                    {
                        NavigateUri = Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) ? uri : null,
                        Foreground = FindBrush("AccentPrimaryBrush"),
                        TextDecorations = null
                    };
                    hyperlink.RequestNavigate += (_, args) =>
                    {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true }); }
                        catch { }
                        args.Handled = true;
                    };
                    ProcessInlines(link, hyperlink.Inlines, fontSize, fontWeight);
                    target.Add(hyperlink);
                    break;

                case LineBreakInline:
                    target.Add(new LineBreak());
                    break;

                default:
                    // Try to get text content
                    if (inline is LeafInline leaf)
                    {
                        var r = new Run(leaf.ToString());
                        if (fontSize.HasValue) r.FontSize = fontSize.Value;
                        target.Add(r);
                    }
                    break;
            }
        }
    }

    private void RenderTable(Markdig.Extensions.Tables.Table table, BlockCollection blocks)
    {
        var wpfTable = new System.Windows.Documents.Table
        {
            CellSpacing = 0,
            BorderBrush = FindBrush("BorderBrush"),
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 8, 0, 8)
        };

        // Determine column count
        var colCount = 0;
        if (table.FirstOrDefault() is Markdig.Extensions.Tables.TableRow firstRow)
            colCount = firstRow.Count;

        for (int i = 0; i < colCount; i++)
            wpfTable.Columns.Add(new TableColumn());

        var rowGroup = new TableRowGroup();
        var isHeader = true;

        foreach (var row in table)
        {
            if (row is not Markdig.Extensions.Tables.TableRow tableRow) continue;

            var wpfRow = new TableRow();
            if (isHeader)
            {
                wpfRow.FontWeight = FontWeights.SemiBold;
                wpfRow.Background = FindBrush("SurfaceTertiaryBrush");
                isHeader = false;
            }

            foreach (var cell in tableRow)
            {
                if (cell is not Markdig.Extensions.Tables.TableCell tableCell) continue;

                var wpfCell = new TableCell
                {
                    Padding = new Thickness(8, 4, 8, 4),
                    BorderBrush = FindBrush("BorderBrush"),
                    BorderThickness = new Thickness(0, 0, 0.5, 0.5)
                };

                foreach (var child in tableCell)
                    ProcessBlock(child, wpfCell.Blocks);

                wpfRow.Cells.Add(wpfCell);
            }
            rowGroup.Rows.Add(wpfRow);
        }

        wpfTable.RowGroups.Add(rowGroup);
        blocks.Add(wpfTable);
    }

    private Brush FindBrush(string key)
    {
        try { return (Brush)FindResource(key); }
        catch { return Brushes.White; }
    }

    private static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((byte)(alpha * 255), color.R, color.G, color.B);
}
