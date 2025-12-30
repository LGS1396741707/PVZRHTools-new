using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace PVZRHTools.Animations
{
    /// <summary>
    /// 控件交互动画 - 高级 OS 风格
    /// </summary>
    public static class ControlAnimations
    {
        /// <summary>
        /// 为按钮添加点击缩放动画
        /// </summary>
        public static void AddButtonPressAnimation(Button button)
        {
            if (button.RenderTransform is not ScaleTransform)
            {
                button.RenderTransform = new ScaleTransform(1, 1);
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            button.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var scale = (ScaleTransform)button.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, 
                    CreateQuickAnimation(0.95, 80));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, 
                    CreateQuickAnimation(0.95, 80));
            };

            button.PreviewMouseLeftButtonUp += (s, e) =>
            {
                var scale = (ScaleTransform)button.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, 
                    CreateSpringAnimation(1.0, 150));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, 
                    CreateSpringAnimation(1.0, 150));
            };

            button.MouseLeave += (s, e) =>
            {
                var scale = (ScaleTransform)button.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, 
                    CreateQuickAnimation(1.0, 100));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, 
                    CreateQuickAnimation(1.0, 100));
            };
        }

        /// <summary>
        /// 为元素添加悬停发光效果
        /// </summary>
        public static void AddHoverGlow(UIElement element, Color glowColor)
        {
            var dropShadow = new DropShadowEffect
            {
                Color = glowColor,
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0
            };
            
            if (element is FrameworkElement fe)
                fe.Effect = dropShadow;

            element.MouseEnter += (s, e) =>
            {
                dropShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                    CreateQuickAnimation(15, 200));
                dropShadow.BeginAnimation(DropShadowEffect.OpacityProperty,
                    CreateQuickAnimation(0.6, 200));
            };

            element.MouseLeave += (s, e) =>
            {
                dropShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                    CreateQuickAnimation(0, 200));
                dropShadow.BeginAnimation(DropShadowEffect.OpacityProperty,
                    CreateQuickAnimation(0, 200));
            };
        }

        /// <summary>
        /// 为元素添加悬停上浮效果
        /// </summary>
        public static void AddHoverLift(UIElement element)
        {
            if (element.RenderTransform is not TranslateTransform)
            {
                element.RenderTransform = new TranslateTransform(0, 0);
            }

            element.MouseEnter += (s, e) =>
            {
                var translate = (TranslateTransform)element.RenderTransform;
                translate.BeginAnimation(TranslateTransform.YProperty,
                    CreateSpringAnimation(-3, 200));
            };

            element.MouseLeave += (s, e) =>
            {
                var translate = (TranslateTransform)element.RenderTransform;
                translate.BeginAnimation(TranslateTransform.YProperty,
                    CreateSpringAnimation(0, 200));
            };
        }

        /// <summary>
        /// TabItem 切换动画
        /// </summary>
        public static void AnimateTabSwitch(FrameworkElement content)
        {
            content.Opacity = 0;
            var translate = new TranslateTransform(20, 0);
            content.RenderTransform = translate;

            var fadeIn = CreateQuickAnimation(1, 250);
            var slideIn = CreateSpringAnimation(0, 300);

            content.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            translate.BeginAnimation(TranslateTransform.XProperty, slideIn);
        }

        /// <summary>
        /// 列表项入场动画
        /// </summary>
        public static void AnimateListItemEntrance(FrameworkElement item, int index)
        {
            item.Opacity = 0;
            var translate = new TranslateTransform(0, 20);
            item.RenderTransform = translate;

            var delay = TimeSpan.FromMilliseconds(index * 30);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            item.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            translate.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }

        /// <summary>
        /// 成功反馈动画（绿色闪烁）
        /// </summary>
        public static void PlaySuccessFeedback(FrameworkElement element)
        {
            var originalBackground = (element as Control)?.Background;
            
            var colorAnim = new ColorAnimation
            {
                To = Color.FromArgb(60, 76, 175, 80),
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true
            };

            if (element is Control control && control.Background is SolidColorBrush brush)
            {
                var animBrush = new SolidColorBrush(brush.Color);
                control.Background = animBrush;
                animBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            }
        }

        /// <summary>
        /// 错误抖动动画
        /// </summary>
        public static void PlayErrorShake(FrameworkElement element)
        {
            var translate = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
            element.RenderTransform = translate;

            var shake = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(400)
            };
            
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromPercent(0.1)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(8, KeyTime.FromPercent(0.3)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-6, KeyTime.FromPercent(0.5)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(6, KeyTime.FromPercent(0.7)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-2, KeyTime.FromPercent(0.9)));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));

            translate.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private static DoubleAnimation CreateQuickAnimation(double to, int durationMs)
        {
            return new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
        }

        private static DoubleAnimation CreateSpringAnimation(double to, int durationMs)
        {
            return new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new ElasticEase 
                { 
                    EasingMode = EasingMode.EaseOut,
                    Oscillations = 1,
                    Springiness = 8
                }
            };
        }
    }
}
