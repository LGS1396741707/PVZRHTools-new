using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PVZRHTools.Animations
{
    /// <summary>
    /// Windows 11 亚克力/云母效果辅助类
    /// </summary>
    public static class AcrylicHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_MICA_EFFECT = 1029;

        /// <summary>
        /// 启用 Windows 11 云母效果
        /// </summary>
        public static void EnableMica(Window window, bool useDarkMode = false)
        {
            if (!IsWindows11OrNewer()) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 设置深色/浅色模式
            int darkMode = useDarkMode ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // 启用云母效果 (2 = Mica, 3 = Acrylic, 4 = Tabbed)
            int backdropType = 2;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            // 扩展框架到客户区
            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // 设置透明背景
            window.Background = Brushes.Transparent;
        }

        /// <summary>
        /// 启用亚克力效果
        /// </summary>
        public static void EnableAcrylic(Window window)
        {
            if (!IsWindows11OrNewer()) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int backdropType = 3; // Acrylic
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }

        /// <summary>
        /// 检查是否为 Windows 11 或更新版本
        /// </summary>
        public static bool IsWindows11OrNewer()
        {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10 && version.Build >= 22000;
        }

        /// <summary>
        /// 创建模拟亚克力背景画刷（兼容旧系统）
        /// </summary>
        public static Brush CreateFallbackAcrylicBrush(Color tintColor, double tintOpacity = 0.7)
        {
            return new SolidColorBrush(Color.FromArgb(
                (byte)(tintOpacity * 255),
                tintColor.R,
                tintColor.G,
                tintColor.B));
        }
    }
}
