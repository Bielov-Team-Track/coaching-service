using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;
using Asp.Versioning;
using Coaching.Application.Interfaces.Services;
using Coaching.Application.DTOs.Drills;

namespace Coaching.Controllers.V1;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public class DrillsController : Shared.Microservices.Controllers.BaseApiController
{
    private readonly IDrillService _drillService;

    public DrillsController(
        IDrillService drillService,
        IJwtPayloadProvider jwtPayloadProvider)
        : base(jwtPayloadProvider)
    {
        _drillService = drillService;
    }

    /// <summary>
    /// Get all public drills with optional filtering.
    /// </summary>
    [HttpGet("drills")]
    public async Task<IActionResult> GetDrills([FromQuery] DrillFilterRequest filter)
    {
        Guid? userId = JwtPayload?.UserId != null && JwtPayload.UserId != Guid.Empty
            ? JwtPayload.UserId
            : null;

        var drills = await _drillService.GetByFilterAsync(filter, userId);
        return Ok(drills);
    }

    /// <summary>
    /// Get a drill by ID.
    /// </summary>
    [HttpGet("drills/{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        Guid? userId = null;
        if (JwtPayload?.UserId != null && JwtPayload.UserId != Guid.Empty)
            userId = JwtPayload.UserId;

        var drill = await _drillService.GetByIdAsync(id, userId);
        if (drill == null) return NotFound();
        return Ok(drill);
    }

    /// <summary>
    /// Create a new drill.
    /// </summary>
    [HttpPost("drills")]
    public async Task<IActionResult> Create([FromBody] CreateDrillDto request)
    {
        CheckIsUserLoggedIn();
        var created = await _drillService.CreateAsync(request, JwtPayload.UserId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, created);
    }

    /// <summary>
    /// Update a drill.
    /// </summary>
    [HttpPut("drills/{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDrillDto request)
    {
        CheckIsUserLoggedIn();

        if (request.Id != id)
            return BadRequest("ID in URL must match ID in body");

        var updated = await _drillService.UpdateAsync(request, JwtPayload.UserId);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a drill.
    /// </summary>
    [HttpDelete("drills/{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        await _drillService.DeleteAsync(id, JwtPayload.UserId);
        return NoContent();
    }

    /// <summary>
    /// Get current user's drills (includes private drills).
    /// </summary>
    [HttpGet("me/drills")]
    public async Task<IActionResult> GetMyDrills()
    {
        CheckIsUserLoggedIn();
        var drills = await _drillService.GetCurrentUserDrillsAsync(JwtPayload.UserId);
        return Ok(drills);
    }

    /// <summary>
    /// Get drills for a specific club.
    /// </summary>
    [HttpGet("clubs/{clubId:guid}/drills")]
    public async Task<IActionResult> GetClubDrills([FromRoute] Guid clubId)
    {
        CheckIsUserLoggedIn();
        var drills = await _drillService.GetClubDrillsAsync(clubId, JwtPayload.UserId);
        return Ok(drills);
    }

    // =========================================================================
    // LIKES
    // =========================================================================

    [HttpPost("drills/{id:guid}/like")]
    public async Task<IActionResult> LikeDrill([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        var status = await _drillService.LikeDrillAsync(id, JwtPayload.UserId);
        return Ok(status);
    }

    [HttpDelete("drills/{id:guid}/like")]
    public async Task<IActionResult> UnlikeDrill([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        var status = await _drillService.UnlikeDrillAsync(id, JwtPayload.UserId);
        return Ok(status);
    }

    [HttpGet("drills/{id:guid}/like")]
    public async Task<IActionResult> GetLikeStatus([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        var status = await _drillService.GetLikeStatusAsync(id, JwtPayload.UserId);
        return Ok(status);
    }

    // =========================================================================
    // BOOKMARKS
    // =========================================================================

    [HttpPost("drills/{id:guid}/bookmark")]
    public async Task<IActionResult> BookmarkDrill([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        var status = await _drillService.BookmarkDrillAsync(id, JwtPayload.UserId);
        return Ok(status);
    }

    [HttpDelete("drills/{id:guid}/bookmark")]
    public async Task<IActionResult> UnbookmarkDrill([FromRoute] Guid id)
    {
        CheckIsUserLoggedIn();
        var status = await _drillService.UnbookmarkDrillAsync(id, JwtPayload.UserId);
        return Ok(status);
    }

    [HttpGet("me/drills/bookmarks")]
    public async Task<IActionResult> GetMyBookmarks()
    {
        CheckIsUserLoggedIn();
        var bookmarks = await _drillService.GetUserBookmarksAsync(JwtPayload.UserId);
        return Ok(bookmarks);
    }

    // =========================================================================
    // COMMENTS
    // =========================================================================

    [HttpPost("drills/{id:guid}/comments")]
    public async Task<IActionResult> CreateComment([FromRoute] Guid id, [FromBody] CreateDrillCommentDto request)
    {
        CheckIsUserLoggedIn();
        var comment = await _drillService.CreateCommentAsync(id, request, JwtPayload.UserId);
        return Created($"/v1/drills/{id}/comments/{comment.Id}", comment);
    }

    [HttpGet("drills/{id:guid}/comments")]
    public async Task<IActionResult> GetComments(
        [FromRoute] Guid id,
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 20)
    {
        var comments = await _drillService.GetCommentsAsync(id, cursor, limit);
        return Ok(comments);
    }

    [HttpDelete("drills/{id:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment([FromRoute] Guid id, [FromRoute] Guid commentId)
    {
        CheckIsUserLoggedIn();
        await _drillService.DeleteCommentAsync(id, commentId, JwtPayload.UserId);
        return NoContent();
    }

    // =========================================================================
    // ATTACHMENTS
    // =========================================================================

    [HttpPost("drills/{id:guid}/attachments/upload-url")]
    public async Task<IActionResult> GetAttachmentUploadUrl([FromRoute] Guid id, [FromBody] DrillAttachmentUploadRequestDto request)
    {
        CheckIsUserLoggedIn();
        var response = await _drillService.GetAttachmentUploadUrlAsync(id, request, JwtPayload.UserId);
        return Ok(response);
    }

    [HttpPost("drills/{id:guid}/attachments")]
    public async Task<IActionResult> AddAttachment([FromRoute] Guid id, [FromBody] CreateDrillAttachmentDto request)
    {
        CheckIsUserLoggedIn();
        var attachment = await _drillService.AddAttachmentAsync(id, request, JwtPayload.UserId);
        return Created($"/v1/drills/{id}/attachments/{attachment.Id}", attachment);
    }

    [HttpDelete("drills/{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment([FromRoute] Guid id, [FromRoute] Guid attachmentId)
    {
        CheckIsUserLoggedIn();
        await _drillService.DeleteAttachmentAsync(id, attachmentId, JwtPayload.UserId);
        return NoContent();
    }

    // =========================================================================
    // ANIMATIONS
    // =========================================================================

    [HttpPut("drills/{id:guid}/animations")]
    public async Task<IActionResult> UpdateAnimations([FromRoute] Guid id, [FromBody] UpdateDrillAnimationsDto request)
    {
        CheckIsUserLoggedIn();
        var updated = await _drillService.UpdateAnimationsAsync(id, request, JwtPayload.UserId);
        return Ok(updated);
    }
}
