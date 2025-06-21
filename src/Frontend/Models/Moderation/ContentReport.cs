using System;
using System.Collections.Generic;

namespace Frontend.Models.Moderation
{
    public class ContentReport
    {
        public string Id { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }
        public string ContentId { get; set; } = string.Empty;
        public string ContentUrl { get; set; } = string.Empty;
        public string ContentSnippet { get; set; } = string.Empty;
        public string ReportedByUserId { get; set; } = string.Empty;
        public string ReportedByUsername { get; set; } = string.Empty;
        public string ContentCreatorUserId { get; set; } = string.Empty;
        public string ContentCreatorUsername { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? ModeratorUserId { get; set; }
        public string? ModeratorUsername { get; set; }
        public string? ModeratorNotes { get; set; }
        public ModeratorAction? Action { get; set; }
        public ReportPriority Priority { get; set; }
    }

    public class CreateReportRequest
    {
        public ContentType ContentType { get; set; }
        public string ContentId { get; set; } = string.Empty;
        public string ContentUrl { get; set; } = string.Empty;
        public string ContentSnippet { get; set; } = string.Empty;
        public string ContentCreatorUserId { get; set; } = string.Empty;
        public string ContentCreatorUsername { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateReportStatusRequest
    {
        public ReportStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class ModeratorActionRequest
    {
        public ModeratorAction Action { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateReportPriorityRequest
    {
        public ReportPriority Priority { get; set; }
    }

    public class ModerationStatistics
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int ResolvedReports { get; set; }
        public int RejectedReports { get; set; }
        public int HighPriorityReports { get; set; }
        public Dictionary<ContentType, int> ReportsByContentType { get; set; } = new();
        public Dictionary<ReportReason, int> ReportsByReason { get; set; } = new();
        public Dictionary<string, int> TopReportedUsers { get; set; } = new();
        public Dictionary<string, int> TopReportingUsers { get; set; } = new();
        public double AverageResolutionTimeHours { get; set; }
    }
}
