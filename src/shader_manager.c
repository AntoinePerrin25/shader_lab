#include "../include/shader_manager.h"
#include "../include/common.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <dirent.h>
#include <sys/stat.h>
#include <time.h>

#define UNUSED (void)

// Global shader manager instance
ShaderManager gShaderManager = {0};

// Default shader source
const char* defaultFragmentShader = 
"#version 460\n"
"in vec2 fragTexCoord;\n"
"out vec4 fragColor;\n"
"uniform sampler2D texture0;\n"
"void main() {\n"
"    fragColor = texture(texture0, fragTexCoord);\n"
"}\n";

void InitShaderManager(void) {
    gShaderManager.count = 0;
    gShaderManager.selectedIndex = 0;
    gShaderManager.scrollOffset = 0;
    gShaderManager.maxVisibleItems = 8;
}

Shader LoadShaderSafe(const char* vsFileName, const char* fsFileName, ShaderState* state) {
    Shader shader = {0};
    
    // Reset state
    state->hasError = false;
    state->errorMessage[0] = '\0';
    state->isDefaultShader = false;
    
    // Try to load the shader
    shader = LoadShader(vsFileName, fsFileName);
    
    // Check if shader is valid
    if (shader.id == 0) {
        // Loading failed, use default shader
        printf("ERROR: Failed to load shader '%s'\n", fsFileName);
        LogMessage("LOG Shader loading failed");
        
        // Load default shader
        shader = LoadShaderFromMemory(NULL, defaultFragmentShader);
        
        if (shader.id == 0) {
            // Even default shader failed
            state->hasError = true;
            snprintf(state->errorMessage, sizeof(state->errorMessage), 
                    "CRITICAL ERROR: Cannot load default shader");
            printf("CRITICAL ERROR: Cannot load default shader\n");
        } else {
            state->isDefaultShader = true;
            snprintf(state->errorMessage, sizeof(state->errorMessage), 
                    "SHADER ERROR: Using default shader. Check '%s'", fsFileName);
            printf("Using default shader due to error in '%s'\n", fsFileName);
        }
    } else {
        printf("Shader loaded successfully: %s\n", fsFileName);
        LogMessage("LOG Shader loaded successfully");
    }
    
    return shader;
}

