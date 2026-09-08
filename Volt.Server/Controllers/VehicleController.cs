using Application.Common;
using Application.Features.Vehicle.Command.CreateVehicleCommand;
using Application.Features.Vehicle.Command.DeleteVehicleCommand;
using Application.Features.Vehicle.Command.UpdateVehicleCommand;
using Application.Features.Vehicle.DTOs;
using Application.Features.Vehicle.Query.GetAllVehiclesQuery;
using Application.Features.Vehicle.Query.GetVehicleByIdQuery;
using Application.Features.Vehicle.Query.GetVehicleStatisticsQuery;
using Application.Features.Vehicle.Query.GetVehiclesByCategoryQuery;
using Application.Features.Vehicle.Query.GetVehiclesBySubCategoryQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VehicleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllVehiclesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(VehicleStatisticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStatistics([FromQuery] int? categoryId, [FromQuery] int? subCategoryId)
        {
            var result = await _mediator.Send(new GetVehicleStatisticsQuery { CategoryId = categoryId, SubCategoryId = subCategoryId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByCategory(int categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _mediator.Send(new GetVehiclesByCategoryQuery 
            { 
                CategoryId = categoryId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("by-subcategory/{subCategoryId}")]
        [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBySubCategory(int subCategoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _mediator.Send(new GetVehiclesBySubCategoryQuery 
            { 
                SubCategoryId = subCategoryId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetVehicleByIdQuery { VehicleId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut]
        [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateVehicleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteVehicleCommand { VehicleId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

