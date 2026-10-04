using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.BL.Mapping
{
    /// <summary>
    /// המרה בין ישויות ה-DB ל-DTOs. מרוכז במקום אחד כדי לשמור על עקביות.
    /// </summary>
    public static class RequestMapper
    {
        public static RequestListItemDTO ToListItemDTO(Request entity)
        {
            return new RequestListItemDTO
            {
                Id = entity.Id,
                Title = entity.Title,
                OrganizationName = entity.OrganizationName,
                Status = entity.Status,
                Priority = entity.Priority,
                AssignedTo = entity.AssignedTo,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RowVersion = EncodeRowVersion(entity.RowVersion)
            };
        }

        public static RequestDTO ToDTO(Request entity)
        {
            return new RequestDTO
            {
                Id = entity.Id,
                Title = entity.Title,
                OrganizationName = entity.OrganizationName,
                Status = entity.Status,
                Priority = entity.Priority,
                AssignedTo = entity.AssignedTo,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RowVersion = EncodeRowVersion(entity.RowVersion)
            };
        }

        public static StatusHistoryDTO ToHistoryDTO(RequestStatusHistory entity)
        {
            return new StatusHistoryDTO
            {
                Id = entity.Id,
                RequestId = entity.RequestId,
                PreviousStatus = entity.PreviousStatus,
                NewStatus = entity.NewStatus,
                ChangedAt = entity.ChangedAt,
                ChangedBy = entity.ChangedBy
            };
        }

        /// <summary>
        /// RowVersion (byte[]) מקודד ל-Base64 כדי לעבור נקי ב-JSON מול ה-client.
        /// </summary>
        public static string EncodeRowVersion(byte[] rowVersion)
        {
            return rowVersion.Length == 0 ? string.Empty : Convert.ToBase64String(rowVersion);
        }

        /// <summary>
        /// פענוח RowVersion מ-Base64 בחזרה ל-byte[] (בעדכון מתחרה).
        /// </summary>
        public static byte[] DecodeRowVersion(string rowVersion)
        {
            return string.IsNullOrEmpty(rowVersion) ? Array.Empty<byte>() : Convert.FromBase64String(rowVersion);
        }
    }
}