void DiscoverShaders(void) {
    printf("=== DISCOVERING SHADERS ===\n");
    
    gShaderManager.count = 0;
    gShaderManager.selectedIndex = 0;
    gShaderManager.scrollOffset = 0;
    gShaderManager.maxVisibleItems = 8;
    
    // Search in "shaders" folder first (new .fs shaders)
    DIR* shaderDir = opendir("shaders");
    if (shaderDir) {
        struct dirent* entry;
        while ((entry = readdir(shaderDir)) != NULL && gShaderManager.count < MAX_SHADERS) {
            if ((strstr(entry->d_name, ".fs") || strstr(entry->d_name, ".glsl")) && entry->d_name[0] != '.') {
                // Extract name without extension
                char nameWithoutExt[MAX_SHADER_NAME];
                int copyLen = snprintf(nameWithoutExt, sizeof(nameWithoutExt), "%s", entry->d_name);
                if (copyLen >= MAX_SHADER_NAME) {
                    printf("Warning: shader name too long, truncated: %s\n", entry->d_name);
                    continue;
                }
                
                char* dotPos = strrchr(nameWithoutExt, '.');
                if (dotPos) *dotPos = '\0';
                
                // Add to manager with folder path
                snprintf(gShaderManager.names[gShaderManager.count], MAX_SHADER_NAME, "%s", nameWithoutExt);
                
                int pathLen = snprintf(gShaderManager.paths[gShaderManager.count], 320, "shaders/%s", entry->d_name);
                if (pathLen >= 320) {
                    printf("Warning: shader path too long, truncated: %s\n", entry->d_name);
                    continue;
                }
                
                printf("Found shader: %s -> %s\n", nameWithoutExt, gShaderManager.paths[gShaderManager.count]);
                gShaderManager.count++;
            }
        }
        closedir(shaderDir);
    }
    
    // Search in root folder for compatibility (old .glsl shaders)
    DIR* dir = opendir(".");
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL && gShaderManager.count < MAX_SHADERS) {
            if ((strstr(entry->d_name, ".fs") || strstr(entry->d_name, ".glsl")) && entry->d_name[0] != '.') {
                // Check if we already have this shader from shaders/ folder
                bool alreadyExists = false;
                for (int i = 0; i < gShaderManager.count; i++) {
                    if (strcmp(gShaderManager.paths[i] + 8, entry->d_name) == 0) { // +8 to skip "shaders/"
                        alreadyExists = true;
                        break;
                    }
                }
                
                if (!alreadyExists) {
                    // Extract name without extension
                    char nameWithoutExt[MAX_SHADER_NAME];
                    int copyLen = snprintf(nameWithoutExt, sizeof(nameWithoutExt), "%s", entry->d_name);
                    if (copyLen >= MAX_SHADER_NAME) {
                        printf("Warning: shader name too long, truncated: %s\n", entry->d_name);
                        continue;
                    }
                    
                    char* dotPos = strrchr(nameWithoutExt, '.');
                    if (dotPos) *dotPos = '\0';
                    
                    // Add to manager
                    snprintf(gShaderManager.names[gShaderManager.count], MAX_SHADER_NAME, "%s (root)", nameWithoutExt);
                    snprintf(gShaderManager.paths[gShaderManager.count], 320, "%s", entry->d_name);
                    
                    printf("Found shader: %s -> %s\n", nameWithoutExt, entry->d_name);
                    gShaderManager.count++;
                }
            }
        }
        closedir(dir);
    }
    
    printf("Total shaders found: %d\n", gShaderManager.count);
    
    // If no shaders found, add a default shader
    if (gShaderManager.count == 0) {
        snprintf(gShaderManager.names[0], MAX_SHADER_NAME, "Default");
        snprintf(gShaderManager.paths[0], 320, "effect.fs");
        gShaderManager.count = 1;
        printf("No shaders found, using default\n");
    }
}

const char* GetSelectedShaderPath(void) {
    if (gShaderManager.selectedIndex >= 0 && gShaderManager.selectedIndex < gShaderManager.count) {
        return gShaderManager.paths[gShaderManager.selectedIndex];
    }
    return "effect.fs"; // Fallback
}

void ReloadCurrentShader(Shader* currentShader, ShaderState* currentShaderState) {
    if (currentShader->id > 0) {
        UnloadShader(*currentShader);
    }
    
    const char* currentShaderPath = GetSelectedShaderPath();
    printf("Manually reloading shader: %s\n", currentShaderPath);
    
    *currentShader = LoadShaderSafe(0, currentShaderPath, currentShaderState);
    
    LogMessage("LOG Shader manually reloaded");
}

// Shader preview system functions
void InitShaderPreviews(RenderTexture2D** previews, int previewSize) {
    if (*previews != NULL) {
        // Free old previews
        for (int i = 0; i < MAX_SHADERS; i++) {
            if ((*previews)[i].id > 0) {
                UnloadRenderTexture((*previews)[i]);
            }
        }
        free(*previews);
    }
    
    *previews = (RenderTexture2D*)calloc(MAX_SHADERS, sizeof(RenderTexture2D));
    
    // Create only render textures, without generating previews immediately
    for (int i = 0; i < gShaderManager.count; i++) {
        (*previews)[i] = LoadRenderTexture(previewSize, previewSize);
        printf("Initialized preview texture %d for shader: %s\n", i, gShaderManager.names[i]);
        
        // Initialize with simple white background to avoid artifacts
        BeginTextureMode((*previews)[i]);
            ClearBackground(LIGHTGRAY);
            DrawText("Loading...", 25, 50, 12, BLACK);
        EndTextureMode();
    }
    
    printf("Shader previews initialized: %d textures (lazy loading)\n", gShaderManager.count);
}

