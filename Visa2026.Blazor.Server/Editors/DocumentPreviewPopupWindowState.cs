using DevExpress.Blazor;

namespace Visa2026.Blazor.Server.Editors;

public enum DocumentPreviewPopupWindowMode
{
    Normal,
    Maximized,
    Minimized
}

/// <summary>Tracks normal / maximized / minimized dimensions for document preview popups.</summary>
public sealed class DocumentPreviewPopupWindowState
{
    public const string DefaultWidth = "min(98vw, 1200px)";
    public const string DefaultHeight = "min(95vh, 960px)";
    public const string MaximizedWidth = "98vw";
    public const string MaximizedHeight = "95vh";
    public const string MinimizedWidth = "min(90vw, 480px)";
    public const string MinimizedHeight = "3.25rem";

    private string _savedWidth = DefaultWidth;
    private string _savedHeight = DefaultHeight;

    public DocumentPreviewPopupWindowMode Mode { get; private set; } = DocumentPreviewPopupWindowMode.Normal;

    public string Width { get; private set; } = DefaultWidth;

    public string Height { get; private set; } = DefaultHeight;

    public bool AllowResize => Mode != DocumentPreviewPopupWindowMode.Minimized;

    public bool IsMaximized => Mode == DocumentPreviewPopupWindowMode.Maximized;

    public bool IsMinimized => Mode == DocumentPreviewPopupWindowMode.Minimized;

    public VerticalAlignment VerticalAlignment =>
        Mode == DocumentPreviewPopupWindowMode.Minimized
            ? VerticalAlignment.Bottom
            : VerticalAlignment.Center;

    public HorizontalAlignment HorizontalAlignment =>
        Mode == DocumentPreviewPopupWindowMode.Minimized
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Center;

    public void Reset()
    {
        Mode = DocumentPreviewPopupWindowMode.Normal;
        _savedWidth = DefaultWidth;
        _savedHeight = DefaultHeight;
        Width = DefaultWidth;
        Height = DefaultHeight;
    }

    public void RememberCurrentSize(string width, string height)
    {
        if (Mode == DocumentPreviewPopupWindowMode.Normal)
        {
            _savedWidth = width;
            _savedHeight = height;
        }
    }

    public void ApplyManualResize(string width, string height)
    {
        Width = width;
        Height = height;

        if (Mode == DocumentPreviewPopupWindowMode.Minimized)
            return;

        Mode = DocumentPreviewPopupWindowMode.Normal;
        _savedWidth = width;
        _savedHeight = height;
    }

    public void ToggleMaximize(string currentWidth, string currentHeight)
    {
        if (Mode == DocumentPreviewPopupWindowMode.Maximized)
        {
            Width = _savedWidth;
            Height = _savedHeight;
            Mode = DocumentPreviewPopupWindowMode.Normal;
            return;
        }

        if (Mode == DocumentPreviewPopupWindowMode.Normal)
        {
            _savedWidth = currentWidth;
            _savedHeight = currentHeight;
        }

        Width = MaximizedWidth;
        Height = MaximizedHeight;
        Mode = DocumentPreviewPopupWindowMode.Maximized;
    }

    public void ToggleMinimize(string currentWidth, string currentHeight)
    {
        if (Mode == DocumentPreviewPopupWindowMode.Minimized)
        {
            Width = _savedWidth;
            Height = _savedHeight;
            Mode = DocumentPreviewPopupWindowMode.Normal;
            return;
        }

        if (Mode == DocumentPreviewPopupWindowMode.Normal)
        {
            _savedWidth = currentWidth;
            _savedHeight = currentHeight;
        }

        Width = MinimizedWidth;
        Height = MinimizedHeight;
        Mode = DocumentPreviewPopupWindowMode.Minimized;
    }

    public string GetPopupCssClass(string baseCssClass)
    {
        return Mode switch
        {
            DocumentPreviewPopupWindowMode.Minimized => $"{baseCssClass} {baseCssClass}--minimized",
            DocumentPreviewPopupWindowMode.Maximized => $"{baseCssClass} {baseCssClass}--maximized",
            _ => baseCssClass
        };
    }
}
