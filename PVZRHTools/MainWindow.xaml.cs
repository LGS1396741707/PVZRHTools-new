using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FastHotKeyForWPF;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using ComboBox = HandyControl.Controls.ComboBox;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using Window = System.Windows.Window;
using Button = System.Windows.Controls.Button;
using TabControl = System.Windows.Controls.TabControl;
using TabItem = System.Windows.Controls.TabItem;
using Expander = System.Windows.Controls.Expander;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;
using ListBox = System.Windows.Controls.ListBox;
using ListBoxItem = System.Windows.Controls.ListBoxItem;
using Slider = System.Windows.Controls.Slider;
using ToolTip = System.Windows.Controls.ToolTip;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Interop;
using PVZRHTools.Animations;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>

namespace PVZRHTools
{
    public partial class MainWindow : Window
    {
        // Win32 API for window resizing
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLIENT = 1;
        private const int BORDER_WIDTH = 15;
        
        // 用于跟踪是否是首次激活（避免启动时重复播放动画）
        private bool _isFirstActivation = true;

        public MainWindow()
        {
            InitializeComponent();
            Title = $"PVZ融合版修改器{ModifierVersion.GameVersion}-{ModifierVersion.Version} B站@梧萱梦汐X 制作";
            WindowTitle.Content = Title;
            Instance = this;
            ModifierSprite = new ModifierSprite();
            Sprite.Show(ModifierSprite);
            ModifierSprite.Hide();
            if (File.Exists((App.IsBepInEx ? "BepInEx/config" : "UserData") + "/ModifierSettings.json"))
                try
                {
                    var s = JsonSerializer.Deserialize(
                        File.ReadAllText((App.IsBepInEx ? "BepInEx/config" : "UserData") + "/ModifierSettings.json"),
                        ModifierSaveModelSGC.Default.ModifierSaveModel);
                    DataContext = s.NeedSave ? new ModifierViewModel(s) : new ModifierViewModel(s.Hotkeys);
                }
                catch
                {
                    File.Delete((App.IsBepInEx ? "BepInEx/config" : "UserData") + "/ModifierSettings.json");
                    DataContext = new ModifierViewModel();
                }
            else
                DataContext = new ModifierViewModel();

            App.inited = true;
            
            // 窗口加载完成后播放启动动画
            Loaded += MainWindow_Loaded;
            
            // 窗口激活时播放过渡动画（从后台切回前台）
            Activated += MainWindow_Activated;
        }
        
        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            // 跳过首次激活（启动时已有启动动画）
            if (_isFirstActivation)
            {
                _isFirstActivation = false;
                return;
            }
            
            // 播放激活过渡动画
            WindowAnimations.PlayActivationAnimation(this);
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 播放 OS 风格启动动画
            WindowAnimations.PlayStartupAnimation(this);
            
            // 添加窗口状态变化动画（最小化/还原/最大化）
            WindowAnimations.AddWindowStateAnimation(this);
            
            // 添加窗口拖动倾斜效果
            AdvancedAnimations.AddWindowDragTilt(this);
            
            // 尝试启用 Windows 11 云母效果（如果可用）
            if (AcrylicHelper.IsWindows11OrNewer())
            {
                // 可选：启用云母或亚克力效果
                // AcrylicHelper.EnableMica(this);
            }
            
            // 为所有按钮添加交互动画
            ApplyAnimationsToControls(this);
        }
        
