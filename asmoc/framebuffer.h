// Simple software framebuffer API built on top of raylib
// Provides a CPU-side pixel buffer you can manipulate and then upload to a GPU texture
// for drawing each frame.

#ifndef FRAMEBUFFER_H
#define FRAMEBUFFER_H

#include "raylib.h"
#include <stdbool.h>

typedef struct Framebuffer {
    int width;
    int height;
    Color *pixels;       // CPU pixel buffer (RGBA 32-bit)
    Texture2D texture;   // GPU texture that mirrors the CPU buffer
    bool dirty;          // Marked when pixels changed and need upload
} Framebuffer;

// Create a framebuffer with given dimensions. Returns a fully initialized object.
// Note: Call fb_destroy when done.
Framebuffer fb_create(int width, int height);

// Free resources owned by the framebuffer.
void fb_destroy(Framebuffer *fb);

// Clear entire framebuffer to a color (marks dirty flag).
void fb_clear(Framebuffer *fb, Color color);

// Put a pixel with bounds checking (marks dirty if written).
void fb_putpixel(Framebuffer *fb, int x, int y, Color color);

// Draw a filled circle (solid) into the framebuffer.
void fb_draw_filled_circle(Framebuffer *fb, int cx, int cy, int radius, Color color);

// Upload to GPU (if dirty) and draw to screen at (x,y) scaled by 'scale'.
// If scale == 1.0f an unscaled draw is used; otherwise a scaled blit is performed.
void fb_present(Framebuffer *fb, int x, int y, float scale, Color tint);

#endif // FRAMEBUFFER_H
