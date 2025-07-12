#include "../include/common.h"
#include "../include/shader_manager.h"
#include "../include/video_processor.h"
#include "../include/ui_renderer.h"
#include "raylib.h"
#include <sys/stat.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h>
#include <stdlib.h>
#include <dirent.h>
#include <unistd.h>
#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOUSER
#define NOGDI
#include <windows.h>
#undef LoadImage
#endif
#define UNUSED (void)

// File modification time helper function
time_t My_GetFileModTime(const char *path)
{
    struct stat attrib;
    if (stat(path, &attrib) == 0)
        return attrib.st_mtime;
    else
        return 0;
}

int main(void)
{
    InitLogger();
    LogMessage("LOG Program Start");
    
    InitVideoProcessor();
    LogMessage("LOG Video processor initialized");
    
    const int screenWidth = 1080;
    const int screenHeight = 900;
    
    // Variables for shader preview system
    const int previewPanelHeight = 150;
    const int previewSize = 120;
    RenderTexture2D* shaderPreviews = NULL;
    bool previewsNeedUpdate = true;
    
    InitWindow(screenWidth, screenHeight, "Shader Lab - Drag & Drop");
    LogMessage("LOG Window Initialized");

    // Initialize shader system
    InitShaderManager();
    DiscoverShaders();
    LogMessage("LOG Shaders discovered");
    
    // Initialize shader previews
    InitShaderPreviews(&shaderPreviews, previewSize);
    LogMessage("LOG Shader previews initialized");

    // Initialize UI
    InitUI();
    LogMessage("LOG UI initialized");

    const char *shaderPath = GetSelectedShaderPath();
    ShaderState shaderState = {0};
    Shader shader = LoadShaderSafe(0, shaderPath, &shaderState);
    LogMessage("LOG Shader loaded with error checking");

    time_t lastModTime = My_GetFileModTime(shaderPath);
    LogMessage("LOG File mod time retrieved");

    Texture2D originalImageTex = {0};
    Texture2D canvasTexture = {0};
    bool useCanvas = false;
    LogMessage("LOG Image textures initialized");
    
    // Variables for image scaling and positioning
    Rectangle imageRect = {0};
    Rectangle sourceRect = {0};
    Vector2 imageScale = {1.0f, 1.0f};

    // Variables for video sequences
    bool isSequence = false;
    bool isPlaying = false;
    int currentFrame = 0;
    int totalFrames = 0;
    float frameTime = 0.0f;
    float frameRate = 30.0f;
    Image* frameSequence = NULL;
    TextureBuffer videoTextureBuffer = {0};
    char loadedFilePath[512] = {0};
    
    // UI control variables
    Rectangle playPauseButton = {10, 550, 80, 30};
    Rectangle prevButton = {100, 550, 40, 30};
    Rectangle nextButton = {150, 550, 40, 30};
    Rectangle frameSlider = {10, 590, 180, 20};
    Rectangle reloadButton = {10, 620, 80, 30};
    Rectangle loadAllButton = {100, 620, 90, 30};
    // float sliderValue = 0.0f; // Currently unused
    
    // Mouse lock system variables
    bool mouseLocked = false;
    Vector2 lockedMouseInImage = {0, 0};
    bool wasSpacePressed = false;
    float radius = 50.0f;
    float power = 1.0f;
    
    SetTargetFPS(60);
    LogMessage("LOG FPS set to 60");

    // Create default white canvas for shaders
    Image whiteCanvas = GenImageColor(800, 600, WHITE);
    canvasTexture = LoadTextureFromImage(whiteCanvas);
    UnloadImage(whiteCanvas);
    useCanvas = true;
    
    // Calculate dimensions for canvas (accounting for preview panel)
    float availableWidth = screenWidth - 200; // Available width (screen - panel)
    float availableHeight = screenHeight - previewPanelHeight; // Available height (screen - preview panel)
    
    float scaleX = availableWidth / canvasTexture.width;
    float scaleY = availableHeight / canvasTexture.height;
    float scale = fminf(scaleX, scaleY); // Keep proportions
    
    imageScale.x = scale;
    imageScale.y = scale;
    
    // Center canvas in available area
    float scaledWidth = canvasTexture.width * scale;
    float scaledHeight = canvasTexture.height * scale;
    
    imageRect.x = 200 + (availableWidth - scaledWidth) / 2;
    imageRect.y = (availableHeight - scaledHeight) / 2;
    imageRect.width = scaledWidth;
    imageRect.height = scaledHeight;
    
    sourceRect.x = 0;
    sourceRect.y = 0;
    sourceRect.width = canvasTexture.width;
    sourceRect.height = canvasTexture.height;
    
    LogMessage("LOG White canvas initialized");

    while (!WindowShouldClose())
    {
        // Check for file modification every 60 frames
        static int frameCounter = 0;
        frameCounter++;
        
        if (frameCounter % 60 == 0) {
            time_t newModTime = My_GetFileModTime(GetSelectedShaderPath());
            if (newModTime != lastModTime && newModTime != 0) {
                printf("Shader file modified, reloading: %s\n", GetSelectedShaderPath());
                UnloadShader(shader);
                shader = LoadShaderSafe(0, GetSelectedShaderPath(), &shaderState);
                lastModTime = newModTime;
                LogMessage("LOG Shader reloaded due to file modification");
                printf("Shader reloaded due to file modification.\n");
            }
        }
        
        // Handle drag & drop
        if (IsFileDropped())
        {
            LogMessage("LOG File dropped");
            FilePathList files = LoadDroppedFiles();
            if (files.count > 0)
            {
                LogMessage("LOG Processing dropped files");
                // Check if file is a supported image/video
                if (IsFileExtension(files.paths[0], ".png") || 
                    IsFileExtension(files.paths[0], ".jpg") || 
                    IsFileExtension(files.paths[0], ".jpeg") || 
                    IsFileExtension(files.paths[0], ".bmp") || 
                    IsFileExtension(files.paths[0], ".tga") || 
                    IsFileExtension(files.paths[0], ".gif") || 
                    IsFileExtension(files.paths[0], ".hdr") || 
                    IsFileExtension(files.paths[0], ".pic") || 
                    IsFileExtension(files.paths[0], ".psd"))
                {
                    LogMessage("LOG Image file detected");
                    
                    // Clean up previous data
                    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
                    if (frameSequence != NULL) {
                        for (int i = 0; i < totalFrames; i++) {
                            UnloadImage(frameSequence[i]);
                        }
                        free(frameSequence);
                        frameSequence = NULL;
                    }
                    FreeTextureBuffer(&videoTextureBuffer);
                    
                    // Load new image
                    Image img = LoadImage(files.paths[0]);
                    originalImageTex = LoadTextureFromImage(img);
                    UnloadImage(img);
                    
                    // Switch to image mode (disable canvas)
                    useCanvas = false;
                    
                    // Reset sequence variables
                    isSequence = false;
                    isPlaying = false;
                    currentFrame = 0;
                    totalFrames = 0;
                    
                    strcpy(loadedFilePath, files.paths[0]);
                    
                    // Calculate new scale and position for image
                    scaleX = availableWidth / originalImageTex.width;
                    scaleY = availableHeight / originalImageTex.height;
                    scale = fminf(scaleX, scaleY);
                    
                    imageScale.x = scale;
                    imageScale.y = scale;
                    
                    scaledWidth = originalImageTex.width * scale;
                    scaledHeight = originalImageTex.height * scale;
                    
                    imageRect.x = 200 + (availableWidth - scaledWidth) / 2;
                    imageRect.y = (availableHeight - scaledHeight) / 2;
                    imageRect.width = scaledWidth;
                    imageRect.height = scaledHeight;
                    
                    sourceRect.x = 0;
                    sourceRect.y = 0;
                    sourceRect.width = originalImageTex.width;
                    sourceRect.height = originalImageTex.height;
                    
                    LogMessage("LOG Image loaded and positioned");
                }
                // Handle video files
                else if (IsFileExtension(files.paths[0], ".mp4") || 
                        IsFileExtension(files.paths[0], ".mov") || 
                        IsFileExtension(files.paths[0], ".avi") || 
                        IsFileExtension(files.paths[0], ".mkv") || 
                        IsFileExtension(files.paths[0], ".webm") || 
                        IsFileExtension(files.paths[0], ".m4v"))
                {
                    LogMessage("LOG Video file detected");
                    printf("Video file detected: %s\n", files.paths[0]);
                    
                    // Clean up previous data
                    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
                    if (frameSequence != NULL) {
                        for (int i = 0; i < totalFrames; i++) {
                            UnloadImage(frameSequence[i]);
                        }
                        free(frameSequence);
                        frameSequence = NULL;
                    }
                    FreeTextureBuffer(&videoTextureBuffer);
                    
                    strcpy(loadedFilePath, files.paths[0]);
                    
                    // Process video synchronously
                    if (StartVideoProcessing(files.paths[0])) {
                        printf("Video processing completed successfully\n");
                        LoadAllAvailableFrames(&frameSequence, &videoTextureBuffer, &totalFrames);
                        
                        if (totalFrames > 0) {
                            printf("Video loaded: %d frames\n", totalFrames);
                            isSequence = true;
                            isPlaying = false;
                            currentFrame = 0;
                            useCanvas = false;
                            
                            // Load first frame as texture
                            Texture2D* firstTexture = GetTextureFromBuffer(&videoTextureBuffer, 0);
                            if (firstTexture != NULL) {
                                originalImageTex = *firstTexture;
                            } else {
                                originalImageTex = LoadTextureFromImage(frameSequence[0]);
                            }
                            
                            // Calculate scaling for video
                            scaleX = availableWidth / originalImageTex.width;
                            scaleY = availableHeight / originalImageTex.height;
                            scale = fminf(scaleX, scaleY);
                            
                            imageScale.x = scale;
                            imageScale.y = scale;
                            
                            scaledWidth = originalImageTex.width * scale;
                            scaledHeight = originalImageTex.height * scale;
                            
                            imageRect.x = 200 + (availableWidth - scaledWidth) / 2;
                            imageRect.y = (availableHeight - scaledHeight) / 2;
                            imageRect.width = scaledWidth;
                            imageRect.height = scaledHeight;
                            
                            sourceRect.x = 0;
                            sourceRect.y = 0;
                            sourceRect.width = originalImageTex.width;
                            sourceRect.height = originalImageTex.height;
                            
                            LogMessage("LOG Video loaded and first frame set");
                        } else {
                            printf("ERROR: No frames loaded from video\n");
                            LogMessage("LOG Video processing failed - no frames");
                        }
                    } else {
                        printf("ERROR: Video processing failed\n");
                        LogMessage("LOG Video processing failed");
                    }
                }
            }
            UnloadDroppedFiles(files);
        }
        
        // Handle shader switching with Q/E keys
        if (IsKeyPressed(KEY_Q)) {
            int newIndex = (gShaderManager.selectedIndex - 1 + gShaderManager.count) % gShaderManager.count;
            gShaderManager.selectedIndex = newIndex;
            
            UnloadShader(shader);
            shader = LoadShaderSafe(0, GetSelectedShaderPath(), &shaderState);
            lastModTime = My_GetFileModTime(GetSelectedShaderPath());
            LogMessage("LOG Shader switched (Q key)");
        }
        if (IsKeyPressed(KEY_E)) {
            int newIndex = (gShaderManager.selectedIndex + 1) % gShaderManager.count;
            gShaderManager.selectedIndex = newIndex;
            
            UnloadShader(shader);
            shader = LoadShaderSafe(0, GetSelectedShaderPath(), &shaderState);
            lastModTime = My_GetFileModTime(GetSelectedShaderPath());
            LogMessage("LOG Shader switched (E key)");
        }
        
        // Handle manual shader reload with R key
        if (IsKeyPressed(KEY_R)) {
            ReloadCurrentShader(&shader, &shaderState);
            lastModTime = My_GetFileModTime(GetSelectedShaderPath());
        }
        
        // Handle canvas mode toggle with C key
        if (IsKeyPressed(KEY_C)) {
            useCanvas = !useCanvas;
            LogMessage(useCanvas ? "LOG Canvas mode enabled" : "LOG Canvas mode disabled");
        }
        
        // Handle mouse lock toggle with SPACE key
        bool isSpacePressed = IsKeyDown(KEY_SPACE);
        if (isSpacePressed && !wasSpacePressed) {
            if (!mouseLocked) {
                Vector2 mouse = GetMousePosition();
                if (CheckCollisionPointRec(mouse, imageRect)) {
                    lockedMouseInImage.x = (mouse.x - imageRect.x) / imageScale.x;
                    lockedMouseInImage.y = (mouse.y - imageRect.y) / imageScale.y;
                    Texture2D currentTexture = useCanvas ? canvasTexture : originalImageTex;
                    lockedMouseInImage.x = fmaxf(0, fminf(lockedMouseInImage.x, currentTexture.width));
                    lockedMouseInImage.y = fmaxf(0, fminf(lockedMouseInImage.y, currentTexture.height));
                    mouseLocked = true;
                    LogMessage("LOG Mouse locked");
                }
            } else {
                mouseLocked = false;
                LogMessage("LOG Mouse unlocked");
            }
        }
        wasSpacePressed = isSpacePressed;
        
        // Handle video playback
        if (isSequence && isPlaying) {
            frameTime += GetFrameTime();
            if (frameTime >= 1.0f / frameRate) {
                frameTime = 0.0f;
                
                int nextFrame = (currentFrame + 1) % totalFrames;
                if (frameSequence != NULL && frameSequence[nextFrame].data != NULL) {
                    currentFrame = nextFrame;
                    Texture2D* nextTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                    if (nextTexture != NULL) {
                        originalImageTex = *nextTexture;
                    } else {
                        UnloadTexture(originalImageTex);
                        originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                    }
                    // sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                } else {
                    isPlaying = false;
                    printf("Playback stopped: next frame not available\n");
                }
            }
        }

        // Main panel area
        Rectangle panel = {0, 0, 200, screenHeight - 150};

        // Determine mouse interaction
        Vector2 mouse = GetMousePosition();
        Vector2 effectiveMouseInImage = mouseLocked ? lockedMouseInImage : (Vector2){0, 0};
        
        bool applyShader = false;
        bool mouseOnUI = false;
        
        // Check if mouse is on UI elements
        if (isSequence) {
            mouseOnUI = CheckCollisionPointRec(mouse, playPauseButton) ||
                       CheckCollisionPointRec(mouse, prevButton) ||
                       CheckCollisionPointRec(mouse, nextButton) ||
                       CheckCollisionPointRec(mouse, frameSlider) ||
                       CheckCollisionPointRec(mouse, reloadButton) ||
                       CheckCollisionPointRec(mouse, loadAllButton);
        }
        
        if (mouseLocked) {
            // If mouse is locked, apply shader continuously at locked position
            applyShader = (originalImageTex.id > 0 || useCanvas);
        } else {
            // Normal behavior: left click to apply shader
            applyShader = (IsMouseButtonDown(MOUSE_LEFT_BUTTON) || IsMouseButtonPressed(MOUSE_LEFT_BUTTON)) && 
                          !CheckCollisionPointRec(mouse, panel) && 
                          !mouseOnUI &&
                          (originalImageTex.id > 0 || useCanvas) && 
                          CheckCollisionPointRec(mouse, imageRect);
        }

        BeginDrawing();
            ClearBackground(RAYWHITE);
            
            // Update shader previews when needed
            float currentTime = GetTime();
            if (previewsNeedUpdate) {
                UpdateShaderPreviews(shaderPreviews, currentTime);
                previewsNeedUpdate = false;
                printf("Shader previews updated\n");
            }
            
            // Render main UI
            RenderMainUI(mouse, &mouseOnUI);
            
            // Render reload button 
            bool shouldReload = false;
            RenderReloadButton(mouse, &mouseOnUI, &shouldReload);
            if (shouldReload) {
                printf("Reloading shader list...\n");
                DiscoverShaders();
                
                // Reload current shader
                const char* currentShaderPath = GetSelectedShaderPath();
                UnloadShader(shader);
                shader = LoadShaderSafe(0, currentShaderPath, &shaderState);
                lastModTime = My_GetFileModTime(currentShaderPath);
                
                printf("Shader list reloaded, found %d shaders\n", gShaderManager.count);
                LogMessage("LOG Shader list reloaded");
            }
            
            // Render shader previews at bottom
            int selectedShaderFromGrid = -1;
            RenderShaderPreviews(shaderPreviews, mouse, &mouseOnUI, &selectedShaderFromGrid);
            
            // Handle shader selection from grid
            if (selectedShaderFromGrid >= 0) {
                gShaderManager.selectedIndex = selectedShaderFromGrid;
                
                // Load new shader
                const char* newShaderPath = GetSelectedShaderPath();
                printf("Switching to shader: %s\n", newShaderPath);
                
                UnloadShader(shader);
                shader = LoadShaderSafe(0, newShaderPath, &shaderState);
                lastModTime = My_GetFileModTime(newShaderPath);
                
                LogMessage("LOG Shader switched via preview click");
                previewsNeedUpdate = true;
            }
            
            // Handle radius/power controls
            if (IsKeyDown(KEY_UP)) {
                radius += 1.0f;
                LogMessage("LOG Radius increased");
            }
            if (IsKeyDown(KEY_DOWN)) {
                radius -= 1.0f;
                LogMessage("LOG Radius decreased");
            }
            if (IsKeyDown(KEY_KP_ADD)) {
                power += 1.0f;
                LogMessage("LOG Power increased");
            }
            if (IsKeyDown(KEY_KP_SUBTRACT)) {
                power -= 1.0f;
                LogMessage("LOG Power decreased");
            }
            
            // Handle video controls (keyboard)
            if (isSequence) {
                if (IsKeyPressed(KEY_P)) {
                    if (!isPlaying) {
                        int nextFrame = (currentFrame + 1) % totalFrames;
                        if (frameSequence[nextFrame].data != NULL) {
                            isPlaying = true;
                            LogMessage("LOG Playback started (keyboard)");
                        }
                    } else {
                        isPlaying = false;
                        LogMessage("LOG Playback paused (keyboard)");
                    }
                }
                if (IsKeyPressed(KEY_LEFT)) {
                    int prevFrame = (currentFrame - 1 + totalFrames) % totalFrames;
                    if (frameSequence != NULL && frameSequence[prevFrame].data != NULL) {
                        currentFrame = prevFrame;
                        Texture2D* prevTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (prevTexture != NULL) {
                            originalImageTex = *prevTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        // sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Previous frame (keyboard)");
                    }
                }
                if (IsKeyPressed(KEY_RIGHT)) {
                    int nextFrame = (currentFrame + 1) % totalFrames;
                    if (frameSequence != NULL && frameSequence[nextFrame].data != NULL) {
                        currentFrame = nextFrame;
                        Texture2D* nextTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (nextTexture != NULL) {
                            originalImageTex = *nextTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        // sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Next frame (keyboard)");
                    }
                }
            }

            // Draw image with shader if needed
            if (originalImageTex.id > 0 || useCanvas)
            {
                // Choose texture to use
                Texture2D currentTexture = useCanvas ? canvasTexture : originalImageTex;
                
                LogMessage("LOG Drawing image/canvas");
                
                // Check if we can apply shader
                if (applyShader && !shaderState.hasError && shader.id > 0)
                {
                    // Convert mouse coordinates to image/canvas coordinates
                    Vector2 mouseInImage;
                    if (mouseLocked) {
                        mouseInImage = effectiveMouseInImage;
                    } else {
                        mouseInImage.x = (mouse.x - imageRect.x) / imageScale.x;
                        mouseInImage.y = (mouse.y - imageRect.y) / imageScale.y;
                    }
                    
                    // Ensure coordinates are within image/canvas bounds
                    mouseInImage.x = fmaxf(0, fminf(mouseInImage.x, currentTexture.width));
                    mouseInImage.y = fmaxf(0, fminf(mouseInImage.y, currentTexture.height));

                    // Get time in seconds since program launch
                    float timeSeconds = (float)GetTime();
                    
                    // Apply shader directly during display
                    BeginShaderMode(shader);
                        // Send all uniform variables to shader
                        int timeLocation = GetShaderLocation(shader, "time");
                        if (timeLocation != -1) {
                            SetShaderValue(shader, timeLocation, &timeSeconds, SHADER_UNIFORM_FLOAT);
                        }
                        SetShaderValue(shader, GetShaderLocation(shader, "mousePos"), 
                                     (float[2]){mouseInImage.x, mouseInImage.y}, SHADER_UNIFORM_VEC2);
                        SetShaderValue(shader, GetShaderLocation(shader, "radius"), &radius, SHADER_UNIFORM_FLOAT);
                        SetShaderValue(shader, GetShaderLocation(shader, "power"), &power, SHADER_UNIFORM_FLOAT);
                        SetShaderValue(shader, GetShaderLocation(shader, "resolution"), 
                                     (float[2]){(float)currentTexture.width, (float)currentTexture.height}, SHADER_UNIFORM_VEC2);

                        DrawTexturePro(currentTexture, sourceRect, imageRect, (Vector2){0, 0}, 0.0f, WHITE);
                    EndShaderMode();
                    LogMessage("LOG Shader applied to image/canvas");
                }
                else
                {
                    // Display original image/canvas without shader
                    DrawTexturePro(currentTexture, sourceRect, imageRect, (Vector2){0, 0}, 0.0f, WHITE);
                    
                    // If shader has error, display message on image/canvas
                    if (shaderState.hasError) {
                        DrawText("INVALID SHADER", imageRect.x + 10, imageRect.y + 10, 20, RED);
                        DrawText("Fix the shader", imageRect.x + 10, imageRect.y + 35, 16, RED);
                    } else if (shaderState.isDefaultShader) {
                        DrawText("DEFAULT SHADER", imageRect.x + 10, imageRect.y + 10, 16, ORANGE);
                    }
                }
                
                // Draw border around image/canvas for debug
                DrawRectangleLinesEx(imageRect, 2, DARKGRAY);
                
                // If using canvas, display indication
                if (useCanvas) {
                    DrawText("Canvas Mode - Drag image to switch", imageRect.x + 10, imageRect.y + imageRect.height - 25, 12, DARKGRAY);
                }
            }

        EndDrawing();
    }

    LogMessage("LOG Program ending - cleaning up");
    
    // Clean up video processor
    CleanupVideoProcessor();
    LogMessage("LOG Video processor cleaned up");
    
    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
    if (canvasTexture.id > 0) UnloadTexture(canvasTexture);
    
    // Clean up image sequences
    if (frameSequence != NULL) {
        for (int i = 0; i < totalFrames; i++) {
            UnloadImage(frameSequence[i]);
        }
        free(frameSequence);
    }
    
    // Clean up texture buffer
    FreeTextureBuffer(&videoTextureBuffer);
    
    // Free shader previews
    FreeShaderPreviews(shaderPreviews);
    
    UnloadShader(shader);
    CloseWindow();
    LogMessage("LOG Program ended");

    CloseLogger();
    return 0;
}
