#ifndef VIDEO_PROCESSOR_H
#define VIDEO_PROCESSOR_H

#include "raylib.h"
#include "common.h"
#include <stdbool.h>
#include <pthread.h>

// Video processing structures
typedef struct {
    Texture2D* textures;
    int capacity;
    int count;
} TextureBuffer;

typedef struct {
    bool isProcessing;
    bool isCompleted;
    bool hasError;
    char errorMessage[512];
    char inputPath[512];
    int totalFrames;
    int processedFrames;
    char outputPath[512];
    pthread_t processingThread;
    pthread_mutex_t statusMutex;
} VideoProcessor;

// Global video processor instance
extern VideoProcessor gVideoProcessor;

// Function prototypes
void InitVideoProcessor(void);
void CleanupVideoProcessor(void);
void CleanupTempFrames(void);
void GetVideoProcessingStatus(bool* isProcessing, bool* isCompleted, bool* hasError, char* errorMsg);
bool StartVideoProcessing(const char* videoPath);

// Texture buffer management
void InitTextureBuffer(TextureBuffer* buffer, int capacity);
void FreeTextureBuffer(TextureBuffer* buffer);
bool LoadTextureToBuffer(TextureBuffer* buffer, const Image* image, int index);
Texture2D* GetTextureFromBuffer(TextureBuffer* buffer, int index);

// Frame loading and processing
void LoadAllAvailableFrames(Image** sequence, TextureBuffer* textureBuffer, int* totalFrames);
bool IsFrameAvailable(int frameIndex);
bool LoadSpecificFrame(int frameIndex, Image* frameImage);
Texture2D* GetTextureFromBuffer(TextureBuffer* buffer, int index);

#endif // VIDEO_PROCESSOR_H
