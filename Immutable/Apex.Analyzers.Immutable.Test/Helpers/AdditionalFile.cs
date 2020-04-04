using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace TestHelper
{
    public class AdditionalFile : AdditionalText
    {
        public AdditionalFile(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public override string Path { get; }
        public string Text { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(Text);
        }
    }
}