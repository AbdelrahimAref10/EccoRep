using System.Collections.Generic;
using Application.Common;
using Application.Features.Category.Command.CreateCategoryCommand;
using Application.Features.Category.Command.DeleteCategoryCommand;
using Application.Features.Category.Command.ActivateCategoryCommand;
using Application.Features.Category.Command.DeactivateCategoryCommand;
using Application.Features.Category.Command.UpdateCategoryCommand;
using Application.Features.Category.DTOs;
using Application.Features.Category.Query.GetAllCategoriesQuery;
using Application.Features.Category.Query.GetCategoriesLookupQuery;
using Application.Features.Category.Query.GetCategoryByIdQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllCategoriesQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery { CategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpGet("lookup")]
        [ProducesResponseType(typeof(List<CategoryLookupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLookup()
        {
            var result = await _mediator.Send(new GetCategoriesLookupQuery());
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }

        [HttpPut]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryCommand command)
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
            var result = await _mediator.Send(new ActivateCategoryCommand { CategoryId = id });
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
            var result = await _mediator.Send(new DeactivateCategoryCommand { CategoryId = id });
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
            var result = await _mediator.Send(new DeleteCategoryCommand { CategoryId = id });
            if (result.IsFailure)
            {
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            }
            return Ok(result.Value);
        }
    }
}

