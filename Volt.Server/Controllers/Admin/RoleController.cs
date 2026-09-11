using Application.Features.Roles.Command.AssignRoleToUserCommand;
using Application.Features.Roles.Command.CreateRoleCommand;
using Application.Features.Roles.Command.DeleteRoleCommand;
using Application.Features.Roles.Command.RemoveRoleFromUserCommand;
using Application.Features.Roles.Command.UpdateRoleCommand;
using Application.Features.Roles.DTOs;
using Application.Features.Roles.Query.GetAllRolesQuery;
using Application.Features.Roles.Query.GetRoleByIdQuery;
using Application.Features.Roles.Query.GetUserRolesQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RoleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("AllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _mediator.Send(new GetAllRolesQuery());
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetRoleById/{roleId}")]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            var result = await _mediator.Send(new GetRoleByIdQuery { RoleId = roleId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetUserRoles/{userId}")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var result = await _mediator.Send(new GetUserRolesQuery { UserId = userId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("CreateRole")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> CreateRole(CreateRoleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("UpdateRole")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> UpdateRole(UpdateRoleCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("DeleteRole/{roleId}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var result = await _mediator.Send(new DeleteRoleCommand { RoleId = roleId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("AssignRoleToUser")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> AssignRoleToUser(AssignRoleToUserCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("RemoveRoleFromUser")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> RemoveRoleFromUser(RemoveRoleFromUserCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

