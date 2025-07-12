#include "../include/ui_renderer.h"
#include "../include/common.h"
#include <stdio.h>
#include <string.h>
#include <math.h>

#define UNUSED (void)

// Global UI state
static UIState gUIState = {0};

void InitUI(void) {
    gUIState.showPreviewGrid = true;
    gUIState.previewSize = 120; // Default preview size
    gUIState.gridSpacing = 6;
    gUIState.scrollOffset = 0.0f; // Initialize scroll offset
    gUIState.maxScrollOffset = 0.0f; // Initialize max scroll
    CalculateGridLayout(&gUIState.gridCols, &gUIState.gridRows, &gUIState.previewSize, &gUIState.gridSpacing, &gUIState.textSize);
}

void CalculateGridLayout(int* cols, int* rows, int* previewSize, float* spacing, int* textSize) {
    int availableWidth = GetScreenWidth() - 20; // Leave 10px margin on each side
    int availableHeight = 200; // Increased from 150px for better visibility
    
    // Calculate optimal number of previews per row - more reasonable target
    int optimalPreviewsPerRow = 15; // Target 15 columns for 25 shaders (was 18)
    if (gShaderManager.count < optimalPreviewsPerRow) {
        optimalPreviewsPerRow = gShaderManager.count;
    }
    
    // Calculate actual row count
    int actualRowCount = (gShaderManager.count + optimalPreviewsPerRow - 1) / optimalPreviewsPerRow;
    
    // Calculate dynamic preview size based on available space - larger minimum
    int actualPreviewSize = (availableWidth - (optimalPreviewsPerRow + 1) * 10) / optimalPreviewsPerRow;
    if (actualPreviewSize > *previewSize * 0.75) actualPreviewSize = *previewSize * 0.75; // Cap at 75% of original size (was 50%)
    if (actualPreviewSize < 50) actualPreviewSize = 50; // Larger minimum size (was 25)
    
    // Calculate total content height for scrolling
    int totalContentHeight = actualRowCount * (actualPreviewSize + 25) + 10; // Add padding
    
    // Calculate maximum scroll offset (how much we can scroll down)
    gUIState.maxScrollOffset = (totalContentHeight > availableHeight) ? 
                               (totalContentHeight - availableHeight) : 0;
    
    // Clamp current scroll offset to valid range
    if (gUIState.scrollOffset < 0) gUIState.scrollOffset = 0;
    if (gUIState.scrollOffset > gUIState.maxScrollOffset) gUIState.scrollOffset = gUIState.maxScrollOffset;
    
    // Set outputs
    *cols = optimalPreviewsPerRow;
    *rows = actualRowCount;
    *previewSize = actualPreviewSize;
    *spacing = actualPreviewSize + 10; // More spacing for better layout
    *textSize = (actualPreviewSize < 50) ? 8 : ((actualPreviewSize < 70) ? 10 : 12); // Larger text sizes
}

Rectangle GetShaderPreviewRect(int index, int cols, int previewSize, float spacing) {
    int screenWidth = GetScreenWidth();
    int screenHeight = GetScreenHeight();
    int previewPanelHeight = 200; // Increased from 150
    
    int col = index % cols;
    int row = index / cols;
    
    // Center the grid horizontally
    int startX = (screenWidth - (cols * spacing - 10)) / 2; // Adjusted for better spacing
    int startY = screenHeight - previewPanelHeight + 10; // Start near top of panel
    
    int x = startX + col * spacing;
    int y = startY + row * (previewSize + 25) - (int)gUIState.scrollOffset; // Apply scroll offset
    
    return (Rectangle){x, y, previewSize, previewSize};
}

