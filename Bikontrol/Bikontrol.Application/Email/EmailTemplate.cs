using System.Net;
using System.Text;

namespace Bikontrol.Application.Email
{
    /// <summary>
    /// Renders the one HTML layout every Bikontrol email shares: a centred card
    /// with the brand header, paragraphs, an optional call-to-action button and a
    /// small footer. Inline styles and tables only, because mail clients ignore
    /// <c>&lt;style&gt;</c> blocks and modern CSS. Every dynamic value is
    /// HTML-encoded.
    /// </summary>
    public static class EmailTemplate
    {
        private const string BrandColor = "#003559";

        public static string Render(
            string heading,
            IEnumerable<string> paragraphs,
            string? buttonLabel = null,
            string? buttonUrl = null,
            IEnumerable<string>? bullets = null,
            string? footerLine = null,
            string? footerLinkLabel = null,
            string? footerLinkUrl = null)
        {
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">")
                .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>")
                .Append("<body style=\"margin:0;padding:0;background:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#1f2937;\">")
                .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f3f4f6;padding:24px 12px;\"><tr><td align=\"center\">")
                .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;\">")
                .Append("<tr><td style=\"background:").Append(BrandColor).Append(";padding:20px 28px;color:#ffffff;font-size:20px;font-weight:bold;\">Bikontrol</td></tr>")
                .Append("<tr><td style=\"padding:28px;font-size:15px;line-height:1.6;\">")
                .Append("<h1 style=\"margin:0 0 16px;font-size:20px;color:").Append(BrandColor).Append(";\">").Append(Encode(heading)).Append("</h1>");

            foreach (var paragraph in paragraphs)
            {
                html.Append("<p style=\"margin:0 0 14px;\">").Append(Encode(paragraph)).Append("</p>");
            }

            if (bullets is not null)
            {
                html.Append("<ul style=\"margin:0 0 14px;padding-left:20px;\">");
                foreach (var bullet in bullets)
                {
                    html.Append("<li style=\"margin-bottom:6px;\">").Append(Encode(bullet)).Append("</li>");
                }

                html.Append("</ul>");
            }

            if (buttonLabel is not null && buttonUrl is not null)
            {
                html.Append("<p style=\"margin:22px 0;\"><a href=\"").Append(Encode(buttonUrl))
                    .Append("\" style=\"background:").Append(BrandColor)
                    .Append(";color:#ffffff;text-decoration:none;padding:12px 22px;border-radius:8px;display:inline-block;font-weight:bold;\">")
                    .Append(Encode(buttonLabel)).Append("</a></p>")
                    .Append("<p style=\"margin:0 0 14px;font-size:12px;color:#6b7280;word-break:break-all;\">")
                    .Append(Encode(buttonUrl)).Append("</p>");
            }

            html.Append("</td></tr>");

            if (footerLine is not null || footerLinkUrl is not null)
            {
                html.Append("<tr><td style=\"padding:16px 28px;background:#f9fafb;font-size:12px;color:#6b7280;\">");
                if (footerLine is not null)
                {
                    html.Append(Encode(footerLine)).Append(' ');
                }

                if (footerLinkUrl is not null && footerLinkLabel is not null)
                {
                    html.Append("<a href=\"").Append(Encode(footerLinkUrl)).Append("\" style=\"color:#6b7280;\">")
                        .Append(Encode(footerLinkLabel)).Append("</a>");
                }

                html.Append("</td></tr>");
            }

            html.Append("</table></td></tr></table></body></html>");
            return html.ToString();
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
