using Application.Features.City.DTOs;
using Application.Features.City.Query.GetCitiesLookupQuery;
using Application.Features.Zone.Query.GetZonesByCityQuery;
using Application.Features.Customer.Command.DeleteCustomerAccountCommand;
using Application.Features.Customer.Command.SaveCustomerLocationCommand;
using Application.Features.Customer.Command.SaveFireBaseTokensForCustomerCommand;
using Application.Features.Customer.Command.UpdateCustomerProfileCommand;
using Application.Features.Customer.DTOs;
using Application.Features.Customer.Query.GetCustomerInfoQuery;
using Application.Features.Customer.Query.GetCustomerLocationQuery;
using Application.Features.Customer.Query.GetCustomerNotificationsQuery;
using Application.Features.Support.DTOs;
using Application.Features.Support.Query.GetSupportQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volt.Server.Controllers.Customer
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CityLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetActiveCities")]
        public async Task<IActionResult> GetActiveCities()
        {
            var query = new GetCitiesLookupQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("GetZonesByCity")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ZoneLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetZonesByCity([FromQuery] int cityId)
        {
            var result = await _mediator.Send(new GetZonesByCityQuery { CityId = cityId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SupportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetSupport")]
        public async Task<IActionResult> GetSupport()
        {
            var result = await _mediator.Send(new GetSupportQuery());
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetCustomerInfo")]
        public async Task<IActionResult> GetCustomerInfo()
        {
            var query = new GetCustomerInfoQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("Notifications")]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null)
        {
            var query = new GetCustomerNotificationsQuery
            {
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("AddDevices")]
        public async Task<IActionResult> AddDevices([FromBody] DeliveryDeviceTokensDto request)
        {
            var command = new SaveFireBaseTokensForCustomerCommand
            {
                AndroidDevice = request.AndriodDevice,
                IosDevice = request.IosDevice
            };

            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("SaveLocation")]
        public async Task<IActionResult> SaveLocation([FromBody] CustomerLocationRequestDto request)
        {
            var command = new SaveCustomerLocationCommand
            {
                Longitude = request.Longitude,
                Latitude = request.Latitude
            };

            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(typeof(CustomerLocationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetLocation")]
        public async Task<IActionResult> GetLocation()
        {
            var query = new GetCustomerLocationQuery();
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileCommand command)
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
        [Route("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var command = new DeleteCustomerAccountCommand();
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}
