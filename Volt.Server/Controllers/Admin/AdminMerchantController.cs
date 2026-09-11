using Application.Features.Merchant.Command.AdminCreateMerchantCommand;
using Application.Features.Merchant.Command.AdminSoftDeleteMerchantCommand;
using Application.Features.Merchant.Command.AdminUpdateMerchantCommand;
using Application.Features.Merchant.Command.SetMerchantCashOnReceiveCommand;
using Application.Features.Merchant.DTOs;
using Application.Features.Merchant.Query.GetActiveMerchantsLookupQuery;
using Application.Features.Merchant.Query.GetAllMerchantsQuery;
using Application.Features.Merchant.Query.GetMerchantByIdQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;
using System.Collections.Generic;

namespace Volt.Server.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminMerchantController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminMerchantController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<MerchantDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllMerchantsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("active")]
        [ProducesResponseType(typeof(List<MerchantLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetActive()
        {
            var result = await _mediator.Send(new GetActiveMerchantsLookupQuery());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("{merchantId:int}")]
        [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int merchantId)
        {
            var result = await _mediator.Send(new GetMerchantByIdQuery { MerchantId = merchantId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] AdminCreateMerchantCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPut("{merchantId:int}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int merchantId, [FromBody] AdminUpdateMerchantCommand command)
        {
            command.MerchantId = merchantId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>Soft delete — sets IsDeleted=true; row stays in DB.</summary>
        [HttpDelete("{merchantId:int}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int merchantId)
        {
            var result = await _mediator.Send(new AdminSoftDeleteMerchantCommand { MerchantId = merchantId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPut("{merchantId:int}/CashOnReceive")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetCashOnReceive(int merchantId, [FromBody] SetMerchantCashOnReceiveCommand command)
        {
            command.MerchantId = merchantId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
