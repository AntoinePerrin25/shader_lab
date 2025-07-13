#include "../include/video_processor.h"
#include "../include/common.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <dirent.h>
#include <sys/stat.h>
#include <unistd.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOUSER
#define NOGDI
#include <windows.h>
#undef LoadImage
#undef Rectangle
#undef CloseWindow
#undef ShowCursor
#define sleep(x) Sleep((x) * 1000)
#endif

// Global video processor instance
VideoProcessor gVideoProcessor = {0};

void InitVideoProcessor(void) {
    memset(&gVideoProcessor, 0, sizeof(VideoProcessor));
    snprintf(gVideoProcessor.outputPath, sizeof(gVideoProcessor.outputPath), "./temp_frames/");
    printf("Video processor initialized (synchronous mode)\n");
}

void CleanupTempFrames(void) {
    DIR* dir = opendir(gVideoProcessor.outputPath);
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL) {
            if (strstr(entry->d_name, "frame_") && strstr(entry->d_name, ".png")) {
                char fullPath[1024];
                snprintf(fullPath, sizeof(fullPath), "%s%s", gVideoProcessor.outputPath, entry->d_name);
                remove(fullPath);
            }
        }
        closedir(dir);
    }
    rmdir(gVideoProcessor.outputPath);
}

static bool ProcessVideoSynchronous(const char* videoPath) {
    printf("=== PROCESSING VIDEO SYNCHRONOUSLY ===\n");
    
    // Copy video path
    strcpy(gVideoProcessor.inputPath, videoPath);
    gVideoProcessor.isProcessing = false;
    gVideoProcessor.isCompleted = false;
    gVideoProcessor.hasError = false;
    gVideoProcessor.totalFrames = 0;
    gVideoProcessor.processedFrames = 0;
    
    // Create temp directory
    printf("Creating temp directory...\n");
    #ifdef _WIN32
    int mkdirResult = system("if not exist temp_frames mkdir temp_frames");
    #else
    int mkdirResult = mkdir(gVideoProcessor.outputPath, 0755);
    #endif
    
    if (mkdirResult != 0) {
        printf("ERROR: Failed to create temp directory\n");
        gVideoProcessor.hasError = true;
        strcpy(gVideoProcessor.errorMessage, "Cannot create temp directory");
        return false;
    }
    
    // FFmpeg command to extract frames and get info
    char ffmpegCmd[1024];
    char ffprobeCmd[1024];
    
    printf("Getting video info with ffprobe...\n");
    // First, get video info (FPS, duration, etc.)
    snprintf(ffprobeCmd, sizeof(ffprobeCmd), "ffprobe -v quiet -select_streams v:0 -show_entries stream=r_frame_rate -of csv=p=0 \"%s\" > temp_fps.txt", 
            videoPath);
    
    int result = system(ffprobeCmd);
    if (result == 0) {
        // Read video FPS
        FILE* fpsFile = fopen("temp_fps.txt", "r");
        if (fpsFile) {
            char fpsString[64];
            if (fgets(fpsString, sizeof(fpsString), fpsFile)) {
                // Remove newline
                fpsString[strcspn(fpsString, "\n")] = 0;
                
                // Handle fractions like "30/1"
                float num, den;
                if (sscanf(fpsString, "%f/%f", &num, &den) == 2 && den > 0) {
                    // gVideoProcessor.fps = num / den;
                } else {
                    // gVideoProcessor.fps = atof(fpsString);
                }
                // printf("Detected FPS: %.2f\n", gVideoProcessor.fps);
            }
            fclose(fpsFile);
        }
        remove("temp_fps.txt");
    } else {
        printf("WARNING: Failed to get video FPS, using default\n");
    }
    
    // Extract frames with FFmpeg
    printf("Starting FFmpeg frame extraction...\n");
    snprintf(ffmpegCmd, sizeof(ffmpegCmd), "ffmpeg -i \"%s\" \"%sframe_%%06d.png\" -y", 
            videoPath, gVideoProcessor.outputPath);
    
    printf("Executing FFmpeg command: %s\n", ffmpegCmd);
    result = system(ffmpegCmd);
    printf("FFmpeg command result: %d\n", result);
    
    if (result != 0) {
        printf("ERROR: FFmpeg failed with error code: %d\n", result);
        gVideoProcessor.hasError = true;
        snprintf(gVideoProcessor.errorMessage, sizeof(gVideoProcessor.errorMessage), 
                "FFmpeg error (code: %d)", result);
        return false;
    }
    
    // Wait for FFmpeg to finish and count frames once
    printf("FFmpeg extraction successful, waiting for completion...\n");
    sleep(2);
    
    // Count generated frames (once only)
    DIR* dir = opendir(gVideoProcessor.outputPath);
    int finalFrameCount = 0;
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL) {
            if (strstr(entry->d_name, "frame_") && strstr(entry->d_name, ".png")) {
                // Check file is not empty
                char fullPath[1024];
                snprintf(fullPath, sizeof(fullPath), "%s%s", gVideoProcessor.outputPath, entry->d_name);
                FILE* testFile = fopen(fullPath, "rb");
                if (testFile) {
                    fseek(testFile, 0, SEEK_END);
                    long size = ftell(testFile);
                    fclose(testFile);
                    if (size > 100) { // File must have at least 100 bytes
                        finalFrameCount++;
                    }
                }
            }
        }
        closedir(dir);
    }
    
    printf("Frame extraction completed: %d frames found\n", finalFrameCount);
    
    // Finalize processing
    gVideoProcessor.totalFrames = finalFrameCount;
    gVideoProcessor.processedFrames = finalFrameCount;
    printf("=== FINAL FRAME COUNT: %d ===\n", finalFrameCount);
    
    if (finalFrameCount > 0) {
        gVideoProcessor.isCompleted = true;
        gVideoProcessor.hasError = false;
        printf("*** VIDEO PROCESSING COMPLETED SUCCESSFULLY ***\n");
        printf("*** %d frames extracted ***\n", finalFrameCount);
        return true;
    } else {
        gVideoProcessor.hasError = true;
        strcpy(gVideoProcessor.errorMessage, "No frames extracted");
        printf("ERROR: No frames extracted\n");
        return false;
    }
}

