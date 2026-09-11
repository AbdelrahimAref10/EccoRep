using Application.Features.AdminNotification.Command.MarkAdminNotificationAsReadCommand;
using Application.Features.AdminNotification.Command.MarkAllAdminNotificationsAsReadCommand;
using Application.Features.AdminNotification.Query.GetAdminNotificationsQuery;
using Application.Features.AdminNotification.Query.GetUnreadAdminNotificationsCountQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Super Admin")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminNotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<Application.Features.AdminNotification.DTOs.AdminNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead = null, [FromQuery] int? skip = null, [FromQuery] int? take = null)
        {
            var query = new GetAdminNotificationsQuery
            {
                IsRead = isRead,
                Skip = skip,
                Take = take
            };

            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("UnreadCount")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var query = new GetUnreadAdminNotificationsCountQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{id}/MarkAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var command = new MarkAdminNotificationAsReadCommand
            {
                AdminNotificationId = id
            };

            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpPost("MarkAllAsRead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _mediator.Send(new MarkAllAdminNotificationsAsReadCommand());
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }
    }
}

