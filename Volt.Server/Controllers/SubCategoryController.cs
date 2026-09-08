using System.Collections.Generic;
using Application.Common;
using Application.Features.SubCategory.Command.CreateSubCategoryCommand;
using Application.Features.SubCategory.Command.DeleteSubCategoryCommand;
using Application.Features.SubCategory.Command.ActivateSubCategoryCommand;
using Application.Features.SubCategory.Command.DeactivateSubCategoryCommand;
using Application.Features.SubCategory.Command.UpdateSubCategoryCommand;
using Application.Features.SubCategory.DTOs;
using Application.Features.SubCategory.Query.GetAllSubCategoriesQuery;
using Application.Features.SubCategory.Query.GetSubCategoriesByCategoryQuery;
using Application.Features.SubCategory.Query.GetSubCategoriesLookupQuery;
using Application.Features.SubCategory.Query.GetSubCategoryByIdQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubCategoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubCategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SubCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllSubCategoriesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SubCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetSubCategoryByIdQuery { SubCategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(typeof(List<SubCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var result = await _mediator.Send(new GetSubCategoriesByCategoryQuery { CategoryId = categoryId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("lookup")]
        [ProducesResponseType(typeof(List<SubCategoryLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLookup([FromQuery] int? categoryId)
        {
            var result = await _mediator.Send(new GetSubCategoriesLookupQuery { CategoryId = categoryId });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SubCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateSubCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut]
        [ProducesResponseType(typeof(SubCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateSubCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _mediator.Send(new ActivateSubCategoryCommand { SubCategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _mediator.Send(new DeactivateSubCategoryCommand { SubCategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteSubCategoryCommand { SubCategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

