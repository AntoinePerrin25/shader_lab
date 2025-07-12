#ifndef UI_RENDERER_H
#define UI_RENDERER_H

#include "raylib.h"
#include "common.h"
#include "shader_manager.h"
#include "video_processor.h"
#include <stdbool.h>

// UI state and configuration
typedef struct {
    bool showPreviewGrid;
    int previewSize;
    int gridCols;
    int gridRows;
    float gridSpacing;
    int textSize;
} UIState;

// Function prototypes for UI rendering
void InitUI(void);
void RenderMainUI(Vector2 mousePos, bool* mouseOnUI);
void RenderShaderPreviews(RenderTexture2D* previews, Vector2 mousePos, bool* mouseOnUI, int* selectedShaderFromGrid);
void RenderVideoProcessingUI(Vector2 mousePos, bool* mouseOnUI);
void RenderReloadButton(Vector2 mousePos, bool* mouseOnUI, bool* shouldReload);

// Helper functions for UI calculations
void CalculateGridLayout(int* cols, int* rows, int* previewSize, float* spacing, int* textSize);
Rectangle GetShaderPreviewRect(int index, int cols, int previewSize, float spacing);

#endif // UI_RENDERER_H
