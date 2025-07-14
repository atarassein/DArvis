#pragma once

#include "common.h"

class logger {
public:
    static void message(const std::string& message) {
        if (log_file_path_.empty()) {
            initialize();
        }
        
        std::ofstream log_file(log_file_path_, std::ios::app);
        if (log_file.is_open()) {
            log_file << "[" << GetTickCount() << "] " << message << '\n';
            log_file.close();
        }
    }

private:
    static void initialize() {
        log_file_path_ = "C:\\temp\\david_log.txt";
    }
    
    static std::string log_file_path_;
};
