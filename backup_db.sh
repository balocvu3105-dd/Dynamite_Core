#!/bin/bash
# Kịch bản tự động sao lưu (backup) cơ sở dữ liệu Dynamite Core
# Được chạy tự động bởi Cron mỗi 7 ngày (1 tuần 1 lần)

# 1. Tạo thư mục lưu backup nếu chưa tồn tại
BACKUP_DIR="/root/Dynamite_Core/backups"
mkdir -p "$BACKUP_DIR"

# 2. Tạo tên file backup kèm ngày giờ (VD: dynamite_db_2026-07-06_23-00.sql.gz)
TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
BACKUP_FILE="$BACKUP_DIR/dynamite_db_$TIMESTAMP.sql.gz"

echo "[$(date)] Bắt đầu sao lưu cơ sở dữ liệu Dynamite Core..."

# 3. Thực hiện xuất dữ liệu từ container dynamite_postgres và nén lại bằng gzip
if docker exec -i dynamite_postgres pg_dump -U dynamite -d dynamite_core | gzip > "$BACKUP_FILE"; then
    echo "[$(date)] -> Sao lưu THÀNH CÔNG: $BACKUP_FILE"
    
    # 4. Tự động xóa các file backup cũ hơn 60 ngày (khoảng 8 bản backup gần nhất) để tránh đầy ổ cứng
    find "$BACKUP_DIR" -name "dynamite_db_*.sql.gz" -type f -mtime +60 -delete
    echo "[$(date)] -> Đã dọn dẹp các bản sao lưu cũ hơn 60 ngày."
else
    echo "[$(date)] -> LỖI: Sao lưu thất bại!" >&2
    exit 1
fi
