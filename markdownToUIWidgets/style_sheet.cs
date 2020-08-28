using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Unity.UIWidgets.material;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;

namespace markdown
{
    public class MarkdownStyleSheet
    {
        public MarkdownStyleSheet(
            TextStyle a,
            TextStyle p,
            TextStyle code,
            TextStyle h1,
            TextStyle h2,
            TextStyle h3,
            TextStyle h4,
            TextStyle h5,
            TextStyle h6,
            TextStyle em,
            TextStyle strong,
            TextStyle blockquote,
            TextStyle img,
            float blockSpacing,
            float listIndent,
            float blockquotePadding,
            Decoration blockquoteDecoration,
            float codeblockPadding,
            Decoration codeblockDecoration,
            Decoration horizontalRuleDecoration)
        {
            _styles = new Dictionary<string, TextStyle>()
            {
                {"a", a},
                {"p", p},
                {"li", p},
                {"code", code},
                {"pre", p},
                {"h1", h1},
                {"h2", h2},
                {"h3", h3},
                {"h4", h4},
                {"h5", h5},
                {"h6", h6},
                {"em", em},
                {"strong", strong},
                {"blockquote", blockquote},
                {"img", img},
                {"ul", p},
            };

            this.a = a;
            this.p = p;
            this.code = code;
            this.h1 = h1;
            this.h2 = h2;
            this.h3 = h3;
            this.h4 = h4;
            this.h5 = h5;
            this.h6 = h6;
            this.em = em;
            this.strong = strong;
            this.blockquote = blockquote;
            this.img = img;
            this.blockSpacing = blockSpacing;
            this.listIndent = listIndent;
            this.blockquotePadding = blockquotePadding;
            this.blockquoteDecoration = blockquoteDecoration;
            this.codeblockPadding = codeblockPadding;
            this.codeblockDecoration = codeblockDecoration;
            this.horizontalRuleDecoration = horizontalRuleDecoration;
        }

        public TextStyle a,
            p,
            code,
            h1,
            h2,
            h3,
            h4,
            h5,
            h6,
            em,
            strong,
            blockquote;

        public float blockSpacing,
            listIndent,
            blockquotePadding;

        public Decoration blockquoteDecoration, codeblockDecoration, horizontalRuleDecoration;
        public float codeblockPadding;


        private Dictionary<string, TextStyle> _styles;

        public TextStyle styles(string tag)
        {
            if (_styles.ContainsKey(tag))
                return _styles[tag];

            return null;
        }
        
        private TextStyle img;

        /// Creates a [MarkdownStyleSheet] from the [TextStyle]s in the provided [ThemeData].
        public static MarkdownStyleSheet fromTheme(ThemeData theme)
        {
            return new MarkdownStyleSheet(
                new TextStyle(true, Colors.blue),
                theme.textTheme.body1,
                new TextStyle(true, Colors.grey.shade700, fontSize: theme.textTheme.body1.fontSize * .85f, fontFamily: "monospace"),
                theme.textTheme.headline,
                theme.textTheme.title,
                theme.textTheme.subhead,
                theme.textTheme.body2,
                theme.textTheme.body2,
                theme.textTheme.body2,
                new TextStyle(true, fontStyle: FontStyle.italic),
                new TextStyle(true, fontWeight: FontWeight.bold),
                theme.textTheme.body1,
                theme.textTheme.body1,
                8,
                32,
                8,
                new BoxDecoration(null, null, null, BorderRadius.circular(2), null, null, null),
                8,
                new BoxDecoration(Colors.grey.shade100, null, null, BorderRadius.circular(2), null, null, null),
                new BoxDecoration(null, null, new Border(
                    new BorderSide(Colors.grey.shade300, 5)))
            ); ;
        }
    }
}