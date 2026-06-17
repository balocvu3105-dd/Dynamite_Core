// src/Dynamite.Core/Entities/SpecialPool.cs
namespace Dynamite.Core.Entities;

public enum SpecialDropTable
{
    CoralBay,    // Vịnh San Hô — tôm, mực, sứa
    DeepOcean,   // Đáy Đại Dương — bạch tuột, cá mập, mắt biển
    MangroveForest, // Rừng Ngập Mặn — cá hiếm vùng cửa sông
    AbyssalZone  // Vực Thẳm — sinh vật phát sáng, nguy hiểm nhất
}

/// <summary>
/// Pool đặc biệt xuất hiện theo lịch hybrid (ngày chẵn guaranteed 1,
/// ngày lẻ random 0–2). User phải Fishing Level ≥ 20 để tham gia.
/// Capacity 2000 cá, có thời gian sống 2–4 tiếng.
/// </summary>
public class SpecialPool : BaseEntity
{
    public ulong GuildId { get; set; }
    public string PoolName { get; set; } = string.Empty;
    public SpecialDropTable DropTable { get; set; }

    public int Capacity      { get; set; } = 2000;
    public int RemainingFish { get; set; } = 2000;

    /// <summary>Level tối thiểu để câu pool này.</summary>
    public int MinLevel { get; set; } = 20;

    public DateTime StartsAt  { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsActive => DateTime.UtcNow >= StartsAt && DateTime.UtcNow < ExpiresAt && RemainingFish > 0;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt || RemainingFish <= 0;
}
