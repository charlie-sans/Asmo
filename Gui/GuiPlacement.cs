using Asmo.Gfx;

namespace Asmo.Gui
{
    /// <summary>
    /// Provides common placement zones for GUI elements using OpenGL coordinates (bottom-left origin)
    /// </summary>
    public static class GuiPlacement
    {
        public static class Center
        {
            /// <summary>
            /// Get position for center of screen
            /// </summary>
            public static (int x, int y) Get(Surface surface) 
                => (surface.Width / 2, surface.Height / 2);

            /// <summary>
            /// Get position for center of screen with offsets (alias for Get with offsets)
            /// </summary>
            public static (int x, int y) Middle(Surface surface, int offsetX = 0, int offsetY = 0) 
                => (surface.Width / 2 + offsetX, surface.Height / 2 + offsetY);

            /// <summary>
            /// Get position for center-top area
            /// </summary>
            public static (int x, int y) Top(Surface surface, int offsetFromTop = 50) 
                => (surface.Width / 2, surface.Height - offsetFromTop);

            /// <summary>
            /// Get position for center-bottom area
            /// </summary>
            public static (int x, int y) Bottom(Surface surface, int offsetFromBottom = 50) 
                => (surface.Width / 2, offsetFromBottom);
        }

        public static class Left
        {
            /// <summary>
            /// Get position for left-center area
            /// </summary>
            public static (int x, int y) Center(Surface surface, int offsetFromLeft = 50) 
                => (offsetFromLeft, surface.Height / 2);

            /// <summary>
            /// Get position for left-top area
            /// </summary>
            public static (int x, int y) Top(Surface surface, int offsetFromLeft = 50, int offsetFromTop = 50) 
                => (offsetFromLeft, surface.Height - offsetFromTop);

            /// <summary>
            /// Get position for left-bottom area
            /// </summary>
            public static (int x, int y) Bottom(Surface surface, int offsetFromLeft = 50, int offsetFromBottom = 50) 
                => (offsetFromLeft, offsetFromBottom);
        }

        public static class Right
        {
            /// <summary>
            /// Get position for right-center area
            /// </summary>
            public static (int x, int y) Center(Surface surface, int offsetFromRight = 50) 
                => (surface.Width - offsetFromRight, surface.Height / 2);

            /// <summary>
            /// Get position for right-top area
            /// </summary>
            public static (int x, int y) Top(Surface surface, int offsetFromRight = 50, int offsetFromTop = 50) 
                => (surface.Width - offsetFromRight, surface.Height - offsetFromTop);

            /// <summary>
            /// Get position for right-bottom area
            /// </summary>
            public static (int x, int y) Bottom(Surface surface, int offsetFromRight = 50, int offsetFromBottom = 50) 
                => (surface.Width - offsetFromRight, offsetFromBottom);
        }

        public static class Top
        {
            /// <summary>
            /// Get position for top-center area
            /// </summary>
            public static (int x, int y) Center(Surface surface, int offsetFromTop = 50) 
                => (surface.Width / 2, surface.Height - offsetFromTop);

            /// <summary>
            /// Get position for top-left area
            /// </summary>
            public static (int x, int y) Left(Surface surface, int offsetFromLeft = 50, int offsetFromTop = 50) 
                => (offsetFromLeft, surface.Height - offsetFromTop);

            /// <summary>
            /// Get position for top-right area
            /// </summary>
            public static (int x, int y) Right(Surface surface, int offsetFromRight = 50, int offsetFromTop = 50) 
                => (surface.Width - offsetFromRight, surface.Height - offsetFromTop);
        }

        public static class Bottom
        {
            /// <summary>
            /// Get position for bottom-center area
            /// </summary>
            public static (int x, int y) Center(Surface surface, int offsetFromBottom = 50) 
                => (surface.Width / 2, offsetFromBottom);

            /// <summary>
            /// Get position for bottom-left area
            /// </summary>
            public static (int x, int y) Left(Surface surface, int offsetFromLeft = 50, int offsetFromBottom = 50) 
                => (offsetFromLeft, offsetFromBottom);

            /// <summary>
            /// Get position for bottom-right area
            /// </summary>
            public static (int x, int y) Right(Surface surface, int offsetFromRight = 50, int offsetFromBottom = 50) 
                => (surface.Width - offsetFromRight, offsetFromBottom);
        }

        /// <summary>
        /// Custom position with offsets from edges
        /// </summary>
        public static (int x, int y) Custom(Surface surface, int? fromLeft = null, int? fromRight = null, 
                                           int? fromTop = null, int? fromBottom = null)
        {
            int x = 0, y = 0;

            // Calculate X position
            if (fromLeft.HasValue)
                x = fromLeft.Value;
            else if (fromRight.HasValue)
                x = surface.Width - fromRight.Value;
            else
                x = surface.Width / 2; // Default to center

            // Calculate Y position (OpenGL coordinates)
            if (fromBottom.HasValue)
                y = fromBottom.Value;
            else if (fromTop.HasValue)
                y = surface.Height - fromTop.Value;
            else
                y = surface.Height / 2; // Default to center

            return (x, y);
        }
    }
}