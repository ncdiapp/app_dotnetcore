using System.Text;
using UglyToad.PdfPig;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    public static class PdfText
    {
        public static (int PageCount, string Text) Read(byte[] bytes)
        {
            using var document = PdfDocument.Open(bytes);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(page.Text);
                if (sb.Length >= DocumentStyles.PdfTextCap)
                    break;
            }
            var text = sb.ToString();
            if (text.Length > DocumentStyles.PdfTextCap)
                text = text.Substring(0, DocumentStyles.PdfTextCap);
            return (document.NumberOfPages, text);
        }
    }
}