void RenderShaderPreviews(RenderTexture2D* previews, Vector2 mousePos, bool* mouseOnUI, int* selectedShaderFromGrid) {
    if (!previews || !gUIState.showPreviewGrid) return;
    
    int screenWidth = GetScreenWidth();
    int screenHeight = GetScreenHeight();
    int previewPanelHeight = 200; // Increased from 150
    
    // Update grid layout
    CalculateGridLayout(&gUIState.gridCols, &gUIState.gridRows, &gUIState.previewSize, &gUIState.gridSpacing, &gUIState.textSize);
    
    // Draw background for preview panel
    Rectangle previewPanel = {0, screenHeight - previewPanelHeight, screenWidth, previewPanelHeight};
    DrawRectangleRec(previewPanel, (Color){40, 40, 40, 200});
    
    // Handle mouse wheel scrolling when mouse is over preview panel
    if (CheckCollisionPointRec(mousePos, previewPanel)) {
        *mouseOnUI = true;
        float wheelMove = GetMouseWheelMove();
        if (wheelMove != 0) {
            gUIState.scrollOffset -= wheelMove * 30; // Scroll speed
            if (gUIState.scrollOffset < 0) gUIState.scrollOffset = 0;
            if (gUIState.scrollOffset > gUIState.maxScrollOffset) gUIState.scrollOffset = gUIState.maxScrollOffset;
        }
    }
    
    // Enable scissor clipping for the preview panel area
    BeginScissorMode(previewPanel.x, previewPanel.y, previewPanel.width, previewPanel.height);
    
    // Render shader previews in optimized grid layout
    for (int i = 0; i < gShaderManager.count; i++) {
        if (i < MAX_SHADERS && previews[i].id > 0) {
            Rectangle previewRect = GetShaderPreviewRect(i, gUIState.gridCols, gUIState.previewSize, gUIState.gridSpacing);
            
            // Only render if the preview is visible within the clipped area
            if (previewRect.y + previewRect.height > previewPanel.y && previewRect.y < previewPanel.y + previewPanel.height) {
                // Scale the preview texture to fit the dynamic size
                Rectangle sourceRect = {0, 0, 120, 120}; // Original preview size
                DrawTexturePro(previews[i].texture, sourceRect, previewRect, (Vector2){0, 0}, 0.0f, WHITE);
                
                // Highlight if this is the current shader
                if (i == gShaderManager.selectedIndex) {
                    DrawRectangleLinesEx(previewRect, 3, (Color){255, 255, 0, 255});
                } else {
                    DrawRectangleLinesEx(previewRect, 1, (Color){150, 150, 150, 255});
                }
                
                // Draw shader name below thumbnail with adaptive text size
                const char* shaderName = gShaderManager.names[i];
                int textWidth = MeasureText(shaderName, gUIState.textSize);
                int textX = previewRect.x + (gUIState.previewSize - textWidth) / 2;
                int textY = previewRect.y + gUIState.previewSize + 2;
                
                // Only draw text if it's visible
                if (textY + gUIState.textSize > previewPanel.y && textY < previewPanel.y + previewPanel.height) {
                    // Ensure text doesn't overflow
                    if (textWidth > gUIState.previewSize) {
                        textX = previewRect.x;
                        // Truncate text if too long - more characters for larger previews
                        char truncatedName[20];
                        int maxChars = (gUIState.previewSize > 60) ? 15 : 10;
                        snprintf(truncatedName, sizeof(truncatedName), "%.*s...", maxChars, shaderName);
                        if (strlen(shaderName) > maxChars) {
                            DrawText(truncatedName, textX, textY, gUIState.textSize, WHITE);
                        } else {
                            DrawText(shaderName, textX, textY, gUIState.textSize, WHITE);
                        }
                    } else {
                        DrawText(shaderName, textX, textY, gUIState.textSize, WHITE);
                    }
                }
                
                // Check for mouse interaction (only if visible)
                if (CheckCollisionPointRec(mousePos, previewRect)) {
                    *mouseOnUI = true;
                    
                    // Check for mouse click on preview
                    if (IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
                        printf("Clicked on shader preview %d: %s\n", i, gShaderManager.names[i]);
                        *selectedShaderFromGrid = i;
                    }
                }
            }
        }
    }
    
    // End scissor clipping
    EndScissorMode();
    
    // Draw scroll indicator if scrollable
    if (gUIState.maxScrollOffset > 0) {
        // Draw scroll bar on the right side
        int scrollBarWidth = 10;
        int scrollBarX = screenWidth - scrollBarWidth - 5;
        Rectangle scrollBarBg = {scrollBarX, previewPanel.y, scrollBarWidth, previewPanelHeight};
        DrawRectangleRec(scrollBarBg, (Color){60, 60, 60, 150});
        
        // Calculate scroll thumb position and size
        float scrollRatio = gUIState.scrollOffset / gUIState.maxScrollOffset;
        float thumbHeight = (previewPanelHeight * previewPanelHeight) / (previewPanelHeight + gUIState.maxScrollOffset);
        if (thumbHeight < 20) thumbHeight = 20; // Minimum thumb size
        float thumbY = previewPanel.y + scrollRatio * (previewPanelHeight - thumbHeight);
        
        Rectangle scrollThumb = {scrollBarX, thumbY, scrollBarWidth, thumbHeight};
        DrawRectangleRec(scrollThumb, (Color){120, 120, 120, 200});
        
        // Draw scroll help text
        DrawText("Scroll: Mouse Wheel", 10, screenHeight - previewPanelHeight + 5, 10, (Color){200, 200, 200, 150});
    }
    
    // Draw debug info for grid layout (toggle with F1)
    static bool showDebugInfo = false;
    if (IsKeyPressed(KEY_F1)) showDebugInfo = !showDebugInfo;
    
    if (showDebugInfo) {
        DrawText(TextFormat("Shaders: %d | Rows: %d | Per Row: %d | Size: %d | Scroll: %.0f/%.0f", 
            gShaderManager.count, gUIState.gridRows, gUIState.gridCols, gUIState.previewSize, 
            gUIState.scrollOffset, gUIState.maxScrollOffset), 
            10, screenHeight - previewPanelHeight + 25, 10, GREEN);
    }
}

