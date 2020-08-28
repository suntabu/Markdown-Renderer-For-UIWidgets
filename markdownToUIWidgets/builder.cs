using System;
using System.Collections.Generic;
using System.Linq;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using UnityEngine.Assertions;
using Image = Unity.UIWidgets.widgets.Image;

namespace markdownRender
{
    public class builder
    {
        static List<string> _kBlockTags = new List<string>()
        {
            "p",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "li",
            "blockquote",
            "pre",
            "ol",
            "ul",
            "hr"
        };


        private static readonly List<string> _kListTags = new List<string>()
        {
            "ul",
            "ol"
        };

        static bool _isBlockTag(string tag)
        {
            return _kBlockTags.Contains(tag);
        }

        static bool _isListTag(string tag)
        {
            return _kListTags.Contains(tag);
        }

        class _BlockElement
        {
            public string tag;
            public List<Widget> children = new List<Widget>();
            public int nextListIndex = 0;

            public _BlockElement(string tag)
            {
                this.tag = tag;
            }
        }

        /// A collection of widgets that should be placed adjacent to (inline with)
        /// other inline elements in the same parent block.
        ///
        /// Inline elements can be textual (a/em/strong) represented by [RichText]
        /// widgets or images (img) represented by [Image.network] widgets.
        ///
        /// Inline elements can be nested within other inline elements, inheriting their
        /// parent's style along with the style of the block they are in.
        ///
        /// When laying out inline widgets, first, any adjacent RichText widgets are
        /// merged, then, all inline widgets are enclosed in a parent [Wrap] widget.
        class _InlineElement
        {
            public string tag;

            /// Created by merging the style defined for this element's [tag] in the
            /// delegate's [MarkdownStyleSheet] with the style of its parent.
            public TextStyle style;

            public _InlineElement(string tag, TextStyle style)
            {
                this.tag = tag;
                this.style = style;
            }

            public List<Widget> children = new List<Widget>();
        }

        /// A delegate used by [MarkdownBuilder] to control the widgets it creates.
        public interface IMarkdownBuilderDelegate
        {
            /// Returns a gesture recognizer to use for an `a` element with the given
            /// `href` attribute.
            GestureRecognizer createLink(string href);

            /// The `styleSheet` is the value of [MarkdownBuilder.styleSheet].
            TextSpan formatText(markdown.MarkdownStyleSheet styleSheet, string code);
        }

        /// Builds a [Widget] tree from parsed Markdown.
        ///
        /// See also:
        ///
        ///  * [Markdown], which is a widget that parses and displays Markdown.
        public class MarkdownBuilder : markdown.NodeVisitor
        {
            /// A delegate that controls how link and `pre` elements behave.
            private IMarkdownBuilderDelegate myDelegate;

            /// Defines which [TextStyle] objects to use for each type of element.
            private markdown.MarkdownStyleSheet styleSheet;

            /// The base directory holding images referenced by Img tags with local file paths.
            private string imageDirectory;

            List<string> _listIndents = new List<string>();
            List<_BlockElement> _blocks = new List<_BlockElement>();
            List<_InlineElement> _inlines = new List<_InlineElement>();
            List<GestureRecognizer> _linkHandlers = new List<GestureRecognizer>();

            /// Creates an object that builds a [Widget] tree from parsed Markdown.
            public MarkdownBuilder(IMarkdownBuilderDelegate myDelegate, markdown.MarkdownStyleSheet styleSheet,
                string imgDir)
            {
                this.myDelegate = myDelegate;
                this.styleSheet = styleSheet;
                this.imageDirectory = imgDir;
            }

            /// Returns widgets that display the given Markdown nodes.
            ///
            /// The returned widgets are typically used as children in a [ListView].
            public List<Widget> build(List<markdown.Node> nodes)
            {
                _listIndents.Clear();
                _blocks.Clear();
                _inlines.Clear();
                _linkHandlers.Clear();

                _blocks.Add(new _BlockElement(null));

                foreach (var node in nodes)
                {
                    UnityEngine.Debug.Assert(_blocks.Count == 1, "_blocks.Count == 1");
                    node.accept(this);
                }

                Debug.Assert(_inlines.isEmpty(), "_inlines.isEmpty()");
                return _blocks.Single().children;
            }

