using Application.Common;
using Application.Features.User.Command.ActivateUserCommand;
using Application.Features.User.Command.CreateUserCommand;
using Application.Features.User.Command.DeactivateUserCommand;
using Application.Features.User.Command.DeleteUserCommand;
using Application.Features.User.Command.UpdateUserCommand;
using Application.Features.User.Query.GetAllUsersQuery;
using Application.Features.User.Query.GetCurrentUserQuery;
using Application.Features.User.Query.GetUserByIdQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminUserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Application.Features.User.DTOs.UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("current")]
        [ProducesResponseType(typeof(Application.Features.User.DTOs.UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCurrent()
        {
            var query = new GetCurrentUserQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Application.Features.User.DTOs.UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var query = new GetUserByIdQuery(id);
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserCommand command)
        {
            command.UserId = id;
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var command = new DeleteUserCommand(id);
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpPost("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(int id)
        {
            var command = new ActivateUserCommand { UserId = id };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var command = new DeactivateUserCommand { UserId = id };
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }
    }
}


