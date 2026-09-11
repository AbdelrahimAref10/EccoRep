using Application.Features.Order.Command.AdminCollectFromDeliveryCommand;
using Application.Features.Order.Command.AdminPayDeliveryCommand;
using Application.Features.Order.Command.AdminPayMerchantCommand;
using Application.Features.Order.DTOs;
using Application.Features.Order.Query.GetAllOrderJournalsQuery;
using Application.Features.Order.Query.GetPartyLedgerQuery;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Admin
{
    /// <summary>
    /// Admin settlement hub APIs: pay delivery, collect from delivery, pay merchant.
    /// Frontend: one "تسويات" screen with 3 tabs calling these endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminSettlementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminSettlementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// All journal movements for admin. Optional filters: orderId, deliveryId, merchantId.
        /// Totals (credit/debit/balance) are over the filtered visible set. Balance = credit − debit.
        /// </summary>
        [HttpGet("journals")]
        [ProducesResponseType(typeof(OrderJournalListDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetJournals([FromQuery] GetAllOrderJournalsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>Ledger timeline + balance for a delivery or merchant (includes float rows).</summary>
        [HttpGet("ledger")]
        [ProducesResponseType(typeof(PartyLedgerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLedger([FromQuery] LedgerPartyType partyType, [FromQuery] int partyId)
        {
            var result = await _mediator.Send(new GetPartyLedgerQuery
            {
                PartyType = partyType,
                PartyId = partyId
            });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>
        /// ادفع للدليفري — Kind: CashFloat (عُهدة بدون أوردر) أو OrderPayout (سداد مستحق على أوردر).
        /// </summary>
        [HttpPost("PayDelivery")]
        [ProducesResponseType(typeof(SettlementResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayDelivery([FromBody] AdminPayDeliveryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>
        /// اقبض من الدليفري — Kind: OrderCashRemittance أو FloatReturn.
        /// </summary>
        [HttpPost("CollectFromDelivery")]
        [ProducesResponseType(typeof(SettlementResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CollectFromDelivery([FromBody] AdminCollectFromDeliveryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>ادفع للميرشانت — سداد مستحق إيجار على أوردر.</summary>
        [HttpPost("PayMerchant")]
        [ProducesResponseType(typeof(SettlementResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayMerchant([FromBody] AdminPayMerchantCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
