using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PVZRHTools.Themes
{
    /// <summary>
    /// 主题管理器 - 支持深色/浅色主题切换动画
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public bool IsDarkTheme { get; private set; } = false;

        // 浅色主题颜色
        public static class LightTheme
        {
            public static Color Background => Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
            public static Color CardBackground => Colors.White;
            public static Color Foreground => Color.FromRgb(0x28, 0x2C, 0x34);
            public static Color SecondaryForeground => Color.FromRgb(0x33, 0x33, 0x33);
            public static Color Accent => Color.FromRgb(0xFF, 0x69, 0xB4); // 粉色
            public static Color AccentLight => Color.FromRgb(0xFF, 0xB6, 0xC1);
            public static Color Border => Color.FromRgb(0xDB, 0x70, 0x93);
            public static Color TabBackground => Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
        }

        // 深色主题颜色
        public static class DarkTheme
        {
            public static Color Background => Color.FromArgb(0xF0, 0x1E, 0x1E, 0x2E);
            public static Color CardBackground => Color.FromRgb(0x2D, 0x2D, 0x3D);
            public static Color Foreground => Color.FromRgb(0xE0, 0xE0, 0xE0);
            public static Color SecondaryForeground => Color.FromRgb(0xB0, 0xB0, 0xB0);
            public static Color Accent => Color.FromRgb(0xFF, 0x79, 0xC6); // 亮粉色
            public static Color AccentLight => Color.FromRgb(0xFF, 0x92, 0xD0);
            public static Color Border => Color.FromRgb(0xFF, 0x79, 0xC6);
            public static Color TabBackground => Color.FromArgb(0x80, 0x2D, 0x2D, 0x3D);
        }

        // 存储原始渐变边框以便恢复
        private LinearGradientBrush? _originalBorderBrush;

        public event Action<bool>? ThemeChanged;

        /// <summary>
        /// 切换主题（带动画）
        /// </summary>
        public void ToggleTheme(Window window)
        {
            IsDarkTheme = !IsDarkTheme;
            ApplyTheme(window, IsDarkTheme, true);
            ThemeChanged?.Invoke(IsDarkTheme);
        }

        /// <summary>
        /// 设置主题
        /// </summary>
        public void SetTheme(Window window, bool isDark, bool animate = true)
        {
            IsDarkTheme = isDark;
            ApplyTheme(window, isDark, animate);
            ThemeChanged?.Invoke(IsDarkTheme);
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme(Window window, bool isDark, bool animate)
        {
            var duration = animate ? TimeSpan.FromMilliseconds(400) : TimeSpan.Zero;
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            // 获取目标颜色
            var targetBg = isDark ? DarkTheme.Background : LightTheme.Background;
            var targetFg = isDark ? DarkTheme.Foreground : LightTheme.Foreground;

            // 窗口背景动画
            AnimateBrushProperty(window, Control.BackgroundProperty, targetBg, duration, easing);

            // 前景色动画
            AnimateBrushProperty(window, Control.ForegroundProperty, targetFg, duration, easing);

            // 边框动画 - 渐变边框特殊处理
            if (window.BorderBrush is LinearGradientBrush gradientBrush)
            {
                // 保存原始渐变边框
                if (_originalBorderBrush == null)
                {
                    _originalBorderBrush = gradientBrush.Clone();
                }

                foreach (var stop in gradientBrush.GradientStops)
                {
                    Color targetColor;
                    if (isDark)
                    {
                        // 深色主题：将粉色调暗一点
                        targetColor = BlendColor(stop.Color, DarkTheme.Accent, 0.5);
                    }
                    else if (_originalBorderBrush != null)
                    {
                        // 浅色主题：恢复原始颜色
                        var originalStop = _originalBorderBrush.GradientStops
                            .FirstOrDefault(s => Math.Abs(s.Offset - stop.Offset) < 0.01);
                        targetColor = originalStop?.Color ?? stop.Color;
                    }
                    else
                    {
                        targetColor = stop.Color;
                    }
                    AnimateGradientStop(stop, targetColor, duration, easing);
                }
            }

            // 递归应用主题到所有子控件
            ApplyThemeToChildren(window, isDark, duration, easing);

            // 更新应用程序资源
            UpdateApplicationResources(isDark, duration);
        }

        /// <summary>
        /// 递归为所有子控件应用主题
        /// </summary>
        private void ApplyThemeToChildren(DependencyObject parent, bool isDark, TimeSpan duration, IEasingFunction easing)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ApplyThemeToControl(child, isDark, duration, easing);
                ApplyThemeToChildren(child, isDark, duration, easing);
            }
        }

        /// <summary>
        /// 为单个控件应用主题
        /// </summary>
        private void ApplyThemeToControl(DependencyObject element, bool isDark, TimeSpan duration, IEasingFunction easing)
        {
            var cardBg = isDark ? DarkTheme.CardBackground : LightTheme.CardBackground;
            var tabBg = isDark ? DarkTheme.TabBackground : LightTheme.TabBackground;
            var fg = isDark ? DarkTheme.Foreground : LightTheme.Foreground;
            var secondaryFg = isDark ? DarkTheme.SecondaryForeground : LightTheme.SecondaryForeground;
            var accent = isDark ? DarkTheme.Accent : LightTheme.Accent;

            // Border 背景 - 更宽松的匹配
            if (element is Border border)
            {
                // 检查是否有背景色需要切换
                if (border.Background is SolidColorBrush bgBrush)
                {
                    var color = bgBrush.Color;
                    // 白色/浅色背景（包括半透明）
                    bool isLight = color.R > 180 && color.G > 180 && color.B > 180 && color.A > 50;
                    // 深色背景
                    bool isDarkBg = color.R < 100 && color.G < 100 && color.B < 100 && color.A > 50;
                    if (isLight || isDarkBg)
                    {
                        AnimateBrushProperty(border, Border.BackgroundProperty, cardBg, duration, easing);
                    }
                }
            }

            // TabControl 背景
            if (element is TabControl tabControl)
            {
                if (IsThemeableBackground(tabControl.Background))
                {
                    AnimateBrushProperty(tabControl, Control.BackgroundProperty, tabBg, duration, easing);
                }
            }

            // TabItem - 强制应用前景色
            if (element is TabItem tabItem)
            {
                AnimateBrushProperty(tabItem, Control.ForegroundProperty, fg, duration, easing);
                // 同时设置 TextElement.Foreground 以确保 ContentPresenter 中的文字也变色
                tabItem.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(fg));
            }

            // ContentPresenter - 只对 TabItem 内部的应用主题色
            if (element is ContentPresenter presenter)
            {
                // 检查父控件是否是 TabItem
                var presenterParent = VisualTreeHelper.GetParent(presenter);
                bool isInTabItem = false;
                while (presenterParent != null)
                {
                    if (presenterParent is TabItem)
                    {
                        isInTabItem = true;
                        break;
                    }
                    if (presenterParent is Label parentLabel && IsAccentColor(parentLabel.Foreground))
                    {
                        // 如果在粉色 Label 内，不改变颜色
                        break;
                    }
                    presenterParent = VisualTreeHelper.GetParent(presenterParent);
                }
                
                if (isInTabItem)
                {
                    presenter.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(fg));
                }
            }

            // Label 前景色
            if (element is Label label)
            {
                if (IsAccentColor(label.Foreground))
                {
                    // 粉色标签保持粉色，只调整深浅
                    AnimateBrushProperty(label, Control.ForegroundProperty, accent, duration, easing);
                }
                else
                {
                    // 其他标签应用主题前景色
                    AnimateBrushProperty(label, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // TextBlock 前景色 - 强制应用（除了粉色和淡蓝色）
            if (element is TextBlock textBlock)
            {
                if (textBlock.Foreground is SolidColorBrush fgBrush)
                {
                    var color = fgBrush.Color;
                    // 粉色系保持不变
                    bool isPink = color.R > 200 && color.G < 150 && color.B > 150;
                    // 淡蓝色保持不变 (#80C8FF 等蓝色系)
                    bool isBlue = color.B > 200 && color.G > 150 && color.R < 150;
                    if (!isPink && !isBlue)
                    {
                        AnimateBrushProperty(textBlock, TextBlock.ForegroundProperty, fg, duration, easing);
                    }
                }
            }

            // CheckBox 前景色 - 强制应用
            if (element is CheckBox checkBox)
            {
                AnimateBrushProperty(checkBox, Control.ForegroundProperty, fg, duration, easing);
            }

            // RadioButton 前景色
            if (element is RadioButton radioButton)
            {
                if (IsThemeableForeground(radioButton.Foreground))
                {
                    AnimateBrushProperty(radioButton, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // Expander - 强制应用前景色和背景色
            if (element is Expander expander)
            {
                // 强制设置前景色
                AnimateBrushProperty(expander, Control.ForegroundProperty, fg, duration, easing);
                // 如果有背景，也设置背景
                if (IsThemeableBackground(expander.Background))
                {
                    AnimateBrushProperty(expander, Control.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // GroupBox
            if (element is GroupBox groupBox)
            {
                if (IsThemeableForeground(groupBox.Foreground))
                {
                    AnimateBrushProperty(groupBox, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // DataGrid
            if (element is DataGrid dataGrid)
            {
                if (IsThemeableBackground(dataGrid.Background))
                {
                    AnimateBrushProperty(dataGrid, Control.BackgroundProperty, cardBg, duration, easing);
                }
                if (IsThemeableForeground(dataGrid.Foreground))
                {
                    AnimateBrushProperty(dataGrid, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // ListBox
            if (element is ListBox listBox)
            {
                if (IsThemeableBackground(listBox.Background))
                {
                    AnimateBrushProperty(listBox, Control.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // ComboBox
            if (element is ComboBox comboBox)
            {
                if (IsThemeableForeground(comboBox.Foreground))
                {
                    AnimateBrushProperty(comboBox, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // TextBox
            if (element is TextBox textBox)
            {
                if (IsThemeableBackground(textBox.Background))
                {
                    AnimateBrushProperty(textBox, Control.BackgroundProperty, cardBg, duration, easing);
                }
                if (IsThemeableForeground(textBox.Foreground))
                {
                    AnimateBrushProperty(textBox, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // ScrollViewer
            if (element is ScrollViewer scrollViewer)
            {
                if (IsThemeableBackground(scrollViewer.Background))
                {
                    AnimateBrushProperty(scrollViewer, Control.BackgroundProperty,
                        isDark ? Color.FromArgb(0x00, 0x00, 0x00, 0x00) : Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),
                        duration, easing);
                }
            }

            // Grid 背景（如果有）
            if (element is Grid grid && grid.Background != null)
            {
                if (IsThemeableBackground(grid.Background))
                {
                    AnimateBrushProperty(grid, Panel.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // StackPanel 背景（如果有）
            if (element is StackPanel stackPanel && stackPanel.Background != null)
            {
                if (IsThemeableBackground(stackPanel.Background))
                {
                    AnimateBrushProperty(stackPanel, Panel.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // Rectangle (用于分隔线)
            if (element is System.Windows.Shapes.Rectangle rect)
            {
                if (IsAccentColor(rect.Fill))
                {
                    AnimateBrushProperty(rect, System.Windows.Shapes.Shape.FillProperty, accent, duration, easing);
                }
            }

            // HeaderedContentControl 基类（包括 Expander, GroupBox 等）
            if (element is HeaderedContentControl headered && !(element is TabItem))
            {
                if (IsThemeableForeground(headered.Foreground))
                {
                    AnimateBrushProperty(headered, Control.ForegroundProperty, fg, duration, easing);
                }
            }

            // Button - 强制应用前景色和背景色
            if (element is Button button)
            {
                // 前景色
                AnimateBrushProperty(button, Control.ForegroundProperty, fg, duration, easing);
                // 背景色
                if (IsThemeableBackground(button.Background))
                {
                    AnimateBrushProperty(button, Control.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // ToggleButton (包括 CheckBox 的内部按钮)
            if (element is ToggleButton toggleButton && !(element is CheckBox) && !(element is RadioButton))
            {
                AnimateBrushProperty(toggleButton, Control.ForegroundProperty, fg, duration, easing);
                if (IsThemeableBackground(toggleButton.Background))
                {
                    AnimateBrushProperty(toggleButton, Control.BackgroundProperty, cardBg, duration, easing);
                }
            }

            // RepeatButton
            if (element is RepeatButton repeatButton)
            {
                AnimateBrushProperty(repeatButton, Control.ForegroundProperty, fg, duration, easing);
            }

            // 通用 Control 处理 - 捕获其他未处理的控件
            if (element is Control control && 
                !(element is Button) && 
                !(element is CheckBox) && 
                !(element is RadioButton) && 
                !(element is TabItem) && 
                !(element is TabControl) &&
                !(element is Expander) &&
                !(element is Label) &&
                !(element is TextBox) &&
                !(element is ComboBox) &&
                !(element is ListBox) &&
                !(element is DataGrid) &&
                !(element is ScrollViewer) &&
                !(element is GroupBox))
            {
                // 前景色
                if (IsThemeableForeground(control.Foreground))
                {
                    AnimateBrushProperty(control, Control.ForegroundProperty, fg, duration, easing);
                }
                // 背景色
                if (IsThemeableBackground(control.Background))
                {
                    AnimateBrushProperty(control, Control.BackgroundProperty, cardBg, duration, easing);
                }
            }
        }

        /// <summary>
        /// 判断背景是否可以应用主题（白色/浅色 或 深色主题色）
        /// </summary>
        private bool IsThemeableBackground(Brush? brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                // 白色/浅色背景（包括半透明）
                if (color.R > 180 && color.G > 180 && color.B > 180 && color.A > 50)
                    return true;
                // 深色主题背景色
                if (color.R < 100 && color.G < 100 && color.B < 100 && color.A > 50)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断前景是否可以应用主题（深色文字 或 浅色文字）
        /// </summary>
        private bool IsThemeableForeground(Brush? brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                // 深色文字 (#333333, #282C34 等)
                if (color.R < 100 && color.G < 100 && color.B < 100)
                    return true;
                // 浅色文字（深色主题的文字 #E0E0E0, #B0B0B0 等）
                if (color.R > 160 && color.G > 160 && color.B > 160 && !IsAccentColor(brush))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断是否为强调色（粉色）
        /// </summary>
        private bool IsAccentColor(Brush? brush)
        {
            if (brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                // 检查是否为粉色系 (#FF69B4, #FFB6C1, #DB7093 等)
                return color.R > 200 && color.G < 150 && color.B > 150;
            }
            return false;
        }

        /// <summary>
        /// 为控件的 Brush 属性添加颜色动画
        /// </summary>
        private void AnimateBrushProperty(DependencyObject element, DependencyProperty property, Color targetColor, TimeSpan duration, IEasingFunction easing)
        {
            var currentBrush = element.GetValue(property) as Brush;
            
            if (currentBrush is SolidColorBrush solidBrush)
            {
                // 创建新的可动画画刷
                var newBrush = new SolidColorBrush(solidBrush.Color);
                element.SetValue(property, newBrush);
                AnimateColor(newBrush, targetColor, duration, easing);
            }
            else
            {
                // 直接设置新颜色
                element.SetValue(property, new SolidColorBrush(targetColor));
            }
        }

        /// <summary>
        /// 更新应用程序级别的资源
        /// </summary>
        private void UpdateApplicationResources(bool isDark, TimeSpan duration)
        {
            var app = Application.Current;
            if (app == null) return;

            // 切换 HandyControl 皮肤
            try
            {
                // 查找并替换 HandyControl 的 SkinDefault.xaml
                var skinUri = isDark
                    ? new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml")
                    : new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml");

                // 查找现有的 HandyControl 皮肤资源
                ResourceDictionary? existingSkin = null;
                int skinIndex = -1;
                for (int i = 0; i < app.Resources.MergedDictionaries.Count; i++)
                {
                    var dict = app.Resources.MergedDictionaries[i];
                    if (dict.Source != null && dict.Source.ToString().Contains("HandyControl") && 
                        (dict.Source.ToString().Contains("SkinDefault") || dict.Source.ToString().Contains("SkinDark")))
                    {
                        existingSkin = dict;
                        skinIndex = i;
                        break;
                    }
                }

                if (skinIndex >= 0)
                {
                    // 替换皮肤
                    app.Resources.MergedDictionaries[skinIndex] = new ResourceDictionary { Source = skinUri };
                }
            }
            catch
            {
                // 如果 HandyControl 深色皮肤不存在，忽略错误
            }

            // 更新或添加主题资源
            var themeDict = new ResourceDictionary();
            
            if (isDark)
            {
                themeDict["ThemeBackground"] = new SolidColorBrush(DarkTheme.CardBackground);
                themeDict["ThemeForeground"] = new SolidColorBrush(DarkTheme.Foreground);
                themeDict["ThemeSecondaryForeground"] = new SolidColorBrush(DarkTheme.SecondaryForeground);
                themeDict["ThemeAccent"] = new SolidColorBrush(DarkTheme.Accent);
                themeDict["ThemeBorder"] = new SolidColorBrush(DarkTheme.Border);
                
                // HandyControl 常用的资源键
                themeDict["PrimaryTextBrush"] = new SolidColorBrush(DarkTheme.Foreground);
                themeDict["SecondaryTextBrush"] = new SolidColorBrush(DarkTheme.SecondaryForeground);
                themeDict["RegionBrush"] = new SolidColorBrush(DarkTheme.CardBackground);
                themeDict["DefaultBrush"] = new SolidColorBrush(DarkTheme.CardBackground);
                themeDict["BorderBrush"] = new SolidColorBrush(DarkTheme.Border);
            }
            else
            {
                themeDict["ThemeBackground"] = new SolidColorBrush(LightTheme.CardBackground);
                themeDict["ThemeForeground"] = new SolidColorBrush(LightTheme.Foreground);
                themeDict["ThemeSecondaryForeground"] = new SolidColorBrush(LightTheme.SecondaryForeground);
                themeDict["ThemeAccent"] = new SolidColorBrush(LightTheme.Accent);
                themeDict["ThemeBorder"] = new SolidColorBrush(LightTheme.Border);
                
                // HandyControl 常用的资源键
                themeDict["PrimaryTextBrush"] = new SolidColorBrush(LightTheme.Foreground);
                themeDict["SecondaryTextBrush"] = new SolidColorBrush(LightTheme.SecondaryForeground);
                themeDict["RegionBrush"] = new SolidColorBrush(LightTheme.CardBackground);
                themeDict["DefaultBrush"] = new SolidColorBrush(LightTheme.CardBackground);
                themeDict["BorderBrush"] = new SolidColorBrush(LightTheme.Border);
            }

            // 查找并替换主题资源字典
            var existingThemeDict = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Contains("ThemeBackground"));
            
            if (existingThemeDict != null)
            {
                var index = app.Resources.MergedDictionaries.IndexOf(existingThemeDict);
                app.Resources.MergedDictionaries[index] = themeDict;
            }
            else
            {
                app.Resources.MergedDictionaries.Add(themeDict);
            }
        }

        /// <summary>
        /// 颜色动画
        /// </summary>
        private void AnimateColor(SolidColorBrush brush, Color targetColor, TimeSpan duration, IEasingFunction easing)
        {
            var animation = new ColorAnimation
            {
                To = targetColor,
                Duration = duration,
                EasingFunction = easing
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        /// <summary>
        /// 渐变停止点颜色动画
        /// </summary>
        private void AnimateGradientStop(GradientStop stop, Color targetColor, TimeSpan duration, IEasingFunction easing)
        {
            var animation = new ColorAnimation
            {
                To = targetColor,
                Duration = duration,
                EasingFunction = easing
            };
            stop.BeginAnimation(GradientStop.ColorProperty, animation);
        }

        /// <summary>
        /// 混合两个颜色
        /// </summary>
        private Color BlendColor(Color color1, Color color2, double ratio)
        {
            return Color.FromArgb(
                (byte)(color1.A * (1 - ratio) + color2.A * ratio),
                (byte)(color1.R * (1 - ratio) + color2.R * ratio),
                (byte)(color1.G * (1 - ratio) + color2.G * ratio),
                (byte)(color1.B * (1 - ratio) + color2.B * ratio)
            );
        }
    }
}
