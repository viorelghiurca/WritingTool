using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace WritingTool.Services
{
    /// <summary>
    /// Provides smooth animations for WinUI3 elements using Composition API.
    /// </summary>
    public static class AnimationService
    {
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan FastDuration = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan SlowDuration = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// Gets the center point of an element for scale animations.
        /// </summary>
        private static Vector3 GetCenterPoint(UIElement element)
        {
            if (element is FrameworkElement fe)
            {
                return new Vector3((float)fe.ActualWidth / 2, (float)fe.ActualHeight / 2, 0);
            }
            return Vector3.Zero;
        }

        /// <summary>
        /// Fades in an element with optional scale animation.
        /// </summary>
        public static void FadeIn(UIElement element, TimeSpan? duration = null, bool withScale = false)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;
                var actualDuration = duration ?? DefaultDuration;

                // Opacity animation
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 0f);
                opacityAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                opacityAnimation.Duration = actualDuration;

                if (withScale)
                {
                    // Scale animation - start slightly smaller
                    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnimation.InsertKeyFrame(0f, new Vector3(0.95f, 0.95f, 1f));
                    scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                    scaleAnimation.Duration = actualDuration;

                    // Set center point for scale
                    visual.CenterPoint = GetCenterPoint(element);

                    visual.StartAnimation("Scale", scaleAnimation);
                }

                visual.StartAnimation("Opacity", opacityAnimation);
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Fades out an element with optional scale animation.
        /// </summary>
        public static void FadeOut(UIElement element, TimeSpan? duration = null, bool withScale = false, Action? onComplete = null)
        {
            var actualDuration = duration ?? FastDuration;
            
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;

                // Opacity animation
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 1f);
                opacityAnimation.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                opacityAnimation.Duration = actualDuration;

                if (withScale)
                {
                    // Scale animation - shrink slightly
                    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
                    scaleAnimation.InsertKeyFrame(1f, new Vector3(0.95f, 0.95f, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                    scaleAnimation.Duration = actualDuration;

                    visual.CenterPoint = GetCenterPoint(element);

                    visual.StartAnimation("Scale", scaleAnimation);
                }

                visual.StartAnimation("Opacity", opacityAnimation);
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }

            if (onComplete != null)
            {
                // Execute callback after animation completes
                Task.Delay(actualDuration).ContinueWith(_ => onComplete(), TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        /// <summary>
        /// Slides an element in from a direction with fade.
        /// </summary>
        public static void SlideIn(UIElement element, SlideDirection direction = SlideDirection.Up, TimeSpan? duration = null)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;
                var actualDuration = duration ?? DefaultDuration;

                // Calculate offset based on direction
                var startOffset = direction switch
                {
                    SlideDirection.Up => new Vector3(0, 30, 0),
                    SlideDirection.Down => new Vector3(0, -30, 0),
                    SlideDirection.Left => new Vector3(30, 0, 0),
                    SlideDirection.Right => new Vector3(-30, 0, 0),
                    _ => new Vector3(0, 30, 0)
                };

                // Offset animation
                var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.InsertKeyFrame(0f, startOffset);
                offsetAnimation.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                offsetAnimation.Duration = actualDuration;

                // Opacity animation
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 0f);
                opacityAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                opacityAnimation.Duration = actualDuration;

                visual.StartAnimation("Offset", offsetAnimation);
                visual.StartAnimation("Opacity", opacityAnimation);
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Slides an element out with fade.
        /// </summary>
        public static void SlideOut(UIElement element, SlideDirection direction = SlideDirection.Down, TimeSpan? duration = null, Action? onComplete = null)
        {
            var actualDuration = duration ?? FastDuration;
            
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;

                // Calculate offset based on direction
                var endOffset = direction switch
                {
                    SlideDirection.Up => new Vector3(0, -30, 0),
                    SlideDirection.Down => new Vector3(0, 30, 0),
                    SlideDirection.Left => new Vector3(-30, 0, 0),
                    SlideDirection.Right => new Vector3(30, 0, 0),
                    _ => new Vector3(0, 30, 0)
                };

                // Offset animation
                var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.InsertKeyFrame(0f, Vector3.Zero);
                offsetAnimation.InsertKeyFrame(1f, endOffset, compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                offsetAnimation.Duration = actualDuration;

                // Opacity animation
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 1f);
                opacityAnimation.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                opacityAnimation.Duration = actualDuration;

                visual.StartAnimation("Offset", offsetAnimation);
                visual.StartAnimation("Opacity", opacityAnimation);
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }

            if (onComplete != null)
            {
                Task.Delay(actualDuration).ContinueWith(_ => onComplete(), TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        /// <summary>
        /// Animates staggered entrance for a collection of elements.
        /// </summary>
        public static void StaggeredEntrance(UIElement[] elements, TimeSpan? staggerDelay = null, TimeSpan? duration = null)
        {
            if (elements == null || elements.Length == 0) return;
            
            var delay = staggerDelay ?? TimeSpan.FromMilliseconds(50);
            var actualDuration = duration ?? DefaultDuration;

            for (int i = 0; i < elements.Length; i++)
            {
                try
                {
                    var element = elements[i];
                    if (element == null) continue;
                    
                    var visual = ElementCompositionPreview.GetElementVisual(element);
                    if (visual?.Compositor == null) continue;
                    
                    var compositor = visual.Compositor;

                    // Set initial state
                    visual.Opacity = 0;
                    visual.Offset = new Vector3(0, 20, 0);

                    // Create delayed animation
                    var itemDelay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * i);

                    // Offset animation with delay
                    var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                    offsetAnimation.InsertKeyFrame(0f, new Vector3(0, 20, 0));
                    offsetAnimation.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                    offsetAnimation.Duration = actualDuration;
                    offsetAnimation.DelayTime = itemDelay;
                    offsetAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                    // Opacity animation with delay
                    var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.InsertKeyFrame(0f, 0f);
                    opacityAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                    opacityAnimation.Duration = actualDuration;
                    opacityAnimation.DelayTime = itemDelay;
                    opacityAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

                    visual.StartAnimation("Opacity", opacityAnimation);
                    visual.StartAnimation("Offset", offsetAnimation);
                }
                catch (AccessViolationException) { }
                catch (InvalidOperationException) { }
            }
        }

        /// <summary>
        /// Applies a subtle scale animation on hover (pointer over).
        /// </summary>
        public static void SetupHoverAnimation(UIElement element, float hoverScale = 1.02f)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;

                element.PointerEntered += (s, e) =>
                {
                    try
                    {
                        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                        scaleAnimation.InsertKeyFrame(1f, new Vector3(hoverScale, hoverScale, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                        scaleAnimation.Duration = FastDuration;

                        visual.CenterPoint = GetCenterPoint(element);
                        visual.StartAnimation("Scale", scaleAnimation);
                    }
                    catch { }
                };

                element.PointerExited += (s, e) =>
                {
                    try
                    {
                        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                        scaleAnimation.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                        scaleAnimation.Duration = FastDuration;

                        visual.StartAnimation("Scale", scaleAnimation);
                    }
                    catch { }
                };
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Creates a press animation effect for buttons.
        /// </summary>
        public static void SetupPressAnimation(UIElement element, float pressScale = 0.97f)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;

                element.PointerPressed += (s, e) =>
                {
                    try
                    {
                        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                        scaleAnimation.InsertKeyFrame(1f, new Vector3(pressScale, pressScale, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
                        scaleAnimation.Duration = TimeSpan.FromMilliseconds(100);

                        visual.CenterPoint = GetCenterPoint(element);
                        visual.StartAnimation("Scale", scaleAnimation);
                    }
                    catch { }
                };

                element.PointerReleased += (s, e) =>
                {
                    try
                    {
                        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                        scaleAnimation.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                        scaleAnimation.Duration = FastDuration;

                        visual.StartAnimation("Scale", scaleAnimation);
                    }
                    catch { }
                };
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Spring animation for bouncy effects.
        /// </summary>
        public static void SpringScale(UIElement element, float targetScale = 1f, float dampingRatio = 0.6f)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual?.Compositor == null) return;
                
                var compositor = visual.Compositor;

                var springAnimation = compositor.CreateSpringVector3Animation();
                springAnimation.FinalValue = new Vector3(targetScale, targetScale, 1f);
                springAnimation.DampingRatio = dampingRatio;
                springAnimation.Period = TimeSpan.FromMilliseconds(50);

                visual.CenterPoint = GetCenterPoint(element);
                visual.StartAnimation("Scale", springAnimation);
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Resets element visual properties to default state.
        /// </summary>
        public static void ResetVisual(UIElement element)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual == null) return;
                
                visual.Opacity = 1;
                visual.Offset = Vector3.Zero;
                visual.Scale = Vector3.One;
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Prepares an element for animation by setting initial hidden state.
        /// </summary>
        public static void PrepareForAnimation(UIElement element)
        {
            try
            {
                if (element == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(element);
                if (visual == null) return;
                
                visual.Opacity = 0;
            }
            catch (AccessViolationException) { }
            catch (InvalidOperationException) { }
        }
    }

    /// <summary>
    /// Direction for slide animations.
    /// </summary>
    public enum SlideDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
