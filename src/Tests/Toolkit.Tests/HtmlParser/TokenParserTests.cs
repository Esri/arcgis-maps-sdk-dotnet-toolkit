using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Toolkit.Tests.HtmlParser;

[TestClass]
public class TokenParserTests
{
    [TestMethod]
    public void PlainText_Only()
    {
        var t = GetTokens("Nothing but text here").ToList();
        Assert.HasCount(1, t);
        Assert.AreEqual("Nothing but text here", t[0].Name);
        Assert.IsNull(t[0].Attributes);
        Assert.AreEqual(HtmlTokenType.PlainText, t[0].Type);
    }

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
    public void AllTagTypes()
    {
        var t = GetTokens(@"none<p></p><br /><img src='foo'/>").ToList();
        Assert.HasCount(5, t);
        Assert.AreEqual(HtmlTokenType.PlainText, t[0].Type);
        Assert.AreEqual(HtmlTokenType.OpenTag, t[1].Type);
        Assert.AreEqual(HtmlTokenType.CloseTag, t[2].Type);
        Assert.AreEqual(HtmlTokenType.SelfClosingTag, t[3].Type);
        Assert.AreEqual(HtmlTokenType.SelfClosingTag, t[4].Type);
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
            // Expect tokenizer to treat the unmatched closing tag as plain text
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
        string input = "<p class=O'Brien title=it's>x</p>y";
        var expectedTokens = new List<HtmlToken>
        {
            new HtmlToken("p", "class=O'Brien title=it's", HtmlTokenType.OpenTag),
            new HtmlToken("x", null, HtmlTokenType.PlainText),
            new HtmlToken("p", null, HtmlTokenType.CloseTag),
            new HtmlToken("y", null, HtmlTokenType.PlainText),
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
    public void ScriptWithLessThan()
    {
        string input = "<script> if (x < 5) alert(\"foo\"); </script>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", null, HtmlTokenType.OpenTag),
                // Content is RAW, no processing, leading/trailing space kept if present originally (parser adds none here)
                new HtmlToken(" if (x < 5) alert(\"foo\"); ", null, HtmlTokenType.PlainText),
                new HtmlToken("script", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void ScriptWithGreaterThanAndAmpersand()
    {
        string input = "<script>if(a>b && c){d=1;}</script>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", null, HtmlTokenType.OpenTag),
                new HtmlToken("if(a>b && c){d=1;}", null, HtmlTokenType.PlainText), // Raw
                new HtmlToken("script", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void ScriptWithHtmlEntities()
    {
        string input = "<script>var msg = '<div>';</script>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", null, HtmlTokenType.OpenTag),
                new HtmlToken("var msg = '<div>';", null, HtmlTokenType.PlainText), // Entities NOT decoded
                new HtmlToken("script", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void EmptyScriptTag()
    {
        string input = "<script></script>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", null, HtmlTokenType.OpenTag),
                new HtmlToken("", null, HtmlTokenType.PlainText), // Empty text node
                new HtmlToken("script", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void ScriptWithAttributes()
    {
        string input = "<script type=\"text/javascript\">go();</script>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("script", "type=\"text/javascript\"", HtmlTokenType.OpenTag),
                new HtmlToken("go();", null, HtmlTokenType.PlainText),
                new HtmlToken("script", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
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
    public void StyleWithGreaterThan()
    {
        string input = "<style> div > p { color: blue; } </style>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("style", null, HtmlTokenType.OpenTag),
                new HtmlToken(" div > p { color: blue; } ", null, HtmlTokenType.PlainText), // Raw
                new HtmlToken("style", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void StyleWithHtmlEntities()
    {
        string input = "<style>.note::before { content: '>>'; }</style>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("style", null, HtmlTokenType.OpenTag),
                new HtmlToken(".note::before { content: '>>'; }", null, HtmlTokenType.PlainText), // Entities NOT decoded
                new HtmlToken("style", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void EmptyStyleTag()
    {
        string input = "<style></style>";
        var expectedTokens = new List<HtmlToken>
            {
                new HtmlToken("style", null, HtmlTokenType.OpenTag),
                new HtmlToken("", null, HtmlTokenType.PlainText), // Empty text node
                new HtmlToken("style", null, HtmlTokenType.CloseTag)
            };
        var tokens = GetTokens(input).ToList();
        AssertEqual(expectedTokens, tokens);
    }

    [TestMethod]
    public void TestLooseBrackets()
    {
        // Parser should treat loose brackets as plain text UNLESS followed by [a-0zA-Z/!]
        var input = "<font>< 2% (vlak en bijna vlak)<br /></font>";
        var expectedTokens = new[]
        {
            new HtmlToken("font", null, HtmlTokenType.OpenTag),
            new HtmlToken("< 2% (vlak en bijna vlak)", null, HtmlTokenType.PlainText),
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