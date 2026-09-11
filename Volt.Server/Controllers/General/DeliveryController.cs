using Application.Features.Delivery.DTOs;
using Application.Features.Delivery.Query.GetActiveDeliveriesLookupQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;
using System.Collections.Generic;

namespace Volt.Server.Controllers.General
{
    /// <summary>Shared delivery lookup (admin UI). No delivery mobile app payments here.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DeliveryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DeliveryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("active")]
        [ProducesResponseType(typeof(List<DeliveryLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetActive([FromQuery] int? cityId = null)
        {
            var result = await _mediator.Send(new GetActiveDeliveriesLookupQuery { CityId = cityId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
