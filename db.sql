CREATE TABLE file_changes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    file_path VARCHAR(255) NOT NULL,
    action VARCHAR(50) NOT NULL,
    process_name VARCHAR(255) NOT NULL, -- Lưu ứng dụng mở file
    timestamp DATETIME NOT NULL
);
