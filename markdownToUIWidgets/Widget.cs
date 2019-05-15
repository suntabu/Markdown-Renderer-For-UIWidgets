using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using markdownRender;
using UIWidgetsGallery.gallery;
using Unity.UIWidgets;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.material;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.service;
using Unity.UIWidgets.widgets;
using Debug = UnityEngine.Debug;
using Dispatcher = UnityToolbag.Dispatcher;
using ThreadState = System.Threading.ThreadState;

namespace markdown
{
    /// Signature for callbacks used by [MarkdownWidget] when the user taps a link.
    ///
    /// Used by [MarkdownWidget.onTapLink].
    public delegate void MarkdownTapLinkCallback(string href);

    /// Creates a format [TextSpan] given a string.
    ///
    /// Used by [MarkdownWidget] to highlight the contents of `pre` elements.
//    public abstract class SyntaxHighlighter
//    {
//        /// Returns the formated [TextSpan] for the given string.
//        protected internal abstract TextSpan format(string source);
//    }


    /// A base class for widgets that parse and display Markdown.
    ///
    /// Supports all standard Markdown from the original
    /// [Markdown specification](https://daringfireball.net/projects/markdown/).
    ///
    /// See also:
    ///
    ///  * [Markdown], which is a scrolling container of Markdown.
    ///  * [MarkdownBody], which is a non-scrolling container of Markdown.
    ///  * <https://daringfireball.net/projects/markdown/>
    public abstract class MarkdownWidget : StatefulWidget
    {
        public Action<string, List<Node>> onParsed;
        public Func<string, List<Node>> getCachedParsed;

        public string data;

        public string id;

        public MarkdownStyleSheet styleSheet;

        internal SyntaxHighlighter syntaxHighlighter;

        internal MarkdownTapLinkCallback onTapLink;

        public string imageDirectory;

        /// Creates a widget that parses and displays Markdown.
        ///
        /// The [data] argument must not be null.
        protected MarkdownWidget(Key key, string data, MarkdownStyleSheet markdownStyleSheet,
            SyntaxHighlighter syntaxHighlighter1, MarkdownTapLinkCallback onTapLink, string imageDirectory) : base(key)
        {
            Debug.Assert(data != null, "data != null");
            this.data = data;
            this.styleSheet = markdownStyleSheet;
            this.syntaxHighlighter = syntaxHighlighter1;
            this.onTapLink = onTapLink;
            this.imageDirectory = imageDirectory;
        }

        /// Subclasses should override this function to display the given children,
        /// which are the parsed representation of [data].
        public abstract Widget build(BuildContext context, List<Widget> children);

        public override State createState()
        {
            return new _MarkdownWidgetState();
        }
    }

    class _MarkdownWidgetState : State<MarkdownWidget>, builder.IMarkdownBuilderDelegate
    {
        private static Thread buildThread;
    
        private List<Widget> _children = new List<Widget>();


        List<GestureRecognizer> _recognizers = new List<GestureRecognizer>();

        public override void didChangeDependencies()
        {
            _parseMarkdown();
            base.didChangeDependencies();
        }

        public override void didUpdateWidget(StatefulWidget oldWidget)
        {
            var noldWidget = oldWidget as MarkdownWidget;
            if (oldWidget == null)
            {
                return;
            }

            base.didUpdateWidget(oldWidget);

            if (widget.data != noldWidget.data || widget.styleSheet != noldWidget.styleSheet)
            {
                _parseMarkdown();
            }
        }

        public override void dispose()
        {
            Debug.Log("disposed!!!!");
            _disposeRecongnizer();
            base.dispose();

            if (buildThread != null && buildThread.ThreadState == ThreadState.Running)
            {
                Debug.Log("disposed!!!!");
                
                buildThread.Abort();
                buildThread = null;
            }
        }