            public override void visitText(markdown.Text text)
            {
                if (_blocks.last().tag == null) return; // Don't allow text directly under the root.

                _addParentInlineIfNeeded(_blocks.last().tag);
//                Debug.Log("---> c: " + _blocks.Count + "   " + _inlines.Count + "  " + _linkHandlers.Count);
                TextSpan span = _blocks.last().tag == "pre"
                    ? myDelegate.formatText(styleSheet, text.text)
                    : new TextSpan(
                        text.text,
                        _inlines.last().style,
                        null,
                        _linkHandlers.Any() ? _linkHandlers.last() : null);
                _inlines.last().children.Add(new RichText(null, span));
            }

            public override bool visitElementBefore(markdown.Element element)
            {
                string tag = element.tag;
                if (_isBlockTag(tag))
                {
//                    Debug.Log("tag---> " + tag);
                    _addAnonymousBLockIfNeeded(styleSheet.styles(tag));
                    if (_isListTag(tag))
                    {
                        _listIndents.Add(tag);
                    }

                    _blocks.Add(new _BlockElement(tag));
                }
                else
                {
                    _addParentInlineIfNeeded(_blocks.last().tag);
                    TextStyle parentStyle = _inlines.last().style;

                    _inlines.Add(new _InlineElement(tag, parentStyle.merge(styleSheet.styles(tag))));
                }

                if (tag == "a")
                {
                    _linkHandlers.Add(myDelegate.createLink(element.attributes["href"]));
                }

                return true;
            }


            public override void visitElementAfter(markdown.Element element)
            {
                string tag = element.tag;
                if (_isBlockTag(tag))
                {
                    _addAnonymousBLockIfNeeded(styleSheet.styles(tag));

                    _BlockElement current = _blocks.removeLast();
                    Widget child;

                    if (current.children.isNotEmpty())
                    {
                        child = new Column(null, null, null, MainAxisAlignment.start, MainAxisSize.max,
                            CrossAxisAlignment.stretch, VerticalDirection.down, current.children);
                    }
                    else
                    {
                        child = new SizedBox();
                    }

                    if (_isListTag(tag))
                    {
                        Debug.Assert(_listIndents.isNotEmpty(), "_listIndents.isNotEmpty()");
                        _listIndents.removeLast();
                    }
                    else if (tag == "li")
                    {
                        if (_listIndents.isNotEmpty())
                        {
                            child = new Row(
                                null,
                                null,
                                null,
                                MainAxisAlignment.start,
                                MainAxisSize.max,
                                CrossAxisAlignment.start,
                                VerticalDirection.down,
                                new List<Widget>()
                                {
                                    new SizedBox(null, styleSheet.listIndent, null, _buildBullet(_listIndents.last())),
                                    new Expanded(null, 1, child)
                                });
                        }
                    }
                    else if (tag == "blockquote")
                    {
                        child = new DecoratedBox(
                            null,
                            styleSheet.blockquoteDecoration,
                            DecorationPosition.background,
                            new Padding(
                                null,
                                EdgeInsets.all(styleSheet.blockquotePadding),
                                child));
                    }
                    else if (tag == "pre")
                    {
                        child = new DecoratedBox(
                            null,
                            styleSheet.codeblockDecoration,
                            DecorationPosition.background,
                            new Padding(
                                null,
                                EdgeInsets.all(styleSheet.codeblockPadding),
                                child));
                    }
                    else if (tag == "hr")
                    {
                        child = new DecoratedBox(
                            null,
                            styleSheet.horizontalRuleDecoration,
                            DecorationPosition.background,
                            child);
                    }

                    _addBlockChild(child);
                }
                else
                {
                    _InlineElement current = _inlines.removeLast();
                    _InlineElement parent = _inlines.last();

                    if (tag == "img")
                    {
                        current.children.Add(_buildImage(element.attributes["src"]));
                    }
                    else if (tag == "a")
                    {
                        _linkHandlers.removeLast();
                    }

                    if (current.children.isNotEmpty())
                    {
                        parent.children.AddRange(current.children);
                    }
                }
            }

