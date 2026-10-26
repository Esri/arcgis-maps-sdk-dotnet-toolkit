using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class TokenParserTests
{
    [TestMethod]
    public void PlainText_CollapseWhitespace()
    {
        // Collapse all whitespace to a single space, but don't trim it from the beginning/end.
        string input = " Lots  of  extra\nspace\there.  ";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken(" Lots of extra space here. ", null, HtmlTokenType.PlainText),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void PlainText_AroundUnclosedTag()
    {
        string input = "Almost<br>nothing but text here";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("Almost", null, HtmlTokenType.PlainText),
            new HtmlToken("br", null, HtmlTokenType.OpenTag),
            new HtmlToken("nothing but text here", null, HtmlTokenType.PlainText),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void SimpleHtmlSnippet()
    {
        string input = "<p>Hello, world!</p>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("p", null, HtmlTokenType.OpenTag),
                new HtmlToken("Hello, world!", null, HtmlTokenType.PlainText),
                new HtmlToken("p", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void Error_UnmatchedBrackets()
    {
        string input = "<p>Error coming up</p";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken("Error coming up", null, HtmlTokenType.PlainText),
            // The end tag cut off by the end of input is dropped, as in browsers
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void Error_DanglingBracket()
    {
        string input = "<p>Error coming up<";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken("Error coming up<", null, HtmlTokenType.PlainText),
            // Expect tokenizer to treat the dangling bracket as plain text
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void SnippetWithAttributes()
    {
        string input = "<p class='main-text'>Hello, <strong>world</strong>!</p>";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken(name: "p", attributes: "class='main-text'", type: HtmlTokenType.OpenTag),
            new HtmlToken("Hello, ", null, HtmlTokenType.PlainText),
            new HtmlToken("strong", null, HtmlTokenType.OpenTag),
            new HtmlToken("world", null, HtmlTokenType.PlainText),
            new HtmlToken("strong", null, HtmlTokenType.CloseTag),
            new HtmlToken("!", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag)
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void BracketsInAttributes()
    {
        string input = "<a href=\"a>b\">x</a>";
        var expectedTokens = new List<HtmlToken>
            {
            new HtmlToken("a", "href=\"a>b\"", HtmlTokenType.OpenTag),
            new HtmlToken("x", null, HtmlTokenType.PlainText),
            new HtmlToken("a", null, HtmlTokenType.CloseTag)
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void QuotesInUnquotedAttributes()
    {
        // An apostrophe in an unquoted value belongs to the value. It must not open a quote that runs into the next tag.
        string input = "<p class=O'Brien>x</p><p title=it's>y</p>";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", "class=O'Brien", HtmlTokenType.OpenTag),
            new HtmlToken("x", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag),
            new HtmlToken("p", "title=it's", HtmlTokenType.OpenTag),
            new HtmlToken("y", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void UnterminatedQuotedAttribute()
    {
        string input = "<a href=\"x>link</a>y";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("a", "href=\"x", HtmlTokenType.OpenTag),
            new HtmlToken("link", null, HtmlTokenType.PlainText),
            new HtmlToken("a", null, HtmlTokenType.CloseTag),
            new HtmlToken("y", null, HtmlTokenType.PlainText),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void TestHtmlSnippetWithEscapedCharacters()
    {
        string input = "<p>&lt;Hello&gt;, &amp; welcome!</p>";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken("<Hello>, & welcome!", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag)
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void SelfClosing_InContext()
    {
        string input = "<p>Hello<br/>world!</p>";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken("Hello", null, HtmlTokenType.PlainText),
            new HtmlToken("br", null, HtmlTokenType.SelfClosingTag),
            new HtmlToken("world!", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag)
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void SelfClosing_ByItself()
    {
        var input = "<img src='image.jpg' alt='An example image' />";
        var expectedTokens = new[]
        {
            new HtmlToken("img", "src='image.jpg' alt='An example image'", HtmlTokenType.SelfClosingTag),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void NestedElements()
    {
        var input = "<div id='container'><p>Visit our <a href='https://example.com'>website</a> for more info.</p><br/>Have a great day!</div>";
        var expectedTokens = new[]
        {
            new HtmlToken("div", "id='container'", HtmlTokenType.OpenTag),
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken("Visit our ", null, HtmlTokenType.PlainText),
            new HtmlToken("a", "href='https://example.com'", HtmlTokenType.OpenTag),
            new HtmlToken("website", null, HtmlTokenType.PlainText),
            new HtmlToken("a", null, HtmlTokenType.CloseTag),
            new HtmlToken(" for more info.", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag),
            new HtmlToken("br", null, HtmlTokenType.SelfClosingTag),
            new HtmlToken("Have a great day!", null, HtmlTokenType.PlainText),
            new HtmlToken("div", null, HtmlTokenType.CloseTag),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void TestExtraWhitespaceAndLinebreaksInsideTag()
    {
        var input = "<div\n   id  =  'container'   ><p>\n<a  href = 'https://example.com'  >website</a></p></div\r\n>";
        var expectedTokens = new[]
        {
            new HtmlToken("div", "id  =  'container'", HtmlTokenType.OpenTag),
            new HtmlToken("p", null, HtmlTokenType.OpenTag),
            new HtmlToken(" ", null, HtmlTokenType.PlainText), // newline converted to space
            new HtmlToken("a", "href = 'https://example.com'", HtmlTokenType.OpenTag),
            new HtmlToken("website", null, HtmlTokenType.PlainText),
            new HtmlToken("a", null, HtmlTokenType.CloseTag),
            new HtmlToken("p", null, HtmlTokenType.CloseTag),
            new HtmlToken("div", null, HtmlTokenType.CloseTag),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void TestUnquotedAttributeValues()
    {
        var input = "<div class=container><img src=image.jpg alt=example></div>";
        var expectedTokens = new[]
        {
            new HtmlToken("div", "class=container", HtmlTokenType.OpenTag),
            new HtmlToken("img", "src=image.jpg alt=example", HtmlTokenType.OpenTag),
            new HtmlToken("div", null, HtmlTokenType.CloseTag),
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    [DataRow("script", "var html = '<p>x</p>';")]
    [DataRow("script", "if (a<b && c>d) {}")]
    [DataRow("style", "p::before { content: '&lt;'; }")]
    [DataRow("style", "p  {  color:  red; }")]
    public void RawTextContentIsLiteral(string tag, string content)
    {
        // Script and style content is neither parsed as markup nor decoded or whitespace-collapsed.
        var expectedTokens = new[]
        {
            new HtmlToken(tag, null, HtmlTokenType.OpenTag),
            new HtmlToken(content, null, HtmlTokenType.PlainText),
            new HtmlToken(tag, null, HtmlTokenType.CloseTag),
        };
        var tokens = GetTokens($"<{tag}>{content}</{tag}>").ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void ScriptCaseInsensitive()
    {
        string input = "<SCRIPT SRC='test.js'>Fallback if < needed</SCRIPT>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", "SRC='test.js'", HtmlTokenType.OpenTag), // Name normalized
                new HtmlToken("Fallback if < needed", null, HtmlTokenType.PlainText), // Raw content
                new HtmlToken("script", null, HtmlTokenType.CloseTag) // Name normalized
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void ScriptUnclosed()
    {
        string input = "<p>Before</p><script> var i = 0; // unclosed";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("p", null, HtmlTokenType.OpenTag),
                new HtmlToken("Before", null, HtmlTokenType.PlainText),
                new HtmlToken("p", null, HtmlTokenType.CloseTag),
                new HtmlToken("script", null, HtmlTokenType.OpenTag),
                new HtmlToken(" var i = 0; // unclosed", null, HtmlTokenType.PlainText) // Rest is raw text
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void TestLooseBrackets()
    {
        // Parser should treat loose brackets as plain text UNLESS followed by [a-zA-Z/!]
        var input = "<font>< 5% (flat terrain)<br /></font>";
        var expectedTokens = new[]
        {
            new HtmlToken("font", null, HtmlTokenType.OpenTag),
            new HtmlToken("< 5% (flat terrain)", null, HtmlTokenType.PlainText),
            new HtmlToken("br", null, HtmlTokenType.SelfClosingTag),
            new HtmlToken("font", null, HtmlTokenType.CloseTag)
        };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    static IEnumerable<HtmlToken> GetTokens(string html)
    {
        var parser = new HtmlTokenParser(html);
        while (parser.NextToken(out var token))
            yield return token;
    }

    private static void AssertEqual(IList<HtmlToken> expected, IList<HtmlToken> actual)
    {
        for (int i = 0; i < Math.Min(actual.Count, expected.Count); i++)
        {
            Assert.AreEqual(expected[i], actual[i], "Token #" + i);
        }
        Assert.HasCount(expected.Count, actual, "Count");
    }
}