bool StartVideoProcessing(const char* videoPath) {
    printf("=== STARTING VIDEO PROCESSING (SYNCHRONOUS) ===\n");
    printf("Video file: %s\n", videoPath);
    
    // Clean previous frames
    CleanupTempFrames();
    
    // Process video immediately
    return ProcessVideoSynchronous(videoPath);
}

bool IsFrameAvailable(int frameIndex) {
    if (frameIndex < 0) return false;
    
    char framePath[1024];
    snprintf(framePath, sizeof(framePath), "%sframe_%06d.png", gVideoProcessor.outputPath, frameIndex + 1);
    
    if (access(framePath, F_OK) != 0) {
        return false;
    }
    
    // Check file size
    FILE* testFile = fopen(framePath, "rb");
    if (testFile) {
        fseek(testFile, 0, SEEK_END);
        long fileSize = ftell(testFile);
        fclose(testFile);
        return fileSize > 100; // File must have at least 100 bytes
    }
    
    return false;
}

bool LoadSpecificFrame(int frameIndex, Image* frameImage) {
    if (frameIndex < 0) return false;
    
    char framePath[1024];
    snprintf(framePath, sizeof(framePath), "%sframe_%06d.png", gVideoProcessor.outputPath, frameIndex + 1);
    
    if (!IsFrameAvailable(frameIndex)) {
        return false;
    }
    
    *frameImage = LoadImage(framePath);
    return frameImage->data != NULL;
}

void GetVideoProcessingStatus(bool* isProcessing, bool* isCompleted, bool* hasError, char* errorMsg) {
    *isProcessing = false; // In synchronous mode, processing is never ongoing
    *isCompleted = gVideoProcessor.isCompleted;
    *hasError = gVideoProcessor.hasError;
    
    if (errorMsg && gVideoProcessor.hasError) {
        strcpy(errorMsg, gVideoProcessor.errorMessage);
    }
}

void CleanupVideoProcessor(void) {
    printf("Cleaning up video processor...\n");
    CleanupTempFrames();
    printf("Video processor cleanup completed\n");
}

void InitTextureBuffer(TextureBuffer* buffer, int capacity) {
    buffer->textures = (Texture2D*)calloc(capacity, sizeof(Texture2D));
    buffer->count = 0;
    buffer->capacity = capacity;
    printf("Texture buffer initialized with capacity: %d\n", capacity);
}

void FreeTextureBuffer(TextureBuffer* buffer) {
    if (buffer->textures) {
        for (int i = 0; i < buffer->count; i++) {
            if (buffer->textures[i].id > 0) {
                UnloadTexture(buffer->textures[i]);
            }
        }
        free(buffer->textures);
        buffer->textures = NULL;
        buffer->count = 0;
        buffer->capacity = 0;
        printf("Texture buffer freed\n");
    }
}

