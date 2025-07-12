#include "../include/common.h"
#include <stdio.h>
#include <string.h>
#include <time.h>

// Global logger instance
LogManager gLogManager = {0};

void InitLogger(void) {
    #if !ENABLE_LOGGING
    return;
    #endif
    gLogManager.logFile = fopen("debug.log", "w");
    if (gLogManager.logFile) {
        fprintf(gLogManager.logFile, "=== LOG START ===\n");
        fflush(gLogManager.logFile);
    }
    gLogManager.messageCount = 0;
    gLogManager.writeIndex = 0;
    gLogManager.consecutiveCount = 0;
    gLogManager.lastMessage[0] = '\0';
}

void FlushConsecutiveMessages(void) {
    #if !ENABLE_LOGGING
    return;
    #endif
    
    if (gLogManager.consecutiveCount > 1 && gLogManager.logFile) {
        fprintf(gLogManager.logFile, "    (repeated %d times)\n", gLogManager.consecutiveCount - 1);
        fflush(gLogManager.logFile);
    }
    gLogManager.consecutiveCount = 0;
}

void LogMessage(const char* message) {
    #if !ENABLE_LOGGING
    return;
    #endif
    
    if (!gLogManager.logFile) return;
    
    // Check if this is the same message as the last one
    if (strcmp(message, gLogManager.lastMessage) == 0) {
        gLogManager.consecutiveCount++;
        return; // Don't write the same message multiple times
    }
    
    // If we had consecutive messages, flush them first
    FlushConsecutiveMessages();
    
    // Log the new message with timestamp
    time_t now = time(NULL);
    struct tm* timeinfo = localtime(&now);
    char timeStr[64];
    strftime(timeStr, sizeof(timeStr), "%H:%M:%S", timeinfo);
    
    fprintf(gLogManager.logFile, "[%s] %s\n", timeStr, message);
    fflush(gLogManager.logFile);
    
    // Store this message for consecutive detection
    strncpy(gLogManager.lastMessage, message, MAX_MESSAGE_LENGTH - 1);
    gLogManager.lastMessage[MAX_MESSAGE_LENGTH - 1] = '\0';
    gLogManager.consecutiveCount = 1;
}

void CloseLogger(void) {
    if (gLogManager.logFile) {
        FlushConsecutiveMessages();
        fprintf(gLogManager.logFile, "=== LOG END ===\n");
        fclose(gLogManager.logFile);
    }
}
