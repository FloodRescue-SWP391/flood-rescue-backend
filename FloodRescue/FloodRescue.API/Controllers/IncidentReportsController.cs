using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.IncidentReport;
using FloodRescue.Services.DTO.Response.IncidentReport;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentReportsController : ControllerBase
    {
        private readonly IIncidentReportService _service;

        public IncidentReportsController(IIncidentReportService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<IncidentReportResponseDTO>>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return ApiResponse<List<IncidentReportResponseDTO>>.Ok(list, "Get incident reports successfully", 200);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<IncidentReportResponseDTO>>> Get(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return ApiResponse<IncidentReportResponseDTO>.Fail("Incident report not found", 404);
            return ApiResponse<IncidentReportResponseDTO>.Ok(item, "Get incident report successfully", 200);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<IncidentReportResponseDTO>>> Create(CreateIncidentReportRequestDTO request)
        {
            var result = await _service.CreateAsync(request);
            return ApiResponse<IncidentReportResponseDTO>.Ok(result, "Create incident report successfully", 201);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, CreateIncidentReportRequestDTO request)
        {
            var result = await _service.UpdateAsync(id, request);
            if (!result) return ApiResponse<bool>.Fail("Update failed", 400);
            return ApiResponse<bool>.Ok(true, "Update incident report successfully", 200);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return ApiResponse<bool>.Fail("Delete failed", 400);
            return ApiResponse<bool>.Ok(true, "Delete incident report successfully", 200);
        }
    }
}