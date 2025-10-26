using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.IO;

namespace WindowProfileCalculatorLibrary
{
    public class SegoeFontResolver : IFontResolver
    {
        public static readonly SegoeFontResolver Instance = new SegoeFontResolver();

        public string DefaultFontName => "Segoe UI";

        public byte[]? GetFont(string faceName)
        {
            try
            {
                string fontPath = @"C:\Windows\Fonts\segoeui.ttf";
                if (File.Exists(fontPath))
                    return File.ReadAllBytes(fontPath);
            }
            catch { }
            return null;
        }

       
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (string.Equals(familyName, "Segoe UI", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold && isItalic)
                    return new FontResolverInfo("Segoe UI Bold Italic");
                if (isBold)
                    return new FontResolverInfo("Segoe UI Bold");
                if (isItalic)
                    return new FontResolverInfo("Segoe UI Italic");
                return new FontResolverInfo("Segoe UI");
            }

            // fallback
            return new FontResolverInfo("Arial");
        }
    }
}
