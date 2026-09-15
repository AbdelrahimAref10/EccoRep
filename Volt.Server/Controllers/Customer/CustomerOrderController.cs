using Application.Common;
using Application.Features.Order.Command.CancelOrderCommand;
using Application.Features.Order.Command.CreateOrderCommand;
using Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand;
using Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand.DTOs;
using Application.Features.Order.DTOs;
using Application.Features.Order.Query.GetCityFeesQuery;
using Application.Features.Order.Query.GetCustomerAvailableVehiclesQuery;
using Application.Features.Order.Query.GetCustomerOrderDeliveryFeesQuery;
using Application.Features.Order.Query.GetCustomerOrdersQuery;
using Application.Features.Order.Query.GetReservedVehiclePerSubCategoryQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Customer
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerOrderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomerOrderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("CityFees")]
        [ProducesResponseType(typeof(CityFeesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCityFees()
        {
            var query = new GetCityFeesQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        /// <summary>
        /// Vehicles in subcategory for date range (excludes UnderMaintenance).
        /// Status: Available=0 / Reserved=1, plus ConflictingDates for reserved vehicles.
        /// </summary>
        [HttpGet("AvailableVehicles")]
        [ProducesResponseType(typeof(List<CustomerAvailableVehicleItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableVehicles([FromQuery] GetCustomerAvailableVehiclesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("DeliveryFees")]
        [ProducesResponseType(typeof(CustomerOrderDeliveryFeesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryFees([FromBody] GetCustomerOrderDeliveryFeesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("ReservedVehiclePerSubCategory")]
        [ProducesResponseType(typeof(List<ReservedDateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReservedVehiclePerSubCategory([FromQuery] int subCategoryId)
        {
            var query = new GetReservedVehiclePerSubCategoryQuery { SubCategoryId = subCategoryId };
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
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("MyOrders")]
        [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyOrders([FromQuery] GetCustomerOrdersQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{orderId}/Cancel")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var command = new CancelOrderCommand { OrderId = orderId };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("CompletePayPalPayment")]
        [ProducesResponseType(typeof(CompletePayPalPaymentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompletePayPalPayment([FromBody] CompletePayPalPaymentRequestDto request)
        {
            var command = new CompletePayPalPaymentCommand
            {
                OrderId = request.OrderId,
                PayPalOrderId = request.PayPalOrderId
            };

            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}