        void _parseMarkdown()
        {
            updateState(new List<Widget>());
            MarkdownStyleSheet styleSheet = widget.styleSheet ?? MarkdownStyleSheet.fromTheme(new ThemeData(brightness: Brightness.light,fontFamily:"Avenir"));
            _disposeRecongnizer();

            // TODO: This can be optimized by doing the split and removing \r at the same time
            string[] lines = widget.data.Replace("\r\n", "\n").Split('\n');
            markdown.Document document = new Document();
            builder.MarkdownBuilder builder = new builder.MarkdownBuilder(this, styleSheet, widget.imageDirectory);

            List<Node> nodes = null;
//            if (widget.getCachedParsed != null)
//            {
//                nodes = widget.getCachedParsed(widget.id);
//            }


//            _children = builder.build(document.parseLines(lines.ToList().remove(string.IsNullOrEmpty)));

            if (buildThread != null && buildThread.ThreadState == ThreadState.Running)
            {
                buildThread.Abort();
            }

            buildThread = new Thread(() =>
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    if (nodes == null)
                    {
                        nodes = document.parseLines(lines.ToList());
                    }


                    if (widget.onParsed != null)
                    {
                        widget.onParsed(widget.id, nodes);
                    }


                    var elements = builder.build(nodes);

                    Dispatcher.Invoke(() =>
                    {
                        updateState(elements);    
                    });
                    
                    
                    sw.Stop();
                    Debug.Log(sw.ElapsedMilliseconds / 1000f);
                }
                catch (ThreadAbortException e)
                {
                    Debug.Log(e.Message);
                }
            });
             
            
            buildThread.Start();
        }

        private void updateState(List<Widget> elements)
        {
            using (WindowProvider.of(context).getScope())
            {
                Debug.Log("====>" + elements.Count + "  " + _children.Count);
                this.setState((() => { _children = elements; }));
            }
        }

        void _disposeRecongnizer()
        {
            if (_recognizers.isEmpty())
            {
                return;
            }

            GestureRecognizer[] localRecognizers = _recognizers.ToArray();
            _recognizers.Clear();
            foreach (var gestureRecognizer in localRecognizers)
            {
                gestureRecognizer.dispose();
            }
        }


        public override Widget build(BuildContext context)
        {
            return widget.build(context, _children);
        }

        public GestureRecognizer createLink(string href)
        {
            TapGestureRecognizer recognizer = new TapGestureRecognizer();
            recognizer.onTap = () =>
            {
                if (widget.onTapLink != null)
                    widget.onTapLink(href);
            };

            _recognizers.Add(recognizer);
            return recognizer;
        }

        public TextSpan formatText(MarkdownStyleSheet styleSheet, string code)
        {
            //TODO: format code!
            if (widget.syntaxHighlighter != null)
                return widget.syntaxHighlighter.format(code);
            return new TextSpan(code, styleSheet.code);
        }
    }


    /// A scrolling widget that parses and displays Markdown.
    ///
    /// Supports all standard Markdown from the original
    /// [Markdown specification](https://daringfireball.net/projects/markdown/).
    ///
    /// See also:
    ///
    ///  * [MarkdownBody], which is a non-scrolling container of Markdown.
    ///  * <https://daringfireball.net/projects/markdown/>
    public class Markdown : MarkdownWidget
    {
        /// Creates a scrolling widget that parses and displays Markdown.
        public Markdown(Key key = null,
            String data = null,
            MarkdownStyleSheet styleSheet = null,
            SyntaxHighlighter syntaxHighlighter = null,
            MarkdownTapLinkCallback onTapLink = null,
            string imageDirectory = null) : base(key, data, styleSheet, syntaxHighlighter, onTapLink, imageDirectory)
        {
            this.padding = EdgeInsets.all(16);
        }

        /// The amount of space by which to inset the children.
        EdgeInsets padding;


        public override Widget build(BuildContext context, List<Widget> children)
        {
            return new ListView(null, Axis.vertical, false, null, null, null, false, padding, null, true, true, null,
                children);
 
        }
    }
}