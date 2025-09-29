using OfiGest.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.RegularExpressions;

namespace OfiGest.Manegers
{
    public class PdfOficioManager
    {
        public byte[] GenerarPdf(OficioPdfModel modelo)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configuración base
                    page.Size(PageSizes.Letter);
                    page.MarginLeft(2, Unit.Centimetre);
                    page.MarginRight(2, Unit.Centimetre);
                    page.MarginTop(1.0f, Unit.Centimetre);
                    page.MarginBottom(1.0f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Encabezado institucional
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

                    // Contenido principal y firma
                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        // Encabezado del oficio
                        column.Item().Padding(5).Column(innerColumn =>
                        {
                            innerColumn.Spacing(4);

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("OFICIO No: ").Bold();
                                t.Span(modelo.Codigo ?? "");
                            });

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("Fecha: ").Bold();
                                t.Span($"{modelo.FechaCreacion:dd/MM/yyyy}");
                            });

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("De: ").Bold();
                                t.Span(modelo.DepartamentoRemitente ?? "");
                            });

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("Para: ").Bold();
                                t.Span(modelo.DirigidoDepartamento ?? "");
                            });

                            if (!string.IsNullOrWhiteSpace(modelo.Via))
                            {
                                innerColumn.Item().Text(t =>
                                {
                                    t.Span("Vía: ").Bold();
                                    t.Span(modelo.Via);
                                });
                            }

                            innerColumn.Item().Text(t =>
                            {
                                t.Span("Asunto: ").Bold();
                                t.Span(modelo.TipoOficio ?? "");
                            });

                            innerColumn.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        });

                        // Cuerpo del oficio - MEJORADO
                        if (!string.IsNullOrWhiteSpace(modelo.Contenido))
                        {
                            column.Item().Text("Por medio del presente oficio, solicitamos amablemente:")
                                .SemiBold().FontSize(12);

                            // Renderizar contenido HTML convertido a formato QuestPDF
                            column.Item().Padding(10).Background(Colors.White)
                                .Element(container => RenderHtmlContent(container, modelo.Contenido));

                            if (!string.IsNullOrWhiteSpace(modelo.Anexos))
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        }

                      
                        // Anexos
                        if (!string.IsNullOrWhiteSpace(modelo.Anexos))
                        {
                            column.Item().Text("ANEXOS:").Bold().FontSize(11);
                            column.Item().Text(modelo.Anexos);
                        }

                        // Firma institucional extendida al fondo
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

                    // Footer institucional
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

        /// <summary>
        /// Renderiza contenido HTML manteniendo el formato básico
        /// </summary>
        private void RenderHtmlContent(IContainer container, string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return;

            // Limpiar y normalizar el HTML
            var cleanedHtml = CleanHtml(htmlContent);

            container.Column(column =>
            {
                // Procesar el contenido HTML completo
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

        /// <summary>
        /// Parsea el documento HTML en elementos estructurados - MÉTODO SIMPLIFICADO Y CORREGIDO
        /// </summary>
        private List<HtmlElement> ParseHtmlDocument(string html)
        {
            var elements = new List<HtmlElement>();

            if (string.IsNullOrWhiteSpace(html))
                return elements;

            // ENFOQUE SIMPLIFICADO: Extraer elementos en orden usando un método más robusto
            elements.AddRange(ExtractHeadings(html));
            elements.AddRange(ExtractParagraphs(html));
            elements.AddRange(ExtractListItems(html));

            // Si no se encontraron elementos, tratar todo como un párrafo
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

        /// <summary>
        /// Extrae encabezados del HTML
        /// </summary>
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

        /// <summary>
        /// Extrae párrafos del HTML
        /// </summary>
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

        /// <summary>
        /// Extrae elementos de lista del HTML
        /// </summary>
        private List<HtmlElement> ExtractListItems(string html)
        {
            var listItems = new List<HtmlElement>();

            // Procesar listas no ordenadas
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

            // Procesar listas ordenadas
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
                itemNumber = 1; // Reset para la siguiente lista
            }

            return listItems;
        }

        /// <summary>
        /// Obtiene el nivel del encabezado (1-6)
        /// </summary>
        private int GetHeadingLevel(string tagName)
        {
            if (tagName.Length == 2 && char.IsDigit(tagName[1]))
            {
                return int.Parse(tagName[1].ToString());
            }
            return 2; // Nivel por defecto
        }

        /// <summary>
        /// Extrae texto manteniendo formato básico de las etiquetas
        /// </summary>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            // Decodificar entidades HTML
            html = System.Net.WebUtility.HtmlDecode(html);

            // Reemplazar saltos de línea HTML
            html = html.Replace("<br>", "\n")
                       .Replace("<br/>", "\n")
                       .Replace("<br />", "\n");

            // Limpiar etiquetas no compatibles pero mantener formato básico
            html = Regex.Replace(html, @"</?(div|span|font|html|body|head|meta|title|link|script|style)[^>]*>", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Limpiar atributos pero mantener etiquetas de formato
            html = Regex.Replace(html, @"<(\w+)[^>]*>", "<$1>", RegexOptions.IgnoreCase);

            // Normalizar espacios pero mantener estructura básica
            html = Regex.Replace(html, @"\s+", " ");
            html = html.Replace("\n ", "\n").Replace(" \n", "\n");

            return html.Trim();
        }

        /// <summary>
        /// Limpia el HTML manteniendo solo formatos básicos compatibles
        /// </summary>
        private string CleanHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            // Normalizar etiquetas de formato
            html = html.Replace("<strong>", "<b>").Replace("</strong>", "</b>")
                       .Replace("<em>", "<i>").Replace("</em>", "</i>");

            // Remover etiquetas problemáticas pero mantener el contenido
            html = Regex.Replace(html, @"</?(div|span|font|html|body|head|meta|title|link|script|style)[^>]*>", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Limpiar atributos de estilo y clases
            html = Regex.Replace(html, @"\s+(style|class|id|data-[^=]+)=""[^""]*""", "",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return html.Trim();
        }

        /// <summary>
        /// Renderiza texto con formato básico - CORREGIDO: LISTAS SIN NEGRITA POR DEFECTO
        /// </summary>
        private void RenderFormattedText(TextDescriptor text, HtmlElement element)
        {
            switch (element.Type)
            {
                case HtmlElementType.Heading:
                    // Renderizar encabezados con estilo diferente
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
                    // Para elementos de lista, agregar viñeta o número SIN NEGRITA POR DEFECTO
                    if (element.IsOrderedList && element.ItemNumber.HasValue)
                    {
                        text.Span($"{element.ItemNumber}. ").FontSize(11); // SIN .SemiBold()
                    }
                    else
                    {
                        text.Span("• ").FontSize(11); // SIN .SemiBold()
                    }

                    // Procesar el contenido con formato - RESPETAR EL FORMATO DEL USUARIO
                    var listSegments = ParseTextSegments(element.Text);
                    foreach (var segment in listSegments)
                    {
                        var textSpan = text.Span(segment.Text).FontSize(11);

                        // SOLO APLICAR FORMATOS SI EL USUARIO LOS APLICÓ
                        if (segment.IsBold) textSpan = textSpan.Bold();
                        if (segment.IsItalic) textSpan = textSpan.Italic();
                        if (segment.IsUnderline) textSpan = textSpan.Underline();
                    }
                    break;

                case HtmlElementType.Paragraph:
                default:
                    // Procesar párrafos normales
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

        /// <summary>
        /// Parsea segmentos de texto con formato
        /// </summary>
        private List<TextSegment> ParseTextSegments(string html)
        {
            var segments = new List<TextSegment>();
            var currentText = new StringBuilder();
            var formatStack = new Stack<string>();

            for (int i = 0; i < html.Length; i++)
            {
                if (html[i] == '<')
                {
                    // Procesar etiqueta
                    var tagEnd = html.IndexOf('>', i);
                    if (tagEnd > i)
                    {
                        // Guardar texto acumulado antes de la etiqueta
                        if (currentText.Length > 0)
                        {
                            segments.Add(CreateTextSegment(currentText.ToString(), formatStack));
                            currentText.Clear();
                        }

                        var tagContent = html.Substring(i + 1, tagEnd - i - 1).ToLower();
                        var isClosingTag = tagContent.StartsWith("/");
                        var tagName = isClosingTag ? tagContent.Substring(1) : tagContent;

                        // Tomar solo el nombre de la etiqueta (sin atributos)
                        tagName = tagName.Split(' ')[0].Trim();

                        if (isClosingTag)
                        {
                            // Etiqueta de cierre
                            if (formatStack.Count > 0 && formatStack.Peek() == tagName)
                            {
                                formatStack.Pop();
                            }
                        }
                        else
                        {
                            // Etiqueta de apertura
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

            // Agregar el último segmento
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

        // Enumeración para tipos de elementos HTML
        private enum HtmlElementType
        {
            Paragraph,
            ListItem,
            Heading
        }

        // Clase para elementos HTML estructurados
        private class HtmlElement
        {
            public string Text { get; set; }
            public HtmlElementType Type { get; set; }
            public bool IsOrderedList { get; set; }
            public int? ItemNumber { get; set; }
            public int HeadingLevel { get; set; } = 2;
        }

        // Clase auxiliar para segmentos de texto
        private class TextSegment
        {
            public string Text { get; set; }
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsUnderline { get; set; }
        }

        // Método existente para nombre de archivo
        public string ObtenerNombreArchivo(OficioPdfModel modelo)
        {
            var nombreLimpio = RemoveDiacritics(modelo.Codigo)
                .Replace(" ", "_")
                .Replace("/", "-");
            return $"{nombreLimpio}.pdf";
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
}