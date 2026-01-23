using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics;

namespace WritingTool.Services
{
    /// <summary>
    /// Provides smooth animated window resize and transition effects.
    /// </summary>
    public class WindowAnimationHelper
    {
        private readonly AppWindow _appWindow;
        private readonly UIElement _rootElement;
        private readonly DispatcherQueue _dispatcherQueue;
        private bool _isAnimating;

        private const int AnimationSteps = 12;
        private const int StepDelayMs = 16; // ~60fps

        public WindowAnimationHelper(AppWindow appWindow, UIElement rootElement, DispatcherQueue dispatcherQueue)
        {
            _appWindow = appWindow;
            _rootElement = rootElement;
            _dispatcherQueue = dispatcherQueue;
        }

        /// <summary>
        /// Smoothly resizes the window with content fade animation.
        /// </summary>
        public async Task AnimateResizeAsync(int targetWidth, int targetHeight, bool fadeContent = true)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                var currentSize = _appWindow.Size;
                var startWidth = currentSize.Width;
                var startHeight = currentSize.Height;

                if (fadeContent && _rootElement != null)
                {
                    try
                    {
                        var visual = ElementCompositionPreview.GetElementVisual(_rootElement);
                        if (visual?.Compositor != null)
                        {
                            var compositor = visual.Compositor;
                            
                            // Fade out content slightly during resize
                            var fadeOutAnimation = compositor.CreateScalarKeyFrameAnimation();
                            fadeOutAnimation.InsertKeyFrame(0f, 1f);
                            fadeOutAnimation.InsertKeyFrame(0.3f, 0.7f);
                            fadeOutAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                            fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(AnimationSteps * StepDelayMs);
                            visual.StartAnimation("Opacity", fadeOutAnimation);

                            // Subtle scale for smooth feel
                            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                            scaleAnimation.InsertKeyFrame(0f, Vector3.One);
                            scaleAnimation.InsertKeyFrame(0.3f, new Vector3(0.98f, 0.98f, 1f));
                            scaleAnimation.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                            scaleAnimation.Duration = TimeSpan.FromMilliseconds(AnimationSteps * StepDelayMs);

                            visual.CenterPoint = new Vector3((float)startWidth / 2, (float)startHeight / 2, 0);
                            visual.StartAnimation("Scale", scaleAnimation);
                        }
                    }
                    catch (AccessViolationException) { }
                    catch (InvalidOperationException) { }
                }

                // Animate window size using easing
                for (int i = 1; i <= AnimationSteps; i++)
                {
                    var progress = EaseOutCubic((float)i / AnimationSteps);

                    var newWidth = (int)(startWidth + (targetWidth - startWidth) * progress);
                    var newHeight = (int)(startHeight + (targetHeight - startHeight) * progress);

                    _appWindow.Resize(new SizeInt32(newWidth, newHeight));

                    await Task.Delay(StepDelayMs);
                }

