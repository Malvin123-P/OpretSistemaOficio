using OfiGest.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.RegularExpressions;

namespace OfiGest.Manegers
{
    public class PdfCertificacionManager
    {
        public byte[] GenerarPdf(CertificacionPdfModel modelo)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(2, Unit.Centimetre);
                    page.MarginRight(2, Unit.Centimetre);
                    page.MarginTop(1.0f, Unit.Centimetre);
                    page.MarginBottom(1.0f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Spacing(5);

                        column.Item().AlignCenter().Height(60)
                            .Image("wwwroot/images/escudo_rd.png", ImageScaling.FitHeight);

                        column.Item().AlignCenter().Text("PRESIDENCIA DE LA REPÚBLICA")
                            .FontFamily("Times New Roman").FontSize(9).Bold();

                        column.Item().AlignCenter().Text("Oficina para el Reordenamiento del Transporte")
                            .FontFamily("Times New Roman").FontSize(16).Bold();
                    });

                    // Contenido principal
                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        // Encabezado de la certificación
                        column.Item().Padding(15).Column(innerColumn =>
                        {
                            innerColumn.Spacing(4);

                            innerColumn.Item().AlignCenter().Text("CERTIFICACIÓN")
                                .FontFamily("Times New Roman").FontSize(16).Bold();

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("No: ").Bold();
                                t.Span(modelo.Codigo ?? "");
                            });

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("Fecha: ").Bold();
                                t.Span($"{modelo.FechaCreacion:dd/MM/yyyy}");
                            });

                            innerColumn.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        });

                      
                        if (!string.IsNullOrWhiteSpace(modelo.Contenido))
                        {
                            column.Item().Padding(5).Background(Colors.White)
                                .Element(container => RenderHtmlContent(container, modelo.Contenido));
                        }

                  
                        if (!string.IsNullOrWhiteSpace(modelo.Anexos))
                        {
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                            column.Item().Text("ANEXOS:").Bold().FontSize(11);
                            column.Item().Text(modelo.Anexos);
                        }

                      
                        column.Item().Extend().AlignBottom().Column(firmaColumn =>
                        {
                            firmaColumn.Spacing(10);

                            firmaColumn.Item().AlignCenter().Column(firmaInner =>
                            {
                                firmaInner.Spacing(4);

                                firmaInner.Item().AlignCenter().Width(200).BorderBottom(0.8f)
                                .BorderColor(Colors.Black)
                                .Text(" ");

                                firmaInner.Item().AlignCenter().Text(modelo.EncargadoDepartamental ?? "")
                                    .SemiBold().FontSize(11);

                                firmaInner.Item().AlignCenter().Text(modelo.DepartamentoRemitente ?? "")
                                    .FontSize(10);

                             
                            });
                        });
                    });

                  
                    page.Footer()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Spacing(4);

                            column.Item().AlignCenter().Text("Av. Máximo Gómez esq. Av. Paseo de los Reyes Católicos, Cristo Rey, Santo Domingo, D. N., Rep. Dom.")
                                .FontSize(8).FontColor(Colors.Black);

                            column.Item().AlignCenter().Text("Tels.: 809-732-2670/ 809-333-2670")
                                .FontSize(8).FontColor(Colors.Black);

                            column.Item().AlignCenter().Text("RNC: 4-30-02742-1")
                                .FontSize(8).FontColor(Colors.Black);
                        });
                });
            });

            return document.GeneratePdf();
        }

        private void RenderHtmlContent(IContainer container, string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return;

            var cleanedHtml = CleanHtml(htmlContent);

            container.Column(column =>
            {
                var documentElements = ParseHtmlDocument(cleanedHtml);

                foreach (var element in documentElements)
                {
                    if (!string.IsNullOrWhiteSpace(element.Text))
                    {
                        var paddingBottom = element.Type switch
                        {
                            HtmlElementType.ListItem => 5,
                            HtmlElementType.Heading => 8,
                            _ => 10
                        };

                        column.Item().PaddingBottom(paddingBottom)
                            .Text(text =>
                            {
                                RenderFormattedText(text, element);
                            });
                    }
                }
            });
        }

        private List<HtmlElement> ParseHtmlDocument(string html)
        {
            var elements = new List<HtmlElement>();

            if (string.IsNullOrWhiteSpace(html))
                return elements;

            elements.AddRange(ExtractHeadings(html));
            elements.AddRange(ExtractParagraphs(html));
            elements.AddRange(ExtractListItems(html));

            if (elements.Count == 0)
            {
                var cleanText = ExtractTextFromHtml(html);
                if (!string.IsNullOrWhiteSpace(cleanText))
                {
                    elements.Add(new HtmlElement
                    {
                        Text = cleanText,
                        Type = HtmlElementType.Paragraph
                    });
                }
            }

            return elements;
        }

        private List<HtmlElement> ExtractHeadings(string html)
        {
            var headings = new List<HtmlElement>();
            var matches = Regex.Matches(html, @"<(h[1-6])[^>]*>(.*?)</\1>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups[2].Success)
                {
                    var textContent = ExtractTextFromHtml(match.Groups[2].Value);
                    if (!string.IsNullOrWhiteSpace(textContent))
                    {
                        headings.Add(new HtmlElement
                        {
                            Text = textContent,
                            Type = HtmlElementType.Heading,
                            HeadingLevel = GetHeadingLevel(match.Groups[1].Value)
                        });
                    }
                }
            }

            return headings;
        }

        private List<HtmlElement> ExtractParagraphs(string html)
        {
            var paragraphs = new List<HtmlElement>();
            var matches = Regex.Matches(html, @"<p[^>]*>(.*?)</p>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    var textContent = ExtractTextFromHtml(match.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(textContent))
                    {
                        paragraphs.Add(new HtmlElement
                        {
                            Text = textContent,
                            Type = HtmlElementType.Paragraph
                        });
                    }
                }
            }

            return paragraphs;
        }

        private List<HtmlElement> ExtractListItems(string html)
        {
            var listItems = new List<HtmlElement>();

         
            var ulMatches = Regex.Matches(html, @"<ul[^>]*>(.*?)</ul>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match ulMatch in ulMatches)
            {
                if (ulMatch.Groups[1].Success)
                {
                    var liMatches = Regex.Matches(ulMatch.Groups[1].Value, @"<li[^>]*>(.*?)</li>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    foreach (Match liMatch in liMatches)
                    {
                        if (liMatch.Groups[1].Success)
                        {
                            var textContent = ExtractTextFromHtml(liMatch.Groups[1].Value);
                            if (!string.IsNullOrWhiteSpace(textContent))
                            {
                                listItems.Add(new HtmlElement
                                {
                                    Text = textContent,
                                    Type = HtmlElementType.ListItem,
                                    IsOrderedList = false
                                });
                            }
                        }
                    }
                }
            }

         
            var olMatches = Regex.Matches(html, @"<ol[^>]*>(.*?)</ol>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            int itemNumber = 1;
            foreach (Match olMatch in olMatches)
            {
                if (olMatch.Groups[1].Success)
                {
                    var liMatches = Regex.Matches(olMatch.Groups[1].Value, @"<li[^>]*>(.*?)</li>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    foreach (Match liMatch in liMatches)
                    {
                        if (liMatch.Groups[1].Success)
                        {
                            var textContent = ExtractTextFromHtml(liMatch.Groups[1].Value);
                            if (!string.IsNullOrWhiteSpace(textContent))
                            {
                                listItems.Add(new HtmlElement
                                {
                                    Text = textContent,
                                    Type = HtmlElementType.ListItem,
                                    IsOrderedList = true,
                                    ItemNumber = itemNumber
                                });
                                itemNumber++;
                            }
                        }
                    }
                }
                itemNumber = 1;
            }

            return listItems;
        }

        private int GetHeadingLevel(string tagName)
        {
            if (tagName.Length == 2 && char.IsDigit(tagName[1]))
            {
                return int.Parse(tagName[1].ToString());
            }
            return 2;
        }

        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            html = System.Net.WebUtility.HtmlDecode(html);

            html = html.Replace("<br>", "\n")
                       .Replace("<br/>", "\n")
                       .Replace("<br />", "\n");

            html = Regex.Replace(html, @"</?(div|span|font|html|body|head|meta|title|link|script|style)[^>]*>", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            html = Regex.Replace(html, @"<(\w+)[^>]*>", "<$1>", RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"\s+", " ");
            html = html.Replace("\n ", "\n").Replace(" \n", "\n");

            return html.Trim();
        }

        private string CleanHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            html = html.Replace("<strong>", "<b>").Replace("</strong>", "</b>")
                       .Replace("<em>", "<i>").Replace("</em>", "</i>");

            html = Regex.Replace(html, @"</?(div|span|font|html|body|head|meta|title|link|script|style)[^>]*>", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            html = Regex.Replace(html, @"\s+(style|class|id|data-[^=]+)=""[^""]*""", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return html.Trim();
        }

        private void RenderFormattedText(TextDescriptor text, HtmlElement element)
        {
            switch (element.Type)
            {
                case HtmlElementType.Heading:
                    var fontSize = element.HeadingLevel switch
                    {
                        1 => 14,
                        2 => 13,
                        3 => 12,
                        _ => 11
                    };

                    text.Span(element.Text)
                        .FontSize(fontSize)
                        .Bold()
                        .Underline();
                    break;

                case HtmlElementType.ListItem:
                    if (element.IsOrderedList && element.ItemNumber.HasValue)
                    {
                        text.Span($"{element.ItemNumber}. ").FontSize(11);
                    }
                    else
                    {
                        text.Span("• ").FontSize(11);
                    }

                    var listSegments = ParseTextSegments(element.Text);
                    foreach (var segment in listSegments)
                    {
                        var textSpan = text.Span(segment.Text).FontSize(11);

                        if (segment.IsBold) textSpan = textSpan.Bold();
                        if (segment.IsItalic) textSpan = textSpan.Italic();
                        if (segment.IsUnderline) textSpan = textSpan.Underline();
                    }
                    break;

                case HtmlElementType.Paragraph:
                default:
                    var segments = ParseTextSegments(element.Text);
                    foreach (var segment in segments)
                    {
                        var textSpan = text.Span(segment.Text).FontSize(11);

                        if (segment.IsBold) textSpan = textSpan.Bold();
                        if (segment.IsItalic) textSpan = textSpan.Italic();
                        if (segment.IsUnderline) textSpan = textSpan.Underline();
                    }
                    break;
            }
        }

        private List<TextSegment> ParseTextSegments(string html)
        {
            var segments = new List<TextSegment>();
            var currentText = new StringBuilder();
            var formatStack = new Stack<string>();

            for (int i = 0; i < html.Length; i++)
            {
                if (html[i] == '<')
                {
                    var tagEnd = html.IndexOf('>', i);
                    if (tagEnd > i)
                    {
                        if (currentText.Length > 0)
                        {
                            segments.Add(CreateTextSegment(currentText.ToString(), formatStack));
                            currentText.Clear();
                        }

                        var tagContent = html.Substring(i + 1, tagEnd - i - 1).ToLower();
                        var isClosingTag = tagContent.StartsWith("/");
                        var tagName = isClosingTag ? tagContent.Substring(1) : tagContent;
                        tagName = tagName.Split(' ')[0].Trim();

                        if (isClosingTag)
                        {
                            if (formatStack.Count > 0 && formatStack.Peek() == tagName)
                            {
                                formatStack.Pop();
                            }
                        }
                        else
                        {
                            if (IsSupportedTag(tagName))
                            {
                                formatStack.Push(tagName);
                            }
                        }

                        i = tagEnd;
                    }
                    else
                    {
                        currentText.Append(html[i]);
                    }
                }
                else
                {
                    currentText.Append(html[i]);
                }
            }

            if (currentText.Length > 0)
            {
                segments.Add(CreateTextSegment(currentText.ToString(), formatStack));
            }

            return segments;
        }

        private TextSegment CreateTextSegment(string text, Stack<string> formatStack)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TextSegment { Text = "" };
            }

            var formats = formatStack.ToArray();
            return new TextSegment
            {
                Text = text,
                IsBold = formats.Contains("b") || formats.Contains("strong"),
                IsItalic = formats.Contains("i") || formats.Contains("em"),
                IsUnderline = formats.Contains("u")
            };
        }

        private bool IsSupportedTag(string tag)
        {
            var supportedTags = new[] { "b", "strong", "i", "em", "u" };
            return supportedTags.Contains(tag);
        }

        private enum HtmlElementType
        {
            Paragraph,
            ListItem,
            Heading
        }

        private class HtmlElement
        {
            public string Text { get; set; }
            public HtmlElementType Type { get; set; }
            public bool IsOrderedList { get; set; }
            public int? ItemNumber { get; set; }
            public int HeadingLevel { get; set; } = 2;
        }

        private class TextSegment
        {
            public string Text { get; set; }
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsUnderline { get; set; }
        }

    
        public string ObtenerNombreArchivo(CertificacionPdfModel modelo)
        {
            var nombreLimpio = RemoveDiacritics(modelo.Codigo)
                .Replace(" ", "_")
                .Replace("/", "-");
            return $"Certificacion_{nombreLimpio}.pdf";
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }

    public class CertificacionPdfModel
    {
        public string Codigo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Contenido { get; set; }
        public string Anexos { get; set; }
        public string EncargadoDepartamental { get; set; }
        public string DepartamentoRemitente { get; set; }
        public string CargoFirmante { get; set; }
    }
}