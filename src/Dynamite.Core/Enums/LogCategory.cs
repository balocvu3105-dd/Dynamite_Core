// src/Dynamite.Core/Enums/LogCategory.cs
namespace Dynamite.Core.Enums;
public enum LogCategory
{
    Message = 0,
    Member = 1,
    Voice = 2,
    Server = 3,
    Audit = 4   // Immutable audit trail — owner/dev only
}