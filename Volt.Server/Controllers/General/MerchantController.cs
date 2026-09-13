using Application.Common;
using Application.Features.Category.DTOs;
using Application.Features.Category.Query.GetCategoriesLookupQuery;
using Application.Features.Merchant.Command.MerchantCreateVehicleCommand;
using Application.Features.Merchant.Command.MerchantDeleteVehicleCommand;
using Application.Features.Merchant.Command.MerchantUpdateVehicleCommand;
using Application.Features.Merchant.DTOs;
using Application.Features.Merchant.Query.GetActiveMerchantsLookupQuery;
using Application.Features.Merchant.Query.GetMyMerchantProfileQuery;
using Application.Features.Merchant.Query.GetMyMerchantVehicleByIdQuery;
using Application.Features.Merchant.Query.GetMyMerchantVehiclesQuery;
using Application.Features.MerchantNotification.Command.MarkAllMyMerchantNotificationsAsReadCommand;
using Application.Features.MerchantNotification.Command.MarkMerchantNotificationAsReadCommand;
using Application.Features.MerchantNotification.DTOs;
using Application.Features.MerchantNotification.Query.GetMyMerchantNotificationsQuery;
using Application.Features.MerchantNotification.Query.GetMyMerchantUnreadNotificationsCountQuery;
using Application.Features.Order.Command.AcceptMerchantOrderCommand;
using Application.Features.Order.Command.MarkMerchantHandoverToDeliveryCommand;
using Application.Features.Order.Command.RejectMerchantOrderCommand;
using Application.Features.Order.DTOs;
using Application.Features.Order.Query.GetMyMerchantDashboardQuery;
using Application.Features.Order.Query.GetMyMerchantJournalsQuery;
using Application.Features.Order.Query.GetMyMerchantLedgerQuery;
using Application.Features.Order.Query.GetMyMerchantOrderDetailQuery;
using Application.Features.Order.Query.GetMyMerchantOrdersQuery;
using Application.Features.SubCategory.DTOs;
using Application.Features.SubCategory.Query.GetSubCategoriesByCategoryQuery;
using Application.Features.Vehicle.DTOs;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;
using System.Collections.Generic;

namespace Volt.Server.Controllers.General
{
    /// <summary>
    /// Merchant portal APIs. Session merchant is always resolved from the logged-in user.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MerchantController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MerchantController(IMediator mediator)
        {
            _mediator = mediator;
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

        [HttpGet("me")]
        [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _mediator.Send(new GetMyMerchantProfileQuery());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("notifications")]
        [ProducesResponseType(typeof(List<MerchantNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] bool? isRead = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null)
        {
            var result = await _mediator.Send(new GetMyMerchantNotificationsQuery
            {
                IsRead = isRead,
                Skip = skip,
                Take = take
            });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("notifications/UnreadCount")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyUnreadNotificationCount()
        {
            var result = await _mediator.Send(new GetMyMerchantUnreadNotificationsCountQuery());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("notifications/{id}/MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var result = await _mediator.Send(new MarkMerchantNotificationAsReadCommand
            {
                MerchantNotificationId = id
            });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok();
        }

        [HttpPost("notifications/MarkAllAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var result = await _mediator.Send(new MarkAllMyMerchantNotificationsAsReadCommand());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok();
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(MerchantDashboardSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _mediator.Send(new GetMyMerchantDashboardQuery());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        // ── Catalog lookups (read-only; merchant cannot create category/subcategory) ──

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCategories()
        {
            var profile = await _mediator.Send(new GetMyMerchantProfileQuery());
            if (profile.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(profile.Error));

            var result = await _mediator.Send(new GetCategoriesLookupQuery { CityId = profile.Value.CityId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("categories/{categoryId}/subcategories")]
        [ProducesResponseType(typeof(List<SubCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSubCategoriesByCategory(int categoryId)
        {
            var profile = await _mediator.Send(new GetMyMerchantProfileQuery());
            if (profile.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(profile.Error));

            var categories = await _mediator.Send(new GetCategoriesLookupQuery { CityId = profile.Value.CityId });
            if (categories.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(categories.Error));

            if (categories.Value.All(c => c.CategoryId != categoryId))
                return BadRequest(ProblemDetail.CreateProblemDetail("Category is not available in your city"));

            var result = await _mediator.Send(new GetSubCategoriesByCategoryQuery { CategoryId = categoryId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        // ── Vehicles (own fleet only) ──

        [HttpGet("vehicles")]
        [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyVehicles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? subCategoryId = null)
        {
            var result = await _mediator.Send(new GetMyMerchantVehiclesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId
            });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("vehicles/{vehicleId}")]
        [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyVehicle(int vehicleId)
        {
            var result = await _mediator.Send(new GetMyMerchantVehicleByIdQuery { VehicleId = vehicleId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("vehicles")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateVehicle([FromBody] MerchantCreateVehicleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPut("vehicles")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateVehicle([FromBody] MerchantUpdateVehicleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpDelete("vehicles/{vehicleId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteVehicle(int vehicleId)
        {
            var result = await _mediator.Send(new MerchantDeleteVehicleCommand { VehicleId = vehicleId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        // ── Orders ──

        [HttpGet("orders")]
        [ProducesResponseType(typeof(PagedResult<MerchantPortalOrderListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] OrderState? state = null,
            [FromQuery] string? orderCode = null,
            [FromQuery] MerchantOrderResponseStatus? myResponseStatus = null,
            [FromQuery] bool? pendingOnly = null,
            [FromQuery] bool? awaitingHandoverOnly = null)
        {
            var result = await _mediator.Send(new GetMyMerchantOrdersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                State = state,
                OrderCode = orderCode,
                MyResponseStatus = myResponseStatus,
                PendingOnly = pendingOnly,
                AwaitingHandoverOnly = awaitingHandoverOnly
            });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("orders/{orderId}")]
        [ProducesResponseType(typeof(MerchantPortalOrderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyOrder(int orderId)
        {
            var result = await _mediator.Send(new GetMyMerchantOrderDetailQuery { OrderId = orderId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("ledger")]
        [ProducesResponseType(typeof(PartyLedgerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyLedger()
        {
            var result = await _mediator.Send(new GetMyMerchantLedgerQuery());
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("journals")]
        [ProducesResponseType(typeof(OrderJournalListDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyJournals([FromQuery] int? orderId = null)
        {
            var result = await _mediator.Send(new GetMyMerchantJournalsQuery { OrderId = orderId });
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("orders/{orderId}/Accept")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptOrder(int orderId, [FromBody] AcceptMerchantOrderCommand? command)
        {
            command ??= new AcceptMerchantOrderCommand();
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("orders/{orderId}/Reject")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectOrder(int orderId, [FromBody] RejectMerchantOrderCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("orders/{orderId}/Handover")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandoverToDelivery(int orderId, [FromBody] MarkMerchantHandoverToDeliveryCommand command)
        {
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
