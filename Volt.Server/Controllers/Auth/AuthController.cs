using Application.Features.Auth.Command.ActivateAccountCommand;
using Application.Features.Auth.Command.ForgetPasswordCommand;
using Application.Features.Auth.Command.LoginCommand;
using Application.Features.Auth.Command.RefreshTokenCommand;
using Application.Features.Auth.Command.RegisterCommand;
using Application.Features.Auth.Command.ResendActivationCodeCommand;
using Application.Features.Auth.Command.ResetPasswordCommand;
using Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("Register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("RefreshToken")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("ForgetPassword")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("ResetPassword")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("Activate")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate([FromBody] ActivateAccountCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpPost("ResendActivationCode")]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendActivationCode([FromBody] ResendActivationCodeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }
    }
}