            Widget _buildImage(string src)
            {
                string[] parts = src.Split('#');
                if (parts.isEmpty()) return SizedBox.expand();

                string path = parts.first();
                float width = 0, height = 0;
                if (parts.Length == 2)
                {
                    var dimensions = parts.last().Split('x');
                    if (dimensions.Length == 2)
                    {
                        width = float.Parse(dimensions[0]);
                        height = float.Parse(dimensions[1]);
                    }
                }

                Uri uri = new Uri(path);
                Widget child;
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    child = Image.network(uri.ToString(), null, 1);
                }
                else if (uri.Scheme == "data")
                {
                    child = _handleDataSchemeUri(uri, width, height);
                }
                else if (uri.Scheme == "resource")
                {
                    //TODO:
                    child = Image.asset(path.Substring(9), null, null, width, height);
                }
                else
                {
                    string filePath = imageDirectory == null
                        ? uri.ToString()
                        : System.IO.Path.Combine(imageDirectory, uri.ToString());
                    child = Image.file(filePath, null, 1, width, height);
                }

                if (_linkHandlers.isNotEmpty())
                {
                    TapGestureRecognizer recognizer = _linkHandlers.last() as TapGestureRecognizer;
                    return new GestureDetector(null, child, null, null, recognizer.onTap);
                }
                else
                {
                    return child;
                }
            }

            Widget _handleDataSchemeUri(Uri uri, float widht, float height)
            {
                //TODO:
                return SizedBox.expand();
            }

            Widget _buildBullet(string listTag)
            {
                if (listTag == "ul")
                    return new Text("•", null, styleSheet.styles("li"), textAlign: TextAlign.center);

                int index = _blocks.last().nextListIndex;
                return new Padding(null, EdgeInsets.only(5.0f),
                    new Text((index + 1) + ".", null, styleSheet.styles("li"), textAlign: TextAlign.right));
            }

            void _addParentInlineIfNeeded(string tag)
            {
                if (_inlines.isEmpty() && !string.IsNullOrEmpty(tag))
                {
//                    Debug.Log("inlines add--->" + tag);
                    _inlines.Add(new _InlineElement(tag, styleSheet.styles(tag)));
                }
            }

            void _addBlockChild(Widget child)
            {
                Assert.IsTrue(_blocks.Count > 0, "_blocks.Count > 0");
                _BlockElement parent = _blocks.last();
                if (parent.children.isNotEmpty())
                    parent.children.Add(new SizedBox(null, null, styleSheet.blockSpacing));
                parent.children.Add(child);
                parent.nextListIndex += 1;
            }

            void _addAnonymousBLockIfNeeded(TextStyle style)
            {
                if (_inlines.isEmpty())
                {
                    return;
                }

                _InlineElement inline = _inlines.Single();
                if (inline.children.isNotEmpty())
                {
                    List<Widget> mergedInlines = _mergeInlineChildren(inline);
                    Wrap wrap = new Wrap(null, Axis.horizontal, WrapAlignment.start, 0, WrapAlignment.start, 0,
                        WrapCrossAlignment.start, null, VerticalDirection.down, mergedInlines);
                    _addBlockChild(wrap);
                    _inlines.Clear();
                }
            }

            List<Widget> _mergeInlineChildren(_InlineElement inline)
            {
                List<Widget> mergedTexts = new List<Widget>();
                foreach (Widget child in inline.children)
                {
                    var childText = child as RichText;
                    if (mergedTexts.isNotEmpty() && mergedTexts.last() is RichText && childText != null)
                    {
                        RichText previous = (RichText) mergedTexts.removeLast();
                        List<TextSpan> children =
                            previous.text.children != null
                                ? previous.text.children
                                : new List<TextSpan>() {previous.text};
                        children.Add(childText.text);
                        TextSpan mergeSpan = new TextSpan("", null, children);
                        mergedTexts.Add(new RichText(text:mergeSpan));
                    }
                    else
                    {
                        mergedTexts.Add(child);
                    }
                }

                return mergedTexts;
            }
        }
    }
}