        /// <summary>
        /// 递归为所有控件应用动画效果
        /// </summary>
        private void ApplyAnimationsToControls(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // 为按钮添加点击动画
                if (child is Button button)
                {
                    ControlAnimations.AddButtonPressAnimation(button);
                    ControlAnimations.AddHoverGlow(button, Color.FromRgb(255, 105, 180)); // 粉色发光
                }
                
                // 为 TabControl 添加内容切换动画
                if (child is TabControl tabControl)
                {
                    ControlAnimations.AddTabControlAnimation(tabControl);
                    
                    // 为每个 TabItem 添加动画
                    foreach (var item in tabControl.Items)
                    {
                        if (item is TabItem tabItem)
                        {
                            ControlAnimations.AddTabItemAnimation(tabItem);
                        }
                    }
                }
                
                // 为 Expander 添加动画（礼盒修改、数值修改、场地特性等）
                if (child is Expander expander)
                {
                    ControlAnimations.AddExpanderAnimation(expander);
                }
                
                // 为 CheckBox 添加切换动画
                if (child is CheckBox checkBox)
                {
                    ControlAnimations.AddCheckBoxAnimation(checkBox);
                }
                
                // 为 ToggleButton 添加切换动画（排除 CheckBox）
                if (child is ToggleButton toggleButton && child is not CheckBox)
                {
                    ControlAnimations.AddToggleButtonAnimation(toggleButton);
                }
                
                // 为 TextBox 添加聚焦动画
                if (child is TextBox textBox)
                {
                    ControlAnimations.AddTextBoxFocusAnimation(textBox);
                }
                
                // 为 ComboBox 添加下拉动画
                if (child is System.Windows.Controls.ComboBox comboBox)
                {
                    ControlAnimations.AddComboBoxAnimation(comboBox);
                }
                
                // 为 Slider 添加滑动动画
                if (child is Slider slider)
                {
                    ControlAnimations.AddSliderAnimation(slider);
                }
                
                // 为 ListBox 的项添加悬停动画
                if (child is ListBox listBox)
                {
                    listBox.Loaded += (s, e) =>
                    {
                        foreach (var item in listBox.Items)
                        {
                            if (listBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem listBoxItem)
                            {
                                ControlAnimations.AddListItemHoverAnimation(listBoxItem);
                            }
                        }
                    };
                }
                
                // 为 DataGrid 行添加悬停动画
                if (child is DataGrid dataGrid)
                {
                    dataGrid.LoadingRow += (s, e) =>
                    {
                        ControlAnimations.AddDataGridRowAnimation(e.Row);
                    };
                }
                
                // 为 ContextMenu 添加弹出动画
                if (child is FrameworkElement fe && fe.ContextMenu != null)
                {
                    ControlAnimations.AddContextMenuAnimation(fe.ContextMenu);
                }
                
                // 为 ToolTip 添加淡入动画
                if (child is FrameworkElement element && element.ToolTip is ToolTip toolTip)
                {
                    ControlAnimations.AddToolTipAnimation(toolTip);
                }
                
                // 为 ProgressBar 添加流光效果
                if (child is ProgressBar progressBar)
                {
                    AdvancedAnimations.AddProgressBarShimmer(progressBar);
                }
                
                // 为重要按钮添加磁吸效果（可选，根据按钮名称判断）
                if (child is Button btn && btn.Name != null && 
                    (btn.Name.Contains("Important") || btn.Name.Contains("Main") || btn.Name.Contains("Primary")))
                {
                    AdvancedAnimations.AddMagneticEffect(btn, 0.1);
                }
                
                // 递归处理子元素
                ApplyAnimationsToControls(child);
            }
        }

        public static MainWindow? Instance { get; set; }
        public static ResourceDictionary LangEN_US => new() { Source = new Uri("/Lang.en-us.xaml", UriKind.Relative) };
        public static ResourceDictionary LangRU_RU => new() { Source = new Uri("/Lang.ru-ru.xaml", UriKind.Relative) };
        public static ResourceDictionary LangZH_CN => new() { Source = new Uri("/Lang.zh-cn.xaml", UriKind.Relative) };
        public ModifierSprite ModifierSprite { get; set; }
        public ModifierViewModel ViewModel => (ModifierViewModel)DataContext;

        public void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = false;
        }

        // 丝滑滚动 - 小幅度 + 平滑插值
        private readonly Dictionary<System.Windows.Controls.ScrollViewer, double> _targetOffsets = new();
        private System.Windows.Threading.DispatcherTimer? _scrollTimer;

        public void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 检查是否有任何 ComboBox 的下拉列表是打开的，如果有则忽略此事件
            // 方法1：检查鼠标是否在 Popup 中
            var source = e.OriginalSource as DependencyObject;
            if (source != null)
            {
                // 向上遍历可视化树，检查是否在 Popup 中
                var current = source;
                while (current != null)
                {
                    if (current is Popup popup && popup.IsOpen)
                    {
                        // 如果鼠标在打开的 Popup 中，不处理此事件，让 Popup 自己处理
                        return;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            
            // 方法2：检查整个窗口是否有打开的 ComboBox（更可靠的方法）
            if (HasOpenComboBox(this))
            {
                // 如果有打开的 ComboBox，不处理此事件
                return;
            }
            
            e.Handled = true;
            
            // 获取 ScrollViewer
            System.Windows.Controls.ScrollViewer? sv = null;
            if (sender is System.Windows.Controls.ScrollViewer scrollViewer)
                sv = scrollViewer;
            else if (sender is HandyControl.Controls.ScrollViewer hcScrollViewer)
                sv = hcScrollViewer;
            
            if (sv == null) return;

            // 初始化目标位置
            if (!_targetOffsets.ContainsKey(sv))
                _targetOffsets[sv] = sv.VerticalOffset;

            // 小幅度滚动：e.Delta 是 120，除以 4 = 30 像素
            double scrollAmount = e.Delta / 4.0;
            _targetOffsets[sv] -= scrollAmount;
            _targetOffsets[sv] = Math.Max(0, Math.Min(_targetOffsets[sv], sv.ScrollableHeight));

            // 启动平滑滚动定时器
            if (_scrollTimer == null)
            {
                _scrollTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                };
                _scrollTimer.Tick += ScrollTimer_Tick;
            }
            
            if (!_scrollTimer.IsEnabled)
                _scrollTimer.Start();
        }

        /// <summary>
        /// 检查窗口是否有打开的 ComboBox 下拉列表
        /// </summary>
        private static bool HasOpenComboBox(DependencyObject parent)
        {
            if (parent == null) return false;
            
            // 检查当前元素是否是 ComboBox 且下拉列表打开
            if (parent is System.Windows.Controls.ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                return true;
            }
            
            // 检查当前元素是否是 HandyControl 的 ComboBox 且下拉列表打开
            if (parent is HandyControl.Controls.ComboBox hcComboBox && hcComboBox.IsDropDownOpen)
            {
                return true;
            }
            
            // 递归检查子元素
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (HasOpenComboBox(child))
                {
                    return true;
                }
            }
            
            return false;
        }

        private void ScrollTimer_Tick(object? sender, EventArgs e)
        {
            bool anyActive = false;
            var toRemove = new List<System.Windows.Controls.ScrollViewer>();

            foreach (var kvp in _targetOffsets.ToList())
            {
                var sv = kvp.Key;
                var target = kvp.Value;
                var current = sv.VerticalOffset;
                var diff = target - current;

                if (Math.Abs(diff) > 0.5)
                {
                    // 平滑插值，每帧移动 20% 的距离
                    var newOffset = current + diff * 0.2;
                    sv.ScrollToVerticalOffset(newOffset);
                    anyActive = true;
                }
                else
                {
                    sv.ScrollToVerticalOffset(target);
                    toRemove.Add(sv);
                }
            }

            foreach (var sv in toRemove)
                _targetOffsets.Remove(sv);

            if (!anyActive)
                _scrollTimer?.Stop();
        }

        public void TitleBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton is MouseButtonState.Pressed && e.RightButton is MouseButtonState.Released &&
                e.MiddleButton is MouseButtonState.Released) DragMove();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            ViewModel.Save();
            GlobalHotKey.Destroy();
            Application.Current.Shutdown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlobalHotKey.Awake();
            foreach (var hvm in from hvm in ViewModel.Hotkeys where hvm.CurrentKeyB != Key.None select hvm)
                hvm.UpdateHotKey();
            
            // 添加窗口消息钩子以支持边框拖拽调整大小
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(WndProc);
        }

        /// <summary>
        /// 处理窗口消息，实现边框拖拽调整大小
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                handled = true;
                var result = GetHitTestResult(lParam);
                return new IntPtr(result);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 根据鼠标位置判断点击区域
        /// </summary>
        private int GetHitTestResult(IntPtr lParam)
        {
            // 获取鼠标屏幕坐标
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

            // 转换为窗口坐标
            var point = PointFromScreen(new Point(x, y));

            // 判断鼠标位置
            bool isLeft = point.X < BORDER_WIDTH;
            bool isRight = point.X > ActualWidth - BORDER_WIDTH;
            bool isTop = point.Y < BORDER_WIDTH;
            bool isBottom = point.Y > ActualHeight - BORDER_WIDTH;

            if (isTop && isLeft) return HTTOPLEFT;
            if (isTop && isRight) return HTTOPRIGHT;
            if (isBottom && isLeft) return HTBOTTOMLEFT;
            if (isBottom && isRight) return HTBOTTOMRIGHT;
            if (isLeft) return HTLEFT;
            if (isRight) return HTRIGHT;
            if (isTop) return HTTOP;
            if (isBottom) return HTBOTTOM;

            return HTCLIENT;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.inited && sender is ComboBox)
            {
                Application.Current.Resources.MergedDictionaries.RemoveAt(2);
                ResourceDictionary lang;
                if ((string?)((ComboBoxItem?)e.AddedItems[0]!).Content == "简体中文")
                    lang = LangZH_CN;
                else if ((string?)((ComboBoxItem?)e.AddedItems[0]!).Content == "English")
                    lang = LangEN_US;
                else
                    lang = LangRU_RU;

                Application.Current.Resources.MergedDictionaries.Add(lang);
                OnApplyTemplate();
            }
        }

        private bool _isUpdatingComboBox = false; // 标志：是否正在更新多选框（防止循环触发）

        private void FlagWaveBuffIdsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingComboBox)
            {
                System.Diagnostics.Debug.WriteLine($"[旗帜波词条] FlagWaveBuffIdsComboBox_SelectionChanged: 跳过（正在更新多选框）");
                return; // 如果正在更新多选框，跳过此事件
            }
            
            if (DataContext is ModifierViewModel vm && sender is HandyControl.Controls.CheckComboBox comboBox)
            {
                System.Diagnostics.Debug.WriteLine($"[旗帜波词条] FlagWaveBuffIdsComboBox_SelectionChanged: 多选框选择改变");
                var selectedItems = comboBox.SelectedItems;
                var ids = new List<int>();
                
                // 计算Advanced Buff的数量（InGameBuffs中不包含Debuff）
                int advancedCount = App.InitData.Value.AdvBuffs.Length; // Advanced Buff的数量
                int ultimateCount = App.InitData.Value.UltiBuffs.Length; // Ultimate Buff的数量
                int ultimateStartIndex = advancedCount; // Ultimate在InGameBuffs中的起始索引
                
                foreach (var item in selectedItems)
                {
                    if (item is TravelBuffVM buffVm)
                    {
                        int encodedId;
                        if (buffVm.TravelBuff.Debuff)
                        {
                            // Debuff: 编码ID = 2000 + OriginalId（游戏中的字典键）
                            encodedId = 2000 + buffVm.TravelBuff.OriginalId;
                        }
                        else if (buffVm.TravelBuff.Index >= ultimateStartIndex)
                        {
                            // Ultimate: 编码ID = 1000 + 数组索引（参考 HeiTa 的实现）
                            // 数组索引 = buffVm.TravelBuff.Index - ultimateStartIndex
                            // 例如：如果 ultimateStartIndex=85，buffVm.TravelBuff.Index=87，则数组索引=2，编码ID=1002
                            int arrayIndex = buffVm.TravelBuff.Index - ultimateStartIndex;
                            encodedId = 1000 + arrayIndex; // Ultimate: 编码ID = 1000 + 数组索引
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 下拉框选择Ultimate: Index={buffVm.TravelBuff.Index}, ultimateStartIndex={ultimateStartIndex}, 数组索引={arrayIndex}, 编码ID={encodedId}");
                        }
                        else
                        {
                            // Advanced: 编码ID = OriginalId（游戏中的字典键）
                            encodedId = buffVm.TravelBuff.OriginalId;
                        }
                        ids.Add(encodedId);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[旗帜波词条] FlagWaveBuffIdsComboBox_SelectionChanged: 准备设置 ids = [{string.Join(", ", ids)}]");
                vm.FlagWaveBuffIds = ids;
                System.Diagnostics.Debug.WriteLine($"[旗帜波词条] FlagWaveBuffIdsComboBox_SelectionChanged: 已设置 vm.FlagWaveBuffIds = [{string.Join(", ", vm.FlagWaveBuffIds)}]");
            }
        }

        private void FlagWaveBuffOrderApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ModifierViewModel vm) return;

            var text = FlagWaveBuffOrderIdsTextBox?.Text;
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ========== 按钮点击开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 输入文本: '{text}'");
            if (string.IsNullOrWhiteSpace(text))
            {
                vm.FlagWaveBuffIds = new List<int>();
                try
                {
                    if (FlagWaveBuffIdsComboBox != null) FlagWaveBuffIdsComboBox.SelectedItems?.Clear();
                }
                catch { }
                return;
            }

            // 支持：逗号/中文逗号/分号/空格/换行 分隔
            // 支持格式：A0, U1, D0（数字从0开始，A0=Advanced第0个词条）
            // 还支持：A:0, U:0, D:0 或编码ID（1000+为Ultimate，2000+为Debuff）
            var parts = text.Split(new[] { ',', '，', ';', '；', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var ids = new List<int>();
            int advancedCount = App.InitData.Value.AdvBuffs.Length; // Advanced Buff的数量
            int ultimateCount = App.InitData.Value.UltiBuffs.Length; // Ultimate Buff的数量
            int ultimateStartIndex = advancedCount; // Ultimate在InGameBuffs中的起始索引
            
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 解析开始 - advancedCount={advancedCount}, ultimateCount={ultimateCount}, ultimateStartIndex={ultimateStartIndex}, InGameBuffs.Count={vm.InGameBuffs?.Count ?? 0}");
            
            foreach (var p in parts)
            {
                var part = p.Trim();
                int id = -1;
                
                System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 开始解析输入: '{part}' (长度={part.Length}, 第一个字符='{(part.Length > 0 ? part[0] : '?')}')");
                
                // 支持格式：A0, U1, D0（数字从0开始，使用OriginalId）
                if (part.Length >= 2 && (part[0] == 'A' || part[0] == 'a'))
                {
                    if (int.TryParse(part.Substring(1), out var advNum) && advNum >= 0)
                    {
                        // 找到第 advNum 个Advanced词条，使用其OriginalId
                        // Advanced词条在 InGameBuffs 的前 advancedCount 个位置
                        if (vm.InGameBuffs != null && advNum < advancedCount && advNum < vm.InGameBuffs.Count)
                        {
                            var buff = vm.InGameBuffs[advNum];
                            if (buff.TravelBuff.Index < ultimateStartIndex) // 确保是Advanced词条
                            {
                                id = buff.TravelBuff.OriginalId; // 使用OriginalId（游戏中的字典键）
                                System.Diagnostics.Debug.WriteLine($"解析 A{advNum}: 找到词条 Index={buff.TravelBuff.Index}, OriginalId={buff.TravelBuff.OriginalId}, 编码ID={id}");
                            }
                        }
                    }
                }
                else if (part.Length >= 2 && (part[0] == 'U' || part[0] == 'u'))
                {
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 检测到 U/u 开头，准备解析: part='{part}', 子串='{part.Substring(1)}'");
                    if (int.TryParse(part.Substring(1), out var ultNum) && ultNum >= 0)
                    {
                        // Ultimate词条：使用 ultNum 作为数组索引（而不是字典键）
                        // ultimateUpgrades 数组的索引对应 InGameBuffs 中 Ultimate 词条的顺序（从0开始）
                        // 所以 U2 应该编码为 1000 + 2 = 1002，解码后使用 2 作为 ultimateUpgrades[2] 的索引
                        // 注意：即使 ultNum 超出范围，也强制编码为 1000 + ultNum，让游戏端处理范围检查
                        id = 1000 + ultNum; // Ultimate: 编码ID = 1000 + ultNum（数组索引）
                        System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ✓ 解析 U{ultNum} 成功: ultNum={ultNum}, ultimateCount={ultimateCount}, 编码ID={id} (1000+ultNum)");
                        if (ultNum >= ultimateCount)
                        {
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 警告: U{ultNum} 超出范围 (ultimateCount={ultimateCount})，但已编码为 {id}，将由游戏端处理");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ✗ 解析 U 开头词条失败: 无法解析数字部分 '{part.Substring(1)}'");
                    }
                }
                else if (part.Length >= 2 && (part[0] == 'D' || part[0] == 'd'))
                {
                    if (int.TryParse(part.Substring(1), out var debNum) && debNum >= 0)
                    {
                        // 找到第 debNum 个Debuff词条，使用其OriginalId
                        if (vm.InGameDebuffs != null && debNum < vm.InGameDebuffs.Count)
                        {
                            var buff = vm.InGameDebuffs[debNum];
                            id = 2000 + buff.TravelBuff.OriginalId; // 使用OriginalId（游戏中的字典键）
                            System.Diagnostics.Debug.WriteLine($"解析 D{debNum}: 找到词条 Index={buff.TravelBuff.Index}, OriginalId={buff.TravelBuff.OriginalId}, 编码ID={id}");
                        }
                    }
                }
                // 支持旧格式：A:0, U:0, D:0
                else if (part.StartsWith("A:", StringComparison.OrdinalIgnoreCase) || part.StartsWith("A：", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(part.Substring(2), out var advId))
                        id = advId; // Advanced: 0-999
                }
                else if (part.StartsWith("U:", StringComparison.OrdinalIgnoreCase) || part.StartsWith("U：", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(part.Substring(2), out var ultId))
                        id = 1000 + ultId; // Ultimate: 1000-1999
                }
                else if (part.StartsWith("D:", StringComparison.OrdinalIgnoreCase) || part.StartsWith("D：", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(part.Substring(2), out var debId))
                        id = 2000 + debId; // Debuff: 2000-2999
                }
                // 支持直接输入编码ID（但排除以 U/u/A/a/D/d 开头的字符串，避免误解析）
                else if (!part.StartsWith("U", StringComparison.OrdinalIgnoreCase) &&
                         !part.StartsWith("A", StringComparison.OrdinalIgnoreCase) &&
                         !part.StartsWith("D", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(part, out var encodedId))
                {
                    id = encodedId;
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 直接解析为编码ID: {id} (来自输入: '{part}')");
                }
                
                if (id >= 0)
                {
                    ids.Add(id);
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ✓ 添加编码ID: {id} (来自输入: '{part}')");
                    
                    // 验证：如果是 U 开头的输入，编码ID应该是 1000+
                    if ((part.StartsWith("U", StringComparison.OrdinalIgnoreCase) || part.StartsWith("U:", StringComparison.OrdinalIgnoreCase) || part.StartsWith("U：", StringComparison.OrdinalIgnoreCase)) && id < 1000)
                    {
                        System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ✗✗✗ 严重错误: Ultimate词条 '{part}' 的编码ID应该是1000+，但实际是{id}！这会导致被错误地解码为Advanced词条！");
                        System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 调试信息: part='{part}', part.Length={part.Length}, part[0]='{(part.Length > 0 ? part[0] : '?')}'");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ✗ 警告: 无法解析输入 '{part}'，跳过 (id={id})");
                }
            }

            // 覆盖顺序：按输入顺序直接作为"每旗解锁"的顺序
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] ========== 解析完成 ==========");
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 最终 ids 列表: [{string.Join(", ", ids)}]");
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 准备设置 vm.FlagWaveBuffIds = [{string.Join(", ", ids)}]");
            vm.FlagWaveBuffIds = ids;
            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 已设置 vm.FlagWaveBuffIds，当前值: [{string.Join(", ", vm.FlagWaveBuffIds)}]");

            // 同步到多选框（仅用于可视化勾选；多选框本身不保证顺序）
            try
            {
                if (FlagWaveBuffIdsComboBox != null)
                {
                    _isUpdatingComboBox = true; // 设置标志，防止触发 SelectionChanged 事件
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 开始同步到多选框，设置 _isUpdatingComboBox = true");
                    FlagWaveBuffIdsComboBox.SelectedItems?.Clear();
                    
                    // 使用外部作用域中已定义的 ultimateStartIndex
                    foreach (var encodedId in ids)
                    {
                        System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 同步到多选框: 处理编码ID={encodedId}");
                        
                        bool found = false;
                        
                        if (encodedId >= 2000)
                        {
                            // Debuff: 解码为游戏中的原始ID（字典键）
                            int gameOriginalId = encodedId - 2000;
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] Debuff: 解码为 gameOriginalId={gameOriginalId}");
                            
                            // 在FlagWaveBuffIdsComboBox中查找对应的项（使用OriginalId匹配）
                            foreach (var comboItem in FlagWaveBuffIdsComboBox.Items)
                            {
                                if (comboItem is TravelBuffVM comboBuffVm && 
                                    comboBuffVm.TravelBuff.OriginalId == gameOriginalId &&
                                    comboBuffVm.TravelBuff.Debuff == true)
                                {
                                    FlagWaveBuffIdsComboBox.SelectedItems?.Add(comboItem);
                                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 找到Debuff项: Index={comboBuffVm.TravelBuff.Index}, OriginalId={comboBuffVm.TravelBuff.OriginalId}, Text={comboBuffVm.TravelBuff.Text}");
                                    found = true;
                                    break;
                                }
                            }
                        }
                        else if (encodedId >= 1000)
                        {
                            // Ultimate: 解码为数组索引
                            int arrayIndex = encodedId - 1000;
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] Ultimate: 解码为数组索引={arrayIndex}, ultimateStartIndex={ultimateStartIndex}");
                            
                            // 在FlagWaveBuffIdsComboBox中查找对应的项（使用数组索引匹配）
                            // Ultimate词条的Index = ultimateStartIndex + arrayIndex
                            int targetIndex = ultimateStartIndex + arrayIndex;
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] Ultimate: 查找 Index={targetIndex} 的项");
                            
                            foreach (var comboItem in FlagWaveBuffIdsComboBox.Items)
                            {
                                if (comboItem is TravelBuffVM comboBuffVm && 
                                    !comboBuffVm.TravelBuff.Debuff &&
                                    comboBuffVm.TravelBuff.Index == targetIndex)
                                {
                                    FlagWaveBuffIdsComboBox.SelectedItems?.Add(comboItem);
                                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 找到Ultimate项: Index={comboBuffVm.TravelBuff.Index}, OriginalId={comboBuffVm.TravelBuff.OriginalId}, Text={comboBuffVm.TravelBuff.Text}");
                                    found = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Advanced: 解码为游戏中的原始ID（字典键）
                            int gameOriginalId = encodedId;
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] Advanced: 解码为 gameOriginalId={gameOriginalId}");
                            
                            // 在FlagWaveBuffIdsComboBox中查找对应的项（使用OriginalId匹配）
                            foreach (var comboItem in FlagWaveBuffIdsComboBox.Items)
                            {
                                if (comboItem is TravelBuffVM comboBuffVm && 
                                    !comboBuffVm.TravelBuff.Debuff &&
                                    comboBuffVm.TravelBuff.OriginalId == gameOriginalId &&
                                    comboBuffVm.TravelBuff.Index < ultimateStartIndex) // 确保是Advanced词条
                                {
                                    FlagWaveBuffIdsComboBox.SelectedItems?.Add(comboItem);
                                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 找到Advanced项: Index={comboBuffVm.TravelBuff.Index}, OriginalId={comboBuffVm.TravelBuff.OriginalId}, Text={comboBuffVm.TravelBuff.Text}");
                                    found = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!found)
                        {
                            System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 警告: 未找到编码ID={encodedId} 对应的多选框项");
                        }
                    }
                    _isUpdatingComboBox = false; // 清除标志
                    System.Diagnostics.Debug.WriteLine($"[旗帜波词条] 多选框同步完成，设置 _isUpdatingComboBox = false");
                }
            }
            catch 
            { 
                _isUpdatingComboBox = false; // 确保在异常情况下也清除标志
            }
        }

        private void LockWheatPlant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}