﻿// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.DocumentView;
using SkiaSharp;
using System.Drawing;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.Paragraphs.Panel;

namespace Get.RichTextKit.Editor.Paragraphs;

/// <summary>
/// Abstract base class for all TextDocument paragraphs
/// </summary>
public abstract class Paragraph : IRun, IParentOrParagraph
{
    public abstract IStyle StartStyle { get; }
    public abstract IStyle EndStyle { get; }
    /// <summary>
    /// Constructs a new Paragraph
    /// </summary>
    protected Paragraph()
    {

    }
    /// <inheritdoc cref="Layout(LayoutParentInfo)"/>
    protected abstract void LayoutOverride(LayoutParentInfo owner);
    /// <summary>
    /// Layout the content of this paragraph
    /// </summary>
    /// <param name="owner">The TextDocument that owns this paragraph</param>
    public void Layout(LayoutParentInfo owner)
        => LayoutOverride(owner with { AvaliableWidth = owner.AvaliableWidth - Margin.Left - Margin.Right });

    public record struct PaintOptions(RectangleF ViewBounds, TextPaintOptions TextPaintOptions, IDocumentViewOwner? viewOwner)
    {
        
    }
    /// <summary>
    /// Paint this paragraph
    /// </summary>
    /// <param name="canvas">The canvas to paint to</param>
    /// <param name="options">Paint options</param>
    public abstract void Paint(SKCanvas canvas, PaintOptions options);

    /// <summary>
    /// Get caret position information
    /// </summary>
    /// <remarks>
    /// The returned caret info should be relative to the paragraph's content
    /// </remarks>
    /// <param name="position">The caret position</param>
    /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
    public abstract CaretInfo GetCaretInfo(CaretPosition position);

    /// <summary>
    /// Get line position information
    /// </summary>
    /// <remarks>
    /// The returned caret info should be relative to the paragraph's content
    /// </remarks>
    /// <param name="line">The line number</param>
    /// <returns>A LineInfo struct</returns>
    public abstract LineInfo GetLineInfo(int line);
    internal LineInfo GetLineInfo(Index idx) => GetLineInfo(idx.GetOffset(LineCount));
    public LineInfo GetLineInfo(int idx, bool fromEnd) => GetLineInfo(new Index(idx, fromEnd));

    /// <summary>
    /// Hit test this paragraph
    /// </summary>
    /// <param name="pt">The coordinate relative to top left of the paragraph content</param>
    /// <returns>A HitTestResult</returns>
    public abstract HitTestResult HitTest(PointF pt);

    /// <summary>
    /// Hit test a line in this paragraph
    /// </summary>
    /// <remarks>
    /// The number of lines can be determined from LineIndicies.Count.
    /// </remarks>
    /// <param name="lineIndex">The line number to be tested</param>
    /// <param name="x">The x-coordinate relative to left of the paragraph content</param>
    /// <returns>A HitTestResult</returns>
    public abstract HitTestResult HitTestLine(int lineIndex, float x);

    public virtual SelectionInfo GetSelectionInfo(TextRange selection) => new(
        selection, null, GetCaretInfo(selection.StartCaretPosition), GetCaretInfo(selection.EndCaretPosition),
        this,
        GetInteractingRuns(selection),
        GetInteractingRunsRecursive(selection),
        GetBFSInteractingRuns(selection)
    );
    // So protected members can access the method
    protected static IEnumerable<SubRunInfo> GetInteractingRuns(Paragraph para, TextRange selection)
        => para.GetInteractingRuns(selection);
    protected ParentInfo VirtualizedParentInfo
    {
        get
        {

            var parentInfo = ParentInfo;
            if (parentInfo.Parent is null)
            {
                if (this is not IParagraphCollection paragraphCollection)
                    throw new InvalidOperationException("The root paragraph must be a panel");
                parentInfo = new(paragraphCollection, 0);
            }
            return parentInfo;
        }
    }
    protected virtual IEnumerable<SubRunInfo> GetInteractingRuns(TextRange selection)
    {
        yield return new(
            VirtualizedParentInfo, selection.Minimum, Math.Abs(selection.Length),
            !(selection.Minimum <= 0 && selection.Maximum >= CodePointLength)
        );
    }
    protected static IEnumerable<SubRunInfo> GetInteractingRunsRecursive(Paragraph para, TextRange selection)
        => para.GetInteractingRunsRecursive(selection);
    protected virtual IEnumerable<SubRunInfo> GetInteractingRunsRecursive(TextRange selection)
        => GetInteractingRuns(selection);
    public IEnumerable<SubRunBFSInfo> GetBFSInteractingRuns(TextRange selection)
    {
        foreach (var subRun in GetInteractingRuns(selection))
        {
            // We can do this because IEnumerable is lazy so the function will actually not evaluate here
            var childEnumerable =
                subRun.Paragraph == this ? Enumerable.Empty<SubRunBFSInfo>() :
                subRun.Paragraph.GetBFSInteractingRuns(
                    new(subRun.Offset, subRun.Offset + subRun.Length)
                );
            if (subRun.Partial)
            {
                foreach (var sr2 in childEnumerable)
                    yield return sr2;
            } else
            {
                yield return new SubRunBFSInfo(subRun, childEnumerable);
            }
        }
    }
    /// <summary>
    /// Retrieves a list of all valid caret positions
    /// </summary>
    public abstract IReadOnlyList<int> CaretIndicies { get; }

