using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;

namespace CopyCopilotReference
{
    public partial class TabSelectionDialog : DialogWindow, INotifyPropertyChanged
    {
        private const int MaxVisibleItemCount = 10;
        private const string CjkPaddingText = "\u6C49\u5B57";
        private const string CjkSingleCharText = "\u6C49";

        private double directoryColumnWidth;
        private double checkboxSpacingWidth;
        private double rowHeight;
        private double listHeight;
        private Brush directoryBrush;
        private Brush foregroundBrush;
        private Brush highlightBrush;
        private Brush highlightTextBrush;

        public TabSelectionDialog(IReadOnlyList<string> paths)
        {
            InitializeComponent();

            Items = new ObservableCollection<TabEntry>(paths.Select(path => new TabEntry(path)));
            DataContext = this;

            UpdateThemeBrushes();
            UpdateLayoutMetrics();

            VSColorTheme.ThemeChanged += OnThemeChanged;
            Closed += OnClosed;
        }

        public ObservableCollection<TabEntry> Items { get; }

        public double DirectoryColumnWidth
        {
            get => directoryColumnWidth;
            private set
            {
                if (Math.Abs(directoryColumnWidth - value) < 0.1)
                {
                    return;
                }

                directoryColumnWidth = value;
                NotifyPropertyChanged(nameof(DirectoryColumnWidth));
            }
        }

        public double CheckboxSpacingWidth
        {
            get => checkboxSpacingWidth;
            private set
            {
                if (Math.Abs(checkboxSpacingWidth - value) < 0.1)
                {
                    return;
                }

                checkboxSpacingWidth = value;
                NotifyPropertyChanged(nameof(CheckboxSpacingWidth));
            }
        }

        public double RowHeight
        {
            get => rowHeight;
            private set
            {
                if (Math.Abs(rowHeight - value) < 0.1)
                {
                    return;
                }

                rowHeight = value;
                NotifyPropertyChanged(nameof(RowHeight));
            }
        }

        public double ListHeight
        {
            get => listHeight;
            private set
            {
                if (Math.Abs(listHeight - value) < 0.1)
                {
                    return;
                }

                listHeight = value;
                NotifyPropertyChanged(nameof(ListHeight));
            }
        }

        public Brush DirectoryBrush
        {
            get => directoryBrush;
            private set
            {
                directoryBrush = value;
                NotifyPropertyChanged(nameof(DirectoryBrush));
            }
        }

        public Brush ForegroundBrush
        {
            get => foregroundBrush;
            private set
            {
                foregroundBrush = value;
                NotifyPropertyChanged(nameof(ForegroundBrush));
            }
        }

        public Brush HighlightBrush
        {
            get => highlightBrush;
            private set
            {
                highlightBrush = value;
                NotifyPropertyChanged(nameof(HighlightBrush));
            }
        }

        public Brush HighlightTextBrush
        {
            get => highlightTextBrush;
            private set
            {
                highlightTextBrush = value;
                NotifyPropertyChanged(nameof(HighlightTextBrush));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<string> GetSelectedPaths()
        {
            return Items.Where(item => item.IsChecked).Select(item => item.FullPath).ToList();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBoxItem item && item.DataContext is TabEntry entry)
            {
                entry.IsChecked = !entry.IsChecked;
                e.Handled = true;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            VSColorTheme.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateThemeBrushes();
        }

        private void UpdateThemeBrushes()
        {
            var background = ToMediaColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
            var foreground = ToMediaColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
            var highlight = ToMediaColor(VSColorTheme.GetThemedColor(EnvironmentColors.SystemHighlightColorKey));
            var highlightText = ToMediaColor(VSColorTheme.GetThemedColor(EnvironmentColors.SystemHighlightTextColorKey));

            Background = new SolidColorBrush(background);
            Foreground = new SolidColorBrush(foreground);

            ForegroundBrush = new SolidColorBrush(foreground);
            HighlightBrush = new SolidColorBrush(highlight);
            HighlightTextBrush = new SolidColorBrush(highlightText);
            DirectoryBrush = new SolidColorBrush(BlendColor(foreground, background, 0.35f));
        }

        private void UpdateLayoutMetrics()
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            CheckboxSpacingWidth = MeasureTextWidth(CjkSingleCharText, typeface, FontSize, pixelsPerDip);
            var twoCjkWidth = MeasureTextWidth(CjkPaddingText, typeface, FontSize, pixelsPerDip);

            var maxDirectory = 0d;
            var maxFile = 0d;
            foreach (var item in Items)
            {
                maxDirectory = Math.Max(maxDirectory, MeasureTextWidth(item.DirectoryText, typeface, FontSize, pixelsPerDip));
                maxFile = Math.Max(maxFile, MeasureTextWidth(item.FileName, typeface, FontSize, pixelsPerDip));
            }

            DirectoryColumnWidth = maxDirectory + twoCjkWidth;
            RowHeight = Math.Ceiling(MeasureTextHeight(typeface, FontSize, pixelsPerDip) + 8);
            ListHeight = RowHeight * Math.Max(1, Math.Min(MaxVisibleItemCount, Items.Count));

            var contentWidth = CheckboxSpacingWidth + DirectoryColumnWidth + maxFile + 160;
            Width = Math.Max(480, contentWidth);
        }

        private static double MeasureTextWidth(string text, Typeface typeface, double fontSize, double pixelsPerDip)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0d;
            }

            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            return Math.Ceiling(formatted.WidthIncludingTrailingWhitespace);
        }

        private static double MeasureTextHeight(Typeface typeface, double fontSize, double pixelsPerDip)
        {
            var formatted = new FormattedText(
                "A",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            return Math.Ceiling(formatted.Height);
        }

        private static Color BlendColor(Color foreground, Color background, double amount)
        {
            var clamped = Math.Max(0d, Math.Min(1d, amount));
            var r = (byte)(foreground.R * (1d - clamped) + background.R * clamped);
            var g = (byte)(foreground.G * (1d - clamped) + background.G * clamped);
            var b = (byte)(foreground.B * (1d - clamped) + background.B * clamped);
            return Color.FromArgb(foreground.A, r, g, b);
        }

        private static Color ToMediaColor(DrawingColor color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public sealed class TabEntry : INotifyPropertyChanged
        {
            private bool isChecked;

            public TabEntry(string fullPath)
            {
                FullPath = fullPath ?? string.Empty;
                var directory = System.IO.Path.GetDirectoryName(FullPath);
                DirectoryText = string.IsNullOrEmpty(directory) ? string.Empty : directory + System.IO.Path.DirectorySeparatorChar;
                FileName = System.IO.Path.GetFileName(FullPath) ?? string.Empty;
            }

            public string FullPath { get; }

            public string DirectoryText { get; }

            public string FileName { get; }

            public bool IsChecked
            {
                get => isChecked;
                set
                {
                    if (isChecked == value)
                    {
                        return;
                    }

                    isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
