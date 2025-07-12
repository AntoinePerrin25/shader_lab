#ifndef SHADER_MANAGER_H
#define SHADER_MANAGER_H

#include "raylib.h"
#include "common.h"
#include <stdbool.h>

// Constants for shader management
#define MAX_SHADERS 64
#define MAX_SHADER_NAME 64

// Structure to manage shader loading errors
typedef struct {
    bool hasError;
    char errorMessage[512];
    bool isDefaultShader;
} ShaderState;

// Structure to manage available shaders
typedef struct {
    char names[MAX_SHADERS][MAX_SHADER_NAME];
    char paths[MAX_SHADERS][320];
    int count;
    int selectedIndex;
    int scrollOffset;
    int maxVisibleItems;
} ShaderManager;

// Global shader manager instance
extern ShaderManager gShaderManager;

// Function prototypes
void InitShaderManager(void);
Shader LoadShaderSafe(const char* vsFileName, const char* fsFileName, ShaderState* state);
void DiscoverShaders(void);
const char* GetSelectedShaderPath(void);
void ReloadCurrentShader(Shader* currentShader, ShaderState* currentShaderState);

// Shader preview system
void InitShaderPreviews(RenderTexture2D** previews, int previewSize);
void GenerateShaderPreview(RenderTexture2D previewTexture, Shader shader, int shaderIndex, float time);
void UpdateShaderPreviews(RenderTexture2D* previews, float time);
void FreeShaderPreviews(RenderTexture2D* previews);

#endif // SHADER_MANAGER_H