    /// <summary>
    /// Retrieves a list of all valid word boundary caret positions
    /// </summary>
    public abstract IReadOnlyList<int> WordBoundaryIndicies { get; }

    /// <summary>
    /// Gets the length of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero length and text paragraphs
    /// should include the end of paragraph marker in the length.
    /// </remarks>
    public abstract int CodePointLength { get; }

    /// <summary>
    /// Gets the line count of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero line count
    /// </remarks>
    public abstract int LineCount { get; }
    /// <summary>
    /// Gets the line count of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero line count
    /// </remarks>
    public abstract int DisplayLineCount { get; }

    /// <summary>
    /// Qureries the height of this paragraph, excluding margins
    /// </summary>
    protected abstract float ContentHeightOverride { get; }
    /// <summary>
    /// Qureries the height of this paragraph
    /// </summary>
    public float ContentHeight => ContentHeightOverride + Margin.Top + Margin.Bottom;

    /// <summary>
    /// Queries the width of this paragraph, excluding margins
    /// </summary>
    protected abstract float ContentWidthOverride { get; }
    /// <summary>
    /// Queries the width of this paragraph
    /// </summary>
    public float ContentWidth => ContentWidthOverride + Margin.Left + Margin.Right;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ContentPosition">
    /// The coordinate of this paragraph's content
    /// </param>
    /// <param name="CodePointIndex">
    /// This code point index of this paragraph relative to the start
    /// </param>
    /// <param name="LineIndex">
    /// This line index of this paragraph relative to the start
    /// </param>
    /// <param name="DisplayLineIndex"></param>
    protected internal readonly record struct LayoutInfo
    (PointF ContentPosition, int CodePointIndex, int LineIndex, int DisplayLineIndex)
    {
        public LayoutInfo OffsetToGlobal(LayoutInfo parentGlobalInfo)
            => new(
                new(
                    ContentPosition.X + parentGlobalInfo.ContentPosition.X,
                    ContentPosition.Y + parentGlobalInfo.ContentPosition.Y
                ),
                CodePointIndex + parentGlobalInfo.CodePointIndex,
                LineIndex + parentGlobalInfo.LineIndex,
                DisplayLineIndex + parentGlobalInfo.DisplayLineIndex
            );
        public void OffsetFromThis(ref CaretInfo ci)
        {
            ci.CodePointIndex += CodePointIndex;
            ci.CaretXCoord += ContentPosition.X;
            ci.CaretRectangle.Offset(new SKPoint(ContentPosition.X, ContentPosition.Y));
            ci.LineIndex += LineIndex;
        }
        public CaretInfo OffsetFromThis(CaretInfo ci)
        {
            OffsetFromThis(ref ci);
            return ci;
        }
        public HitTestResult OffsetFromThis(HitTestResult htr)
        {
            OffsetFromThis(ref htr);
            return htr;
        }
        public void OffsetFromThis(ref HitTestResult htr)
        {

            if (htr.ClosestLine is not -1)
                htr.ClosestLine += LineIndex;
            if (htr.OverLine is not -1)
                htr.OverLine += LineIndex;
            if (htr.ClosestCodePointIndex is not -1)
                htr.ClosestCodePointIndex += CodePointIndex;
            if (htr.OverCodePointIndex is not -1)
                htr.OverCodePointIndex += CodePointIndex;
        }
        public LineInfo OffsetFromThis(LineInfo info)
        {
            OffsetFromThis(ref info);
            return info;
        }
        public void OffsetFromThis(ref LineInfo info)
        {
            info.Line += LineIndex;
            info.Start = OffsetFromThis(info.Start);
            info.End = OffsetFromThis(info.End);
            if (info.NextLine is not null)
                info.NextLine += LineIndex;
            if (info.PrevLine is not null)
                info.PrevLine += LineIndex;
        }
        public CaretPosition OffsetFromThis(CaretPosition caretPosition)
        {
            OffsetFromThis(ref caretPosition);
            return caretPosition;
        }
        public void OffsetFromThis(ref CaretPosition caretPosition)
        {
            caretPosition = new(caretPosition.CodePointIndex + CodePointIndex, caretPosition.AltPosition);
        }
        public CaretPosition OffsetToThis(CaretPosition caretPosition)
        {
            OffsetToThis(ref caretPosition);
            return caretPosition;
        }
        public void OffsetToThis(ref CaretPosition caretPosition)
        {
            caretPosition = new(caretPosition.CodePointIndex - CodePointIndex, caretPosition.AltPosition);
        }
        public TextRange OffsetFromThis(TextRange textRange)
        {
            OffsetFromThis(ref textRange);
            return textRange;
        }
        public void OffsetFromThis(ref TextRange textRange)
        {
            textRange = new(textRange.Start + CodePointIndex, textRange.End + CodePointIndex, textRange.AltPosition);
        }
        public void OffsetToThis(ref TextRange textRange)
        {
            textRange = new(textRange.Start - CodePointIndex, textRange.End - CodePointIndex, textRange.AltPosition);
        }
        public TextRange OffsetToThis(TextRange textRange)
        {
            OffsetToThis(ref textRange);
            return textRange;
        }
        public DeleteInfo OffsetFromThis(DeleteInfo info)
        {
            OffsetFromThis(ref info);
            return info;
        }
        public void OffsetFromThis(ref DeleteInfo info)
        {
            info = info with { Range = OffsetFromThis(info.Range) };
        }
        public void OffsetToThis(ref DeleteInfo info)
        {
            info = info with { Range = OffsetToThis(info.Range) };
        }
        public DeleteInfo OffsetToThis(DeleteInfo info)
        {
            OffsetToThis(ref info);
            return info;
        }
        public void OffsetToThis(ref PointF pt)
        {
            pt.X -= ContentPosition.X;
            pt.Y -= ContentPosition.Y;
        }
        public PointF OffsetToThis(PointF pt)
        {
            OffsetToThis(ref pt);
            return pt;
        }
        public void OffsetXToThis(ref float x)
        {
            x -= ContentPosition.X;
        }
        public float OffsetXToThis(float x)
        {
            OffsetXToThis(ref x);
            return x;
        }
        public void OffsetYToThis(ref float y)
        {
            y -= ContentPosition.Y;
        }
        public float OffsetYToThis(float y)
        {
            OffsetYToThis(ref y);
            return y;
        }
    }
    protected internal LayoutInfo GlobalInfo { get; internal set; }
    protected internal LayoutInfo LocalInfo { get; internal set; }

