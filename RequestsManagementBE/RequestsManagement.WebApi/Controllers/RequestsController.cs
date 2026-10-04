using Microsoft.AspNetCore.Mvc;
using RequestsManagement.BL.Interfaces;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestBL _requestBL;

        public RequestsController(IRequestBL requestBL)
        {
            _requestBL = requestBL;
        }

        /// <summary>
        /// שליפת פניות עם סינון, חיפוש, מיון ודפדוף (צד שרת).
        /// GET /api/requests?page=1&pageSize=20&status=New&search=...&sortBy=createdat&sortDescending=true
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] RequestFilterDTO filter, CancellationToken cancellationToken)
        {
            var result = await _requestBL.GetRequestsAsync(filter, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// נתונים מסכמים (Aggregations) - ספירה לפי סטטוס ולפי עדיפות.
        /// GET /api/requests/stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            var result = await _requestBL.GetStatsAsync(cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// עדכון סטטוס של פנייה בודדת עם אכיפת Optimistic Concurrency.
        /// PATCH /api/requests/{id}/status
        /// 200 OK / 404 NotFound / 400 Validation / 409 Conflict
        /// </summary>
        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            var result = await _requestBL.UpdateStatusAsync(id, dto, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// צפייה בהיסטוריית שינויי הסטטוס של פנייה (Audit).
        /// GET /api/requests/{id}/history
        /// </summary>
        [HttpGet("{id:long}/history")]
        public async Task<IActionResult> GetHistory(long id, CancellationToken cancellationToken)
        {
            var result = await _requestBL.GetHistoryAsync(id, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// עדכון סטטוס מרובה (Bulk) - עד 100 פניות, בגישת Partial Success.
        /// POST /api/requests/bulk-status
        /// מחזיר 200 עם פירוט succeeded/failed לכל פנייה.
        /// </summary>
        [HttpPost("bulk-status")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            var result = await _requestBL.BulkUpdateStatusAsync(dto, cancellationToken);
            return Ok(result);
        }
    }
}
