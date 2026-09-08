using Application.Common;
using Application.Features.Order.Command.AdminCreateOrderCommand;
using Application.Features.Order.Command.AdminUpdateOrderCommand;
using Application.Features.Order.Command.MarkOrderCancellationFeePaidCommand;
using Application.Features.Order.Command.MarkOrderMoneyRefundedCommand;
using Application.Features.Order.Command.RejectOrderCommand;
using Application.Features.Order.Command.UpdateOrderStateCommand;
using Application.Features.Order.DTOs;
using Application.Features.Order.Query.AdminCalculateOrderTotalsQuery;
using Application.Features.Order.Query.GetAdminAvailableVehiclesQuery;
using Application.Features.Order.Query.GetAllOrdersQuery;
using Application.Features.Order.Query.GetOrderByIdQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminOrderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminOrderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAllOrders([FromQuery] GetAllOrdersQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var query = new GetOrderByIdQuery { OrderId = id };
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("AvailableVehicles")]
        [ProducesResponseType(typeof(AdminAvailableVehiclesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableVehicles([FromQuery] GetAdminAvailableVehiclesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("CalculateTotals")]
        [ProducesResponseType(typeof(AdminOrderTotalsPreviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CalculateTotals([FromBody] AdminCalculateOrderTotalsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] AdminCreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut("{orderId}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] AdminUpdateOrderCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/UpdateState")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOrderState(int orderId, [FromBody] UpdateOrderStateCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/RejectOrder")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectOrder(int orderId)
        {
            var command = new RejectOrderCommand { OrderId = orderId };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/MarkMoneyRefunded")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkMoneyRefunded(int orderId)
        {
            var command = new MarkOrderMoneyRefundedCommand { OrderId = orderId };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/CancellationFee/MarkPaid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkOrderCancellationFeePaid(int orderId)
        {
            var command = new MarkOrderCancellationFeePaidCommand { OrderId = orderId };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