    /// <summary>
    /// The coordinate of this paragraph's content (ie: after applying margin)
    /// </summary>
    /// <remarks>
    /// This property is calculated and assigned by the TextDocument
    /// </remarks>
    protected internal PointF DrawingContentPosition { get; internal set; }
    
    /// <summary>
    /// The margin
    /// </summary>
    public Thickness Margin { get; internal set; }
    public ParentInfo ParentInfo { get; internal set; }

    // Explicit implementation of IRun so we can use RunExtensions
    // with the paragraphs collection.
    int IRun.Offset => GlobalInfo.CodePointIndex;
    int IRun.Length => CodePointLength;

    public abstract bool CanDeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection);
    public abstract bool DeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager);
    public virtual bool CanJoinWith(Paragraph other) { return false; }
    public virtual bool TryJoin(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int thisIndex) { return false; }
    public abstract Paragraph Split(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int splitIndex);
    public abstract void GetTextByAppendTextToBuffer(Utf32Buffer buffer, int position, int length);
    protected static void EnsureParagraphEnding(Paragraph p, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
        => p.EnsureParagraphEnding(UndoManager);
    protected virtual void EnsureParagraphEnding(UndoManager<Document, DocumentViewUpdateInfo> UndoManager) { }
    public Utf32Buffer GetText(int position, int length)
    {
        var buf = new Utf32Buffer();
        GetTextByAppendTextToBuffer(buf, position, length);
        return buf;
    }
    /// <summary>
    /// Get the style of the text at a specified code point index
    /// </summary>
    /// <param name="position">The offset of the code point</param>
    /// <returns>An IStyle</returns>
    public abstract IStyle GetStyleAtPosition(CaretPosition position);
    public abstract IReadOnlyList<StyleRunEx> GetStyles(int position, int length);
    public abstract void ApplyStyle(IStyle style, int position, int length);
    protected internal Document? Owner { get; private set; }
    protected internal virtual void OnParagraphAdded(Document owner) { Owner = owner; }
    protected internal virtual void OnParagraphRemoved(Document owner) { Owner = null; }
    public ParagraphIndex GlobalParagraphIndex => new(GetParaIndex(null).ToArray());
    IEnumerable<int> GetParaIndex(IParagraphCollection? stopAtParent)
    {
        var parentInfo = ParentInfo;
        if (parentInfo.Parent is not Paragraph paragraph || paragraph == stopAtParent)
        {
            yield break;
        } else
        {
            foreach (var a in paragraph.GetParaIndex(stopAtParent))
                yield return a;
            yield return parentInfo.Index;
        }
    }
}
public record struct ParagraphIndex(int[] RecursiveIndexArray)
{
    
}
public record struct StyleRunEx(int Start, int Length, IStyle Style) : IRun
{
    public StyleRunEx(StyleRun styleRun) : this(styleRun.Start, styleRun.Length, styleRun.Style)
    {

    }
    int IRun.Offset => Start;
}