                // Ensure final size is exact
                _appWindow.Resize(new SizeInt32(targetWidth, targetHeight));
            }
            finally
            {
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Animates window position change.
        /// </summary>
        public async Task AnimateMoveAsync(int targetX, int targetY)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                var currentPos = _appWindow.Position;
                var startX = currentPos.X;
                var startY = currentPos.Y;

                for (int i = 1; i <= AnimationSteps; i++)
                {
                    var progress = EaseOutCubic((float)i / AnimationSteps);

                    var newX = (int)(startX + (targetX - startX) * progress);
                    var newY = (int)(startY + (targetY - startY) * progress);

                    _appWindow.Move(new PointInt32(newX, newY));

                    await Task.Delay(StepDelayMs);
                }

                _appWindow.Move(new PointInt32(targetX, targetY));
            }
            finally
            {
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Animates both resize and move together.
        /// </summary>
        public async Task AnimateResizeAndMoveAsync(int targetWidth, int targetHeight, int targetX, int targetY, bool fadeContent = true)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                var currentSize = _appWindow.Size;
                var currentPos = _appWindow.Position;
                var startWidth = currentSize.Width;
                var startHeight = currentSize.Height;
                var startX = currentPos.X;
                var startY = currentPos.Y;

                if (fadeContent && _rootElement != null)
                {
                    try
                    {
                        var visual = ElementCompositionPreview.GetElementVisual(_rootElement);
                        if (visual?.Compositor != null)
                        {
                            var compositor = visual.Compositor;
                            
                            // Content animation during transition
                            var fadeAnimation = compositor.CreateScalarKeyFrameAnimation();
                            fadeAnimation.InsertKeyFrame(0f, 1f);
                            fadeAnimation.InsertKeyFrame(0.2f, 0.8f);
                            fadeAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                            fadeAnimation.Duration = TimeSpan.FromMilliseconds(AnimationSteps * StepDelayMs);
                            visual.StartAnimation("Opacity", fadeAnimation);
                        }
                    }
                    catch (AccessViolationException) { }
                    catch (InvalidOperationException) { }
                }

                for (int i = 1; i <= AnimationSteps; i++)
                {
                    var progress = EaseOutCubic((float)i / AnimationSteps);

                    var newWidth = (int)(startWidth + (targetWidth - startWidth) * progress);
                    var newHeight = (int)(startHeight + (targetHeight - startHeight) * progress);
                    var newX = (int)(startX + (targetX - startX) * progress);
                    var newY = (int)(startY + (targetY - startY) * progress);

                    _appWindow.MoveAndResize(new RectInt32(newX, newY, newWidth, newHeight));

                    await Task.Delay(StepDelayMs);
                }

                _appWindow.MoveAndResize(new RectInt32(targetX, targetY, targetWidth, targetHeight));
            }
            finally
            {
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Shows window with entrance animation.
        /// </summary>
        public void AnimateWindowEntrance()
        {
            try
            {
                if (_rootElement == null) return;
                
                var visual = ElementCompositionPreview.GetElementVisual(_rootElement);
                if (visual == null) return;
                
                var compositor = visual.Compositor;
                if (compositor == null) return;

                // Start from slightly scaled down and transparent
                visual.Opacity = 0;
                visual.Scale = new Vector3(0.96f, 0.96f, 1f);

                // Animate to full
                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 0f);
                opacityAnimation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);

                var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(0f, new Vector3(0.96f, 0.96f, 1f));
                scaleAnimation.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
                scaleAnimation.Duration = TimeSpan.FromMilliseconds(200);

                if (_rootElement is FrameworkElement fe)
                {
                    visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2, (float)fe.ActualHeight / 2, 0);
                }

                visual.StartAnimation("Opacity", opacityAnimation);
                visual.StartAnimation("Scale", scaleAnimation);
            }
            catch (AccessViolationException)
            {
                // Window in invalid state, skip animation
            }
            catch (InvalidOperationException)
            {
                // Element not loaded, skip animation
            }
        }

        /// <summary>
        /// Animate window exit before hiding.
        /// </summary>
        public async Task AnimateWindowExitAsync()
        {
            try
            {
                if (_rootElement == null)
                {
                    await Task.Delay(50);
                    return;
                }
                
                var visual = ElementCompositionPreview.GetElementVisual(_rootElement);
                if (visual == null)
                {
                    await Task.Delay(50);
                    return;
                }
                
                var compositor = visual.Compositor;
                if (compositor == null)
                {
                    await Task.Delay(50);
                    return;
                }

                var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
                opacityAnimation.InsertKeyFrame(0f, 1f);
                opacityAnimation.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(150);

                var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(0f, Vector3.One);
                scaleAnimation.InsertKeyFrame(1f, new Vector3(0.96f, 0.96f, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f)));
                scaleAnimation.Duration = TimeSpan.FromMilliseconds(150);

                if (_rootElement is FrameworkElement fe)
                {
                    visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2, (float)fe.ActualHeight / 2, 0);
                }

                visual.StartAnimation("Opacity", opacityAnimation);
                visual.StartAnimation("Scale", scaleAnimation);

                await Task.Delay(150);

                // Reset for next show
                visual.Opacity = 1;
                visual.Scale = Vector3.One;
            }
            catch (AccessViolationException)
            {
                // Window in invalid state, skip animation
            }
            catch (InvalidOperationException)
            {
                // Element not loaded, skip animation
            }
        }

        private static float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);

        private static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
    }
}