void GenerateShaderPreview(RenderTexture2D previewTexture, Shader shader, int shaderIndex, float time) {
    UNUSED shaderIndex; // Mark parameter as unused to avoid warning
    
    // Static shared canvas for all previews
    static Texture2D sharedPreviewCanvas = {0};
    static bool canvasInitialized = false;
    
    if (!canvasInitialized) {
        Image whiteImage = GenImageColor(120, 120, WHITE); // Fixed size for previews
        sharedPreviewCanvas = LoadTextureFromImage(whiteImage);
        UnloadImage(whiteImage);
        canvasInitialized = true;
    }
    
    BeginTextureMode(previewTexture);
        ClearBackground(WHITE);
        
        if (shader.id > 0) {
            BeginShaderMode(shader);
                // Uniforms for preview (simplified versions for performance)
                int timeLocation = GetShaderLocation(shader, "time");
                if (timeLocation != -1) {
                    SetShaderValue(shader, timeLocation, &time, SHADER_UNIFORM_FLOAT);
                }
                
                // Mouse position at center for preview
                float centerMouse[2] = {60.0f, 60.0f}; // Fixed coordinates for preview
                int mousePosLocation = GetShaderLocation(shader, "mousePos");
                if (mousePosLocation != -1) {
                    SetShaderValue(shader, mousePosLocation, centerMouse, SHADER_UNIFORM_VEC2);
                }
                
                // Other uniforms with fixed values for performance
                float previewRadius = 30.0f;
                float previewPower = 1.0f;
                int radiusLocation = GetShaderLocation(shader, "radius");
                int powerLocation = GetShaderLocation(shader, "power");
                if (radiusLocation != -1) SetShaderValue(shader, radiusLocation, &previewRadius, SHADER_UNIFORM_FLOAT);
                if (powerLocation != -1) SetShaderValue(shader, powerLocation, &previewPower, SHADER_UNIFORM_FLOAT);
                
                float resolution[2] = {120.0f, 120.0f}; // Fixed resolution
                int resolutionLocation = GetShaderLocation(shader, "resolution");
                if (resolutionLocation != -1) {
                    SetShaderValue(shader, resolutionLocation, resolution, SHADER_UNIFORM_VEC2);
                }
                
                DrawTexture(sharedPreviewCanvas, 0, 0, WHITE);
            EndShaderMode();
        } else {
            // Invalid shader - draw red square with X
            DrawRectangle(0, 0, 120, 120, RED);
            DrawText("X", 55, 55, 20, WHITE);
        }
    EndTextureMode();
}

void UpdateShaderPreviews(RenderTexture2D* previews, float time) {
    UNUSED time; // Mark parameter as unused
    if (!previews) return;
    
    static bool allPreviewsGenerated = false;
    
    // Generate all previews at the beginning, then leave them static
    if (!allPreviewsGenerated) {
        // Generate all previews at once on startup
        for (int i = 0; i < gShaderManager.count; i++) {
            if (previews[i].id > 0) {
                ShaderState tempState = {0};
                Shader tempShader = LoadShaderSafe(0, gShaderManager.paths[i], &tempState);
                
                // Use fixed time (2.0f) for static previews
                GenerateShaderPreview(previews[i], tempShader, i, 2.0f);
                
                if (tempShader.id > 0) {
                    UnloadShader(tempShader);
                }
            }
        }
        allPreviewsGenerated = true;
        printf("All shader previews generated (static)\n");
    }
}

void FreeShaderPreviews(RenderTexture2D* previews) {
    if (!previews) return;
    
    for (int i = 0; i < MAX_SHADERS; i++) {
        if (previews[i].id > 0) {
            UnloadRenderTexture(previews[i]);
        }
    }
    free(previews);
}
