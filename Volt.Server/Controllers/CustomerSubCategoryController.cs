using Application.Features.SubCategory.DTOs;
using Application.Features.SubCategory.Query.GetAllActiveSubcategoriesByCityQuery;
using Application.Features.SubCategory.Query.GetAllOffersByCityQuery;
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
    public class CustomerSubCategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserSession _userSession;

        public CustomerSubCategoryController(IMediator mediator, IUserSession userSession)
        {
            _mediator = mediator;
            _userSession = userSession;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SubCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetAllActiveSubcategories")]
        public async Task<IActionResult> GetAllActiveSubcategories()
        {
            // Get CustomerId from session (UserId for customers)
            var customerId = _userSession.UserId;
            if (customerId <= 0)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail("Customer not authenticated"));
            }

            var query = new GetAllActiveSubcategoriesByCityQuery
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

        [HttpGet]
        [ProducesResponseType(typeof(List<SubCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        [Route("GetAllOffers")]
        public async Task<IActionResult> GetAllOffers()
        {
            // Get CustomerId from session (UserId for customers)
            var customerId = _userSession.UserId;
            if (customerId <= 0)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail("Customer not authenticated"));
            }

            var query = new GetAllOffersByCityQuery
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