bool LoadTextureToBuffer(TextureBuffer* buffer, const Image* image, int index) {
    if (!buffer->textures || index >= buffer->capacity) {
        printf("ERROR: Invalid texture buffer or index\n");
        return false;
    }
    
    // If there's already a texture at this index, unload it
    if (index < buffer->count && buffer->textures[index].id > 0) {
        UnloadTexture(buffer->textures[index]);
    }
    
    // Load new texture
    buffer->textures[index] = LoadTextureFromImage(*image);
    
    // Update count if necessary
    if (index >= buffer->count) {
        buffer->count = index + 1;
    }
    
    printf("Texture loaded to buffer at index %d (ID: %d)\n", index, buffer->textures[index].id);
    return buffer->textures[index].id > 0;
}

Texture2D* GetTextureFromBuffer(TextureBuffer* buffer, int index) {
    if (!buffer->textures || index >= buffer->count || index < 0) {
        return NULL;
    }
    
    if (buffer->textures[index].id > 0) {
        return &buffer->textures[index];
    }
    
    return NULL;
}

void LoadAllAvailableFrames(Image** sequence, TextureBuffer* textureBuffer, int* totalFrames) {
    LogMessage("LOG LoadExtractedFrames called");
    printf("=== LOADING EXTRACTED FRAMES ===\n");
    
    printf("LoadExtractedFrames: checking state - completed=%d, hasError=%d, frameCount=%d\n", 
           gVideoProcessor.isCompleted, gVideoProcessor.hasError, gVideoProcessor.totalFrames);
    
    // Check processing is completed
    if (gVideoProcessor.hasError) {
        printf("LoadExtractedFrames failed: hasError=%d, error='%s'\n", 
               gVideoProcessor.hasError, gVideoProcessor.errorMessage);
        LogMessage("LOG LoadExtractedFrames failed - has error");
        return;
    }
    
    int totalExpectedFrames = gVideoProcessor.totalFrames;
    
    // Count currently available frames
    int availableFrames = 0;
    for (int i = 0; i < 15000; i++) {
        if (IsFrameAvailable(i)) {
            availableFrames = i + 1;
        } else {
            break;
        }
    }
    
    printf("Found %d available frames (expected: %d)\n", availableFrames, totalExpectedFrames);
    LogMessage("LOG Counted available frames");
    
    if (availableFrames <= 0) {
        LogMessage("LOG No frames available to load");
        return;
    }
    
    // Allocate memory for frames
    *sequence = (Image*)calloc(15000, sizeof(Image));
    if (*sequence == NULL) {
        LogMessage("LOG Failed to allocate memory for frames");
        return;
    }
    
    // Initialize texture buffer
    InitTextureBuffer(textureBuffer, 15000);
    
    LogMessage("LOG Memory allocated for frames and texture buffer");
    
    // Load available frames and their textures
    int loadedFrames = 0;
    int failedTextures = 0;
    
    for (int i = 0; i < availableFrames; i++) {
        if (LoadSpecificFrame(i, &(*sequence)[i])) {
            // Load texture into buffer
            if (LoadTextureToBuffer(textureBuffer, &(*sequence)[i], i)) {
                loadedFrames++;
                if (loadedFrames == 1) {
                    LogMessage("LOG First frame loaded successfully");
                    printf("*** FIRST FRAME LOADED - READY FOR DISPLAY ***\n");
                }
                // Show progress every 50 frames
                if (loadedFrames % 50 == 0) {
                    printf("Progress: %d/%d frames loaded (%.1f%%)\n", 
                           loadedFrames, availableFrames, 
                           (float)loadedFrames / availableFrames * 100.0f);
                }
            } else {
                printf("Failed to load texture for frame %d\n", i);
                failedTextures++;
                UnloadImage((*sequence)[i]);
                
                if (failedTextures > 10) {
                    printf("Too many texture failures (%d), stopping load\n", failedTextures);
                    break;
                }
            }
        } else {
            printf("Failed to load frame %d\n", i);
            LogMessage("LOG Failed to load specific frame");
            break;
        }
    }
    
    *totalFrames = loadedFrames;
    printf("Total frames loaded: %d/%d (%.1f%%) - Texture failures: %d\n", 
           loadedFrames, availableFrames, 
           (float)loadedFrames / availableFrames * 100.0f, failedTextures);
    LogMessage("LOG Frame loading completed");
    
    if (loadedFrames == 0) {
        free(*sequence);
        *sequence = NULL;
        FreeTextureBuffer(textureBuffer);
        LogMessage("LOG No frames loaded - cleaning up");
        return;
    }
    
    LogMessage("LOG LoadExtractedFrames returning true");
}
