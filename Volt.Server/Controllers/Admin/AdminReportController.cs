using Application.Features.AdminReport.Common;
using Application.Features.AdminReport.DTOs;
using Application.Features.AdminReport.Export;
using Application.Features.AdminReport.Query.GetCancellationDebtsReportQuery;
using Application.Features.AdminReport.Query.GetCancelledOrdersReportQuery;
using Application.Features.AdminReport.Query.GetOrdersDetailsReportQuery;
using Application.Features.AdminReport.Query.GetPayPalRefundsReportQuery;
using Application.Features.AdminReport.Query.GetPaymentsReportQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Response;

namespace Volt.Server.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminReportController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IReportExportService _exportService;

        public AdminReportController(IMediator mediator, IReportExportService exportService)
        {
            _mediator = mediator;
            _exportService = exportService;
        }

        // ───────────── Orders Details ─────────────

        [HttpGet("OrdersDetails")]
        [ProducesResponseType(typeof(OrdersDetailsReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOrdersDetails([FromQuery] GetOrdersDetailsReportQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("OrdersDetails/Export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportOrdersDetails(
            [FromQuery] GetOrdersDetailsReportQuery query,
            [FromQuery] ReportExportFormat format = ReportExportFormat.Excel)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));

            var file = _exportService.ExportOrdersDetails(result.Value, format);
            return File(file.Content, file.ContentType, file.FileName);
        }

        // ───────────── Cancelled Orders ─────────────

        [HttpGet("CancelledOrders")]
        [ProducesResponseType(typeof(CancelledOrdersReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCancelledOrders([FromQuery] GetCancelledOrdersReportQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("CancelledOrders/Export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportCancelledOrders(
            [FromQuery] GetCancelledOrdersReportQuery query,
            [FromQuery] ReportExportFormat format = ReportExportFormat.Excel)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));

            var file = _exportService.ExportCancelledOrders(result.Value, format);
            return File(file.Content, file.ContentType, file.FileName);
        }

        // ───────────── Cancellation Debts ─────────────

        [HttpGet("CancellationDebts")]
        [ProducesResponseType(typeof(CancellationDebtsReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCancellationDebts([FromQuery] GetCancellationDebtsReportQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("CancellationDebts/Export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportCancellationDebts(
            [FromQuery] GetCancellationDebtsReportQuery query,
            [FromQuery] ReportExportFormat format = ReportExportFormat.Excel)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));

            var file = _exportService.ExportCancellationDebts(result.Value, format);
            return File(file.Content, file.ContentType, file.FileName);
        }

        // ───────────── Payments ─────────────

        [HttpGet("Payments")]
        [ProducesResponseType(typeof(PaymentsReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPayments([FromQuery] GetPaymentsReportQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("Payments/Export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportPayments(
            [FromQuery] GetPaymentsReportQuery query,
            [FromQuery] ReportExportFormat format = ReportExportFormat.Excel)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));

            var file = _exportService.ExportPayments(result.Value, format);
            return File(file.Content, file.ContentType, file.FileName);
        }

        // ───────────── PayPal Refunds ─────────────

        [HttpGet("PayPalRefunds")]
        [ProducesResponseType(typeof(PayPalRefundsReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPayPalRefunds([FromQuery] GetPayPalRefundsReportQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));
            return Ok(result.Value);
        }

        [HttpGet("PayPalRefunds/Export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetail), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportPayPalRefunds(
            [FromQuery] GetPayPalRefundsReportQuery query,
            [FromQuery] ReportExportFormat format = ReportExportFormat.Excel)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return BadRequest(ProblemDetail.CreateProblemDetail(result.Error));

            var file = _exportService.ExportPayPalRefunds(result.Value, format);
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
