// framebuffer.c - implementation of a simple software framebuffer using raylib

#include "framebuffer.h"
#include <stdlib.h>
#include <string.h>
#include <math.h>

Framebuffer fb_create(int width, int height) {
    Framebuffer fb = {0};
    fb.width = width;
    fb.height = height;
    size_t count = (size_t)width * (size_t)height;
    fb.pixels = (Color*)malloc(count * sizeof(Color));
    if (!fb.pixels) {
        TraceLog(LOG_ERROR, "Framebuffer allocation failed (%dx%d)", width, height);
        return fb; // Returns with pixels NULL -> invalid, caller can check
    }

    // Initialize to transparent black
    memset(fb.pixels, 0, count * sizeof(Color));

    // Create a texture that mirrors our CPU buffer
    Image img = {0};
    img.data = fb.pixels;           // raylib will copy data into GPU on LoadTextureFromImage
    img.width = width;
    img.height = height;
    img.mipmaps = 1;
    img.format = PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;
    fb.texture = LoadTextureFromImage(img);
    fb.dirty = true;
    return fb;
}

void fb_destroy(Framebuffer *fb) {
    if (!fb) return;
    if (fb->texture.id > 0) UnloadTexture(fb->texture);
    if (fb->pixels) free(fb->pixels);
    fb->pixels = NULL;
    fb->texture.id = 0;
}

void fb_clear(Framebuffer *fb, Color color) {
    if (!fb || !fb->pixels) return;
    size_t count = (size_t)fb->width * (size_t)fb->height;
    for (size_t i = 0; i < count; ++i) fb->pixels[i] = color;
    fb->dirty = true;
}

void fb_putpixel(Framebuffer *fb, int x, int y, Color color) {
    if (!fb || !fb->pixels) return;
    if ((unsigned)x >= (unsigned)fb->width || (unsigned)y >= (unsigned)fb->height) return; // bounds check
    fb->pixels[y * fb->width + x] = color;
    fb->dirty = true;
}

void fb_draw_filled_circle(Framebuffer *fb, int cx, int cy, int radius, Color color) {
    if (radius <= 0 || !fb || !fb->pixels) return;
    int r2 = radius * radius;
    int minY = cy - radius;
    int maxY = cy + radius;
    if (minY < 0) minY = 0;
    if (maxY >= fb->height) maxY = fb->height - 1;
    for (int y = minY; y <= maxY; ++y) {
        int dy = y - cy;
        int dxMax = (int)sqrtf((float)(r2 - dy * dy));
        int minX = cx - dxMax;
        int maxX = cx + dxMax;
        if (minX < 0) minX = 0;
        if (maxX >= fb->width) maxX = fb->width - 1;
        Color *row = fb->pixels + y * fb->width;
        for (int x = minX; x <= maxX; ++x) row[x] = color;
    }
    fb->dirty = true;
}

void fb_present(Framebuffer *fb, int x, int y, float scale, Color tint) {
    if (!fb || !fb->pixels || fb->texture.id == 0) return;
    if (fb->dirty) {
        UpdateTexture(fb->texture, fb->pixels);
        fb->dirty = false;
    }
    if (scale <= 0.0f) scale = 1.0f;
    if (scale == 1.0f) {
        DrawTexture(fb->texture, x, y, tint);
    } else {
        Rectangle src = {0, 0, (float)fb->width, (float)fb->height};
        Rectangle dst = {(float)x, (float)y, fb->width * scale, fb->height * scale};
        Vector2 origin = {0, 0};
        DrawTexturePro(fb->texture, src, dst, origin, 0.0f, tint);
    }
}
