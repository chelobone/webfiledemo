
using PDFiumSharp;
using System.Drawing;

namespace WebFileLoader.Helpers
{
    public static class PdfHelper
    {
        public static string RenderPDFAsImages(byte[] file, string fileName, string OutputFolder)
        {
            //string fileName = Path.GetFileNameWithoutExtension(Inputfile);
            string target = string.Empty;
            using (PdfDocument doc = new PdfDocument(file))
            {

                var page = doc.Pages[0];
                using (var bitmap = new Bitmap((int)page.Width, (int)page.Height))
                {
                    var grahpics = Graphics.FromImage(bitmap);
                    grahpics.Clear(Color.White);
                    page.Render(bitmap);
                    var targetFile = Path.Combine(OutputFolder, fileName + "_.png");
                    target = targetFile;
                    bitmap.Save(targetFile);
                }
            }

            return target;
        }
        public static string RenderPDFAsImages(string Inputfile, string OutputFolder)
        {
            string fileName = Path.GetFileNameWithoutExtension(Inputfile);
            string targe = string.Empty;
            using (PdfDocument doc = new PdfDocument(Inputfile))
            {

                var page = doc.Pages[0];
                using (var bitmap = new Bitmap((int)page.Width, (int)page.Height))
                {
                    var grahpics = Graphics.FromImage(bitmap);
                    grahpics.Clear(Color.White);
                    page.Render(bitmap);
                    var targetFile = Path.Combine(OutputFolder, fileName + "_.png");
                    targe = targetFile;
                    bitmap.Save(targetFile);
                }
            }

            return targe;
        }
    }
}
