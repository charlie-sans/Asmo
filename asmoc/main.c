// Demo of a simple software framebuffer API built on top of raylib.
// See framebuffer.h / framebuffer.c for the implementation.
#include "raylib.h"
#include "framebuffer.h"
#include <math.h>

//------------------------------------------------------------------------------------
// Program main entry point
//------------------------------------------------------------------------------------
int main(void)
{
    // Initialization
    //--------------------------------------------------------------------------------------
    const int screenWidth = 800;
    const int screenHeight = 450;

    InitWindow(screenWidth, screenHeight, "Software Framebuffer API Demo");

    SetTargetFPS(60);               // Set our game to run at 60 frames-per-second
    //--------------------------------------------------------------------------------------

    // Create a low-resolution software framebuffer we can scale up.
    Framebuffer fb = fb_create(320, 180);
    if (!fb.pixels) {
        CloseWindow();
        return 1; // allocation failed
    }

    int frame = 0;
    // Main loop
    while (!WindowShouldClose())    // Detect window close button or ESC key
    {
        // Update
        //----------------------------------------------------------------------------------
        frame++;
        // Example animation parameters
        float t = frame * 0.03f;
        int cx = (int)(fb.width / 2 + (fb.width / 3) * sinf(t * 0.9f));
        int cy = (int)(fb.height / 2 + (fb.height / 4) * cosf(t * 1.3f));
        int radius = 20 + (int)(10 * (sinf(t * 1.7f) * 0.5f + 0.5f));

        // Draw into software framebuffer
        fb_clear(&fb, (Color){20, 20, 28, 255});

        // A moving color gradient background (direct pixel writes)
        for (int y = 0; y < fb.height; ++y) {
            for (int x = 0; x < fb.width; ++x) {
                unsigned char r = (unsigned char)((x + frame) & 0xFF);
                unsigned char g = (unsigned char)((y * 2 + frame * 3) & 0xFF);
                unsigned char b = (unsigned char)(((x ^ y) + frame * 2) & 0xFF);
                // Darken the gradient slightly for contrast
                fb_putpixel(&fb, x, y, (Color){(unsigned char)(r * 3 / 5), (unsigned char)(g * 3 / 5), (unsigned char)(b * 3 / 5), 255});
            }
        }

        // Draw a filled circle on top
        fb_draw_filled_circle(&fb, cx, cy, radius, (Color){255, 220, 80, 255});
        fb_draw_filled_circle(&fb, cx - radius/2, cy - radius/2, radius/3, (Color){80, 200, 255, 255});
        //----------------------------------------------------------------------------------

        // Draw
        //----------------------------------------------------------------------------------
        BeginDrawing();

            ClearBackground((Color){10, 10, 14, 255});

            // Compute integer scale to fit while preserving aspect
            int scaleX = screenWidth / fb.width;
            int scaleY = screenHeight / fb.height;
            int scale = scaleX < scaleY ? scaleX : scaleY;
            if (scale < 1) scale = 1;
            int drawW = fb.width * scale;
            int drawH = fb.height * scale;
            int posX = (screenWidth - drawW) / 2;
            int posY = (screenHeight - drawH) / 2;

            fb_present(&fb, posX, posY, (float)scale, WHITE);

            DrawText("Software framebuffer demo (CPU -> texture -> screen)", 10, 10, 18, RAYWHITE);
            DrawText("Gradient + animated circles written pixel-by-pixel", 10, 32, 14, GRAY);
            DrawFPS(10, screenHeight - 30);

        EndDrawing();
        //----------------------------------------------------------------------------------
    }

    // De-Initialization
    //--------------------------------------------------------------------------------------
    fb_destroy(&fb);
    CloseWindow();        // Close window and OpenGL context
    //--------------------------------------------------------------------------------------

    return 0;
}