using Application.Common;
using Application.Features.Order.Command.AdminCreateOrderCommand;
using Application.Features.Order.Command.AdminUpdateOrderCommand;
using Application.Features.Order.Command.AssignDeliveryToOrderCommand;
using Application.Features.Order.Command.DeliveryRemittanceToCompanyCommand;
using Application.Features.Order.Command.MarkCustomerRejectedReceiptCommand;
using Application.Features.Order.Command.MarkMerchantHandoverToDeliveryCommand;
using Application.Features.Order.Command.MarkOrderCancellationFeePaidCommand;
using Application.Features.Order.Command.MarkOrderMoneyRefundedCommand;
using Application.Features.Order.Command.AdminReplacementOrderVehicleCommand;
using Application.Features.Order.Command.AdminRemoveOrderVehicleCommand;
using Application.Features.Order.Command.OrderVehicleLifecycleCommands;
using Application.Features.Order.Command.ReassignMerchantOrderCommand;
using Application.Features.Order.Command.RejectOrderCommand;
using Application.Features.Order.Command.SendOrderToMerchantsCommand;
using Application.Features.Order.Command.SettleDeliveryPayoutCommand;
using Application.Features.Order.Command.SettleMerchantPayoutCommand;
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

namespace Volt.Server.Controllers.Admin
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
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _mediator.Send(new GetOrderByIdQuery { OrderId = id });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("AvailableVehicles")]
        [ProducesResponseType(typeof(AdminAvailableVehiclesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableVehicles([FromQuery] GetAdminAvailableVehiclesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("CalculateTotals")]
        [ProducesResponseType(typeof(AdminOrderTotalsPreviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CalculateTotals([FromBody] AdminCalculateOrderTotalsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] AdminCreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
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
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
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
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/SendToMerchants")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendToMerchants(int orderId, [FromBody] SendOrderToMerchantsCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/ReassignMerchant")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReassignMerchant(int orderId, [FromBody] ReassignMerchantOrderCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>Replacement of one order vehicle with another available in the reservation range. Recalculates order totals.</summary>
        [HttpPost("{orderId}/Replacement")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Replacement(int orderId, [FromBody] AdminReplacementOrderVehicleCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        /// <summary>Remove one vehicle from the order and recalculate totals/payment.</summary>
        [HttpPost("{orderId}/RemoveVehicle")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveVehicle(int orderId, [FromBody] AdminRemoveOrderVehicleCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/AssignDelivery")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignDelivery(int orderId, [FromBody] AssignDeliveryToOrderCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/MarkMerchantHandover")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkMerchantHandover(int orderId, [FromBody] MarkMerchantHandoverToDeliveryCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/vehicles/{vehicleId}/ReceivedFromOwner")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkVehicleReceivedFromOwner(
            int orderId,
            int vehicleId,
            [FromBody] MarkVehicleReceivedFromOwnerCommand? command)
        {
            command ??= new MarkVehicleReceivedFromOwnerCommand();
            command.OrderId = orderId;
            command.VehicleId = vehicleId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/vehicles/{vehicleId}/DeliveredToCustomer")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkVehicleDeliveredToCustomer(
            int orderId,
            int vehicleId,
            [FromBody] MarkVehicleDeliveredToCustomerCommand? command)
        {
            command ??= new MarkVehicleDeliveredToCustomerCommand();
            command.OrderId = orderId;
            command.VehicleId = vehicleId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/vehicles/{vehicleId}/ReceivedFromCustomer")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkVehicleReceivedFromCustomer(
            int orderId,
            int vehicleId,
            [FromBody] MarkVehicleReceivedFromCustomerCommand? command)
        {
            command ??= new MarkVehicleReceivedFromCustomerCommand();
            command.OrderId = orderId;
            command.VehicleId = vehicleId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/vehicles/{vehicleId}/DeliveredToOwner")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkVehicleDeliveredToOwner(
            int orderId,
            int vehicleId,
            [FromBody] MarkVehicleDeliveredToOwnerCommand? command)
        {
            command ??= new MarkVehicleDeliveredToOwnerCommand();
            command.OrderId = orderId;
            command.VehicleId = vehicleId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/vehicles/{vehicleId}/NotReceivedByCustomer")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkVehicleNotReceivedByCustomer(
            int orderId,
            int vehicleId,
            [FromBody] MarkVehicleNotReceivedByCustomerCommand command)
        {
            command.OrderId = orderId;
            command.VehicleId = vehicleId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/NotDelivered")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkOrderNotDelivered(int orderId, [FromBody] MarkOrderNotDeliveredCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/RejectReceipt")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectReceipt(int orderId, [FromBody] MarkCustomerRejectedReceiptCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/DeliveryRemittance")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeliveryRemittance(int orderId, [FromBody] DeliveryRemittanceToCompanyCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/SettleMerchant")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SettleMerchant(int orderId, [FromBody] SettleMerchantPayoutCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/SettleDelivery")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SettleDelivery(int orderId, [FromBody] SettleDeliveryPayoutCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/RejectOrder")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectOrder(int orderId)
        {
            var result = await _mediator.Send(new RejectOrderCommand { OrderId = orderId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/MarkMoneyRefunded")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkMoneyRefunded(int orderId)
        {
            var result = await _mediator.Send(new MarkOrderMoneyRefundedCommand { OrderId = orderId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/CancellationFee/MarkPaid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkOrderCancellationFeePaid(int orderId)
        {
            var result = await _mediator.Send(new MarkOrderCancellationFeePaidCommand { OrderId = orderId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