void RenderReloadButton(Vector2 mousePos, bool* mouseOnUI, bool* shouldReload) {
    Rectangle reloadButton = {10, 80, 100, 25};
    
    // Check mouse interaction
    bool isHovered = CheckCollisionPointRec(mousePos, reloadButton);
    if (isHovered) {
        *mouseOnUI = true;
    }
    
    // Draw button
    Color buttonColor = SKYBLUE;
    if (isHovered) {
        buttonColor = ColorBrightness(buttonColor, 0.2f);
    }
    
    DrawRectangleRec(reloadButton, buttonColor);
    DrawRectangleLinesEx(reloadButton, 2, BLACK);
    DrawText("Reload Shaders", reloadButton.x + 5, reloadButton.y + 5, 10, BLACK);
    
    // Check for click
    if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && isHovered) {
        *shouldReload = true;
    }
}

void RenderMainUI(Vector2 mousePos, bool* mouseOnUI) {
    UNUSED mousePos; // Not used in current implementation
    UNUSED mouseOnUI; // Not used in current implementation
    int screenHeight = GetScreenHeight();
    int previewPanelHeight = 200; // Updated to match new preview panel height
    
    // Main panel (left side)
    Rectangle panel = {0, 0, 200, screenHeight - previewPanelHeight};
    DrawRectangleRec(panel, LIGHTGRAY);
    
    int textY = 15;
    
    // Title
    DrawText("Shader Lab", 10, textY, 16, BLACK);
    textY += 30;
    
    // Controls section
    DrawText("Controls:", 10, textY, 14, BLACK);
    textY += 20;
    DrawText("Arrow Keys: Radius", 10, textY, 12, BLACK);
    textY += 15;
    DrawText("+/-: Power", 10, textY, 12, BLACK);
    textY += 15;
    DrawText("Space: Lock Mouse", 10, textY, 12, BLACK);
    textY += 15;
    DrawText("C: Canvas Mode", 10, textY, 12, BLACK);
    textY += 15;
    DrawText("Q/E: Switch Shaders", 10, textY, 12, BLACK);
    textY += 15;
    DrawText("R: Reload Shader", 10, textY, 12, BLACK);
    textY += 25;
    
    // Current shader info
    DrawText("Current Shader:", 10, textY, 14, BLACK);
    textY += 20;
    if (gShaderManager.selectedIndex >= 0 && gShaderManager.selectedIndex < gShaderManager.count) {
        DrawText(gShaderManager.names[gShaderManager.selectedIndex], 10, textY, 12, BLACK);
    } else {
        DrawText("Default", 10, textY, 12, BLACK);
    }
    textY += 25;
    
    // Instructions
    DrawText("Drag image/video here", 10, textY, 12, DARKGRAY);
    textY += 15;
    DrawText("Click preview below", 10, textY, 12, DARKGRAY);
    textY += 15;
    DrawText("to switch shaders", 10, textY, 12, DARKGRAY);
}

void RenderVideoProcessingUI(Vector2 mousePos, bool* mouseOnUI) {
    UNUSED mousePos; // Not used in current implementation
    UNUSED mouseOnUI; // Not used in current implementation
    // This function can be expanded to show video processing status
    // For now, we'll keep it simple since we removed the complex video UI
    
    bool isProcessing, isCompleted, hasError;
    char errorMsg[512];
    GetVideoProcessingStatus(&isProcessing, &isCompleted, &hasError, errorMsg);
    
    if (hasError) {
        int textY = 400;
        DrawText("Video Error:", 10, textY, 12, RED);
        textY += 15;
        DrawText(errorMsg, 10, textY, 10, RED);
    }
}
