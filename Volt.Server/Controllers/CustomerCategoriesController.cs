using Application.Features.Category.DTOs;
using Application.Features.Category.Query.GetAllActiveCategoriesByCityQuery;
using Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserSession _userSession;

        public CustomerCategoriesController(IMediator mediator, IUserSession userSession)
        {
            _mediator = mediator;
            _userSession = userSession;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetAllActiveCategories")]
        public async Task<IActionResult> GetAllActiveCategories()
        {
            // Get CustomerId from session (UserId for customers)
            var customerId = _userSession.UserId;
            if (customerId <= 0)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail("Customer not authenticated"));
            }

            var query = new GetAllActiveCategoriesByCityQuery
            {
                CustomerId = customerId
            };

            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

