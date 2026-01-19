using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using HandyControl.Controls;
using ToolModData;

namespace PVZRHTools;

public class SelectedItemsExt : DependencyObject
{
    // Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(SelectedItemsExt),
            new PropertyMetadata(OnSelectedItemsChanged));

    public static IList GetSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SelectedItemsProperty);
    }

    public static void OnlistBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var dataSource = GetSelectedItems((sender as DependencyObject)!);
        foreach (var item in e.AddedItems) dataSource.Add(item);
        foreach (var item in e.RemovedItems) dataSource.Remove(item);
        SetSelectedItems((sender as DependencyObject)!, dataSource);
    }

    public static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListBox listBox && listBox.SelectionMode is SelectionMode.Multiple)
        {
            if (e.OldValue is not null) listBox.SelectionChanged -= OnlistBoxSelectionChanged;
            var collection = (e.NewValue as IList)!;
            listBox.SelectedItems.Clear();
            if (collection is not null)
            {
                foreach (var item in collection) listBox.SelectedItems.Add(item);
                listBox.OnApplyTemplate();
                listBox.SelectionChanged += OnlistBoxSelectionChanged;
            }
        }
    }

    public static void SetSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(BasicProperties))]
internal partial class BasicPropertiesSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Exit))]
internal partial class ExitSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GameModes))]
internal partial class GameModesSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(InGameActions))]
internal partial class InGameActionsSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(InGameHotkeys))]
internal partial class InGameHotkeysSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(InitData))]
internal partial class InitDataSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ISyncData))]
internal partial class ISyncDataSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModifierSaveModel))]
internal partial class ModifierSaveModelSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SyncAll))]
internal partial class SyncAllSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SyncTravelBuff))]
internal partial class SyncTravelBuffSGC : JsonSerializerContext

{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ValueProperties))]
internal partial class ValuePropertiesSGC : JsonSerializerContext

{
}

/// <summary>
/// ComboBox 中文搜索支持扩展
/// </summary>
public class ComboBoxChineseSearchExt : DependencyObject
{
    private static readonly Dictionary<object, CollectionViewSource> _viewSources = new();
    private static readonly Dictionary<object, object?> _originalSources = new();
    private static readonly Dictionary<object, System.Windows.Threading.DispatcherTimer> _debounceTimers = new();

    /// <summary>
    /// 启用中文搜索的附加属性
    /// </summary>
    public static readonly DependencyProperty EnableChineseSearchProperty =
        DependencyProperty.RegisterAttached("EnableChineseSearch", typeof(bool), typeof(ComboBoxChineseSearchExt),
            new PropertyMetadata(false, OnEnableChineseSearchChanged));

    public static bool GetEnableChineseSearch(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableChineseSearchProperty);
    }

    public static void SetEnableChineseSearch(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableChineseSearchProperty, value);
    }

    private static void OnEnableChineseSearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 支持 HandyControl 和标准 WPF ComboBox
        if (d is not (HandyControl.Controls.ComboBox or System.Windows.Controls.ComboBox)) return;

        if ((bool)e.NewValue)
        {
            // 启用中文搜索
            if (d is FrameworkElement fe)
            {
                fe.Loaded += ComboBox_Loaded;
            }
            
            // 对于可编辑的 ComboBox，监听内部 TextBox 的 TextChanged 事件
            if (d is HandyControl.Controls.ComboBox hcComboBox)
            {
                if (hcComboBox.IsLoaded)
                {
                    InitializeChineseSearch(hcComboBox);
                    AttachTextBoxListener(hcComboBox);
                }
                else
                {
                    hcComboBox.Loaded += (s, args) =>
                    {
                        InitializeChineseSearch(hcComboBox);
                        AttachTextBoxListener(hcComboBox);
                    };
                }
            }
            else if (d is System.Windows.Controls.ComboBox wpfComboBox)
            {
                if (wpfComboBox.IsLoaded)
                {
                    InitializeChineseSearch(wpfComboBox);
                    AttachTextBoxListener(wpfComboBox);
                }
                else
                {
                    wpfComboBox.Loaded += (s, args) =>
                    {
                        InitializeChineseSearch(wpfComboBox);
                        AttachTextBoxListener(wpfComboBox);
                    };
                }
            }
        }
        else
        {
            // 禁用中文搜索
            if (d is FrameworkElement fe)
            {
                fe.Loaded -= ComboBox_Loaded;
            }
            
            // 清理资源
            if (_viewSources.TryGetValue(d, out var viewSource))
            {
                viewSource.View.Filter = null;
                _viewSources.Remove(d);
            }
            
            // 清理防抖定时器
            if (_debounceTimers.TryGetValue(d, out var timer))
            {
                timer.Stop();
                timer.Tick -= (s, e) => { }; // 移除事件处理器
                _debounceTimers.Remove(d);
            }
            
            // 恢复原始 ItemsSource
            if (_originalSources.TryGetValue(d, out var originalSource))
            {
                if (d is HandyControl.Controls.ComboBox hc)
                {
                    hc.ItemsSource = originalSource as IEnumerable;
                }
                else if (d is System.Windows.Controls.ComboBox wpf)
                {
                    wpf.ItemsSource = originalSource as IEnumerable;
                }
                _originalSources.Remove(d);
            }
        }
    }

    private static void AttachTextBoxListener(object comboBox)
    {
        // 查找 ComboBox 内部的 TextBox
        if (comboBox is not FrameworkElement fe) return;

        fe.Dispatcher.BeginInvoke(new System.Action(() =>
        {
            var textBox = FindVisualChild<System.Windows.Controls.TextBox>(fe);
            if (textBox != null)
            {
                // 创建防抖定时器，优化性能
                var debounceTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100) // 100ms 防抖延迟
                };
                
                System.Action? pendingAction = null;
                debounceTimer.Tick += (s, args) =>
                {
                    debounceTimer.Stop();
                    pendingAction?.Invoke();
                    pendingAction = null;
                };
                
                _debounceTimers[comboBox] = debounceTimer;
                
                // 处理选择项后避免全选文字的问题
                if (comboBox is HandyControl.Controls.ComboBox hcComboBox)
                {
                    hcComboBox.SelectionChanged += (s, e) =>
                    {
                        // 延迟处理，确保 Text 已更新
                        hcComboBox.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            try
                            {
                                var innerTextBox = FindVisualChild<System.Windows.Controls.TextBox>(hcComboBox);
                                if (innerTextBox != null && innerTextBox.SelectionLength > 0)
                                {
                                    // 如果文字被全选，将光标移到末尾
                                    innerTextBox.SelectionStart = innerTextBox.Text?.Length ?? 0;
                                    innerTextBox.SelectionLength = 0;
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    };
                    
                    hcComboBox.DropDownClosed += (s, e) =>
                    {
                        // 下拉列表关闭后，将光标移到文字末尾
                        hcComboBox.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            try
                            {
                                var innerTextBox = FindVisualChild<System.Windows.Controls.TextBox>(hcComboBox);
                                if (innerTextBox != null)
                                {
                                    innerTextBox.SelectionStart = innerTextBox.Text?.Length ?? 0;
                                    innerTextBox.SelectionLength = 0;
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    };
                }
                else if (comboBox is System.Windows.Controls.ComboBox wpfComboBox)
                {
                    wpfComboBox.SelectionChanged += (s, e) =>
                    {
                        // 延迟处理，确保 Text 已更新
                        wpfComboBox.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            try
                            {
                                var innerTextBox = FindVisualChild<System.Windows.Controls.TextBox>(wpfComboBox);
                                if (innerTextBox != null && innerTextBox.SelectionLength > 0)
                                {
                                    // 如果文字被全选，将光标移到末尾
                                    innerTextBox.SelectionStart = innerTextBox.Text?.Length ?? 0;
                                    innerTextBox.SelectionLength = 0;
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    };
                    
                    wpfComboBox.DropDownClosed += (s, e) =>
                    {
                        // 下拉列表关闭后，将光标移到文字末尾
                        wpfComboBox.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            try
                            {
                                var innerTextBox = FindVisualChild<System.Windows.Controls.TextBox>(wpfComboBox);
                                if (innerTextBox != null)
                                {
                                    innerTextBox.SelectionStart = innerTextBox.Text?.Length ?? 0;
                                    innerTextBox.SelectionLength = 0;
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    };
                }
                
                textBox.TextChanged += (sender, e) =>
                {
                    // 停止之前的定时器
                    debounceTimer.Stop();
                    
                    // 创建新的待执行操作
                    pendingAction = () =>
                    {
                        if (!_viewSources.TryGetValue(comboBox, out var viewSource)) return;
                        
                        // 获取当前文本
                        string? currentText = null;
                        if (comboBox is HandyControl.Controls.ComboBox hc)
                        {
                            currentText = hc.Text;
                        }
                        else if (comboBox is System.Windows.Controls.ComboBox wpf)
                        {
                            currentText = wpf.Text;
                        }
                        
                        // 刷新视图（这是唯一需要立即执行的操作）
                        viewSource.View.Refresh();
                        
                        // 延迟处理 UI 更新，避免阻塞
                        if (comboBox is FrameworkElement fe2)
                        {
                            fe2.Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                try
                                {
                                    bool hasText = !string.IsNullOrWhiteSpace(currentText);
                                    
                                    // 处理下拉列表的打开/关闭
                                    if (comboBox is HandyControl.Controls.ComboBox hc2)
                                    {
                                        if (hasText && !hc2.IsDropDownOpen)
                                        {
                                            hc2.IsDropDownOpen = true;
                                        }
                                        else if (!hasText && hc2.IsDropDownOpen)
                                        {
                                            hc2.IsDropDownOpen = false;
                                        }
                                    }
                                    else if (comboBox is System.Windows.Controls.ComboBox wpf2)
                                    {
                                        if (hasText && !wpf2.IsDropDownOpen)
                                        {
                                            wpf2.IsDropDownOpen = true;
                                        }
                                        else if (!hasText && wpf2.IsDropDownOpen)
                                        {
                                            wpf2.IsDropDownOpen = false;
                                        }
                                    }
                                }
                                catch
                                {
                                    // 忽略错误
                                }
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        }
                    };
                    
                    // 启动防抖定时器
                    debounceTimer.Start();
                };
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }

    private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is HandyControl.Controls.ComboBox hcComboBox)
        {
            InitializeChineseSearch(hcComboBox);
        }
        else if (sender is System.Windows.Controls.ComboBox wpfComboBox)
        {
            InitializeChineseSearch(wpfComboBox);
        }
    }

    private static void InitializeChineseSearch(object comboBox)
    {
        // 如果已经有 ViewSource，先清理
        if (_viewSources.ContainsKey(comboBox))
        {
            _viewSources.Remove(comboBox);
        }

        // 获取原始 ItemsSource
        object? currentItemsSource = null;
        IEnumerable? originalSource = null;
        string? displayMemberPath = null;
        
        if (comboBox is HandyControl.Controls.ComboBox hcComboBox)
        {
            currentItemsSource = hcComboBox.ItemsSource;
            displayMemberPath = hcComboBox.DisplayMemberPath;
        }
        else if (comboBox is System.Windows.Controls.ComboBox wpfComboBox)
        {
            currentItemsSource = wpfComboBox.ItemsSource;
            displayMemberPath = wpfComboBox.DisplayMemberPath;
        }

        if (currentItemsSource == null) return;

        // 如果 ItemsSource 已经是 CollectionView，说明之前已经初始化过
        // 尝试从保存的原始源中获取
        if (currentItemsSource is ICollectionView)
        {
            if (_originalSources.TryGetValue(comboBox, out var savedSource) && savedSource != null)
            {
                originalSource = savedSource as IEnumerable;
            }
            else
            {
                // 如果找不到保存的源，说明这是第一次初始化但 ItemsSource 已经是 View
                // 这种情况下，需要重新获取原始绑定源
                // 但由于无法获取，只能跳过或者使用当前源
                // 为了避免错误，直接返回，不进行初始化
                return;
            }
        }
        else
        {
            // 保存原始源（只在第一次初始化时保存）
            originalSource = currentItemsSource as IEnumerable;
            if (!_originalSources.ContainsKey(comboBox))
            {
                _originalSources[comboBox] = originalSource;
            }
        }

        if (originalSource == null) return;

        // 检查 originalSource 是否是 CollectionView，如果是则不能设置为 Source
        if (originalSource is ICollectionView)
        {
            // 如果原始源已经是 CollectionView，无法创建新的 CollectionViewSource
            // 这种情况下，无法实现过滤功能，直接返回
            return;
        }

        // 创建 CollectionViewSource 来支持过滤
        // Dictionary<int, string> 已经实现了 IEnumerable<KeyValuePair<int, string>>，可以直接使用
        var viewSource = new CollectionViewSource();
        viewSource.Source = originalSource;
        
        // 保存 displayMemberPath 以便在过滤时使用
        var filterContext = new { DisplayMemberPath = displayMemberPath };
        
        viewSource.Filter += (sender, e) =>
        {
            string? currentText = null;
            if (comboBox is HandyControl.Controls.ComboBox hc)
            {
                currentText = hc.Text;
            }
            else if (comboBox is System.Windows.Controls.ComboBox wpf)
            {
                currentText = wpf.Text;
            }

            if (string.IsNullOrWhiteSpace(currentText))
            {
                e.Accepted = true;
                return;
            }

            var searchText = currentText.Trim();
            e.Accepted = FilterItem(e.Item, searchText, filterContext.DisplayMemberPath);
        };

        // 保存 ViewSource 引用
        _viewSources[comboBox] = viewSource;

        // 保存当前的 SelectedValue，以便在设置新的 ItemsSource 后恢复
        object? currentSelectedValue = null;
        if (comboBox is HandyControl.Controls.ComboBox hc)
        {
            currentSelectedValue = hc.SelectedValue;
            // 使用 SetCurrentValue 来设置 ItemsSource，这样不会触发验证错误
            hc.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, DependencyProperty.UnsetValue);
            hc.ItemsSource = viewSource.View;
            // 初始化时清空 Text，确保框框内为空
            hc.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                try
                {
                    // 如果 SelectedValue 为空，清空 Text
                    if (hc.SelectedValue == null)
                    {
                        hc.Text = string.Empty;
                    }
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            // 恢复 SelectedValue（如果存在且在新视图中）
            if (currentSelectedValue != null)
            {
                // 延迟恢复，确保视图已更新
                hc.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    try
                    {
                        // 检查该项是否在视图中
                        var view = viewSource.View;
                        bool found = false;
                        foreach (var item in view)
                        {
                            if (item is KeyValuePair<int, string> kvp && kvp.Key.Equals(currentSelectedValue))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            // 使用 SetCurrentValue 来设置，避免触发验证错误
                            hc.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, currentSelectedValue);
                        }
                    }
                    catch
                    {
                        // 如果无法恢复，忽略错误
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
        else if (comboBox is System.Windows.Controls.ComboBox wpf)
        {
            currentSelectedValue = wpf.SelectedValue;
            // 使用 SetCurrentValue 来设置 ItemsSource，这样不会触发验证错误
            wpf.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, DependencyProperty.UnsetValue);
            wpf.ItemsSource = viewSource.View;
            // 初始化时清空 Text，确保框框内为空
            wpf.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                try
                {
                    // 如果 SelectedValue 为空，清空 Text
                    if (wpf.SelectedValue == null)
                    {
                        wpf.Text = string.Empty;
                    }
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            // 恢复 SelectedValue（如果存在且在新视图中）
            if (currentSelectedValue != null)
            {
                // 延迟恢复，确保视图已更新
                wpf.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    try
                    {
                        // 检查该项是否在视图中
                        var view = viewSource.View;
                        bool found = false;
                        foreach (var item in view)
                        {
                            if (item is KeyValuePair<int, string> kvp && kvp.Key.Equals(currentSelectedValue))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            // 使用 SetCurrentValue 来设置，避免触发验证错误
                            wpf.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, currentSelectedValue);
                        }
                    }
                    catch
                    {
                        // 如果无法恢复，忽略错误
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }


    private static bool FilterItem(object item, string searchText, string? displayMemberPath)
    {
        if (item == null) return false;

        string? displayText = null;

        // 如果指定了 DisplayMemberPath，使用反射获取值
        if (!string.IsNullOrEmpty(displayMemberPath))
        {
            try
            {
                var property = item.GetType().GetProperty(displayMemberPath);
                if (property != null)
                {
                    displayText = property.GetValue(item)?.ToString();
                }
            }
            catch
            {
                // 如果反射失败，尝试直接转换为字符串
                displayText = item.ToString();
            }
        }
        else
        {
            displayText = item.ToString();
        }

        if (string.IsNullOrEmpty(displayText)) return false;

        // 支持按 ID（数字）和中文名称搜索
        // 检查是否包含搜索文本（不区分大小写）
        if (displayText.Contains(searchText, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 如果是 KeyValuePair，也检查 Key（ID）
        if (item is KeyValuePair<int, string> kvp)
        {
            // 检查 ID 是否匹配
            if (int.TryParse(searchText, out int searchId) && kvp.Key == searchId)
            {
                return true;
            }
        }

        return false;
    }
}