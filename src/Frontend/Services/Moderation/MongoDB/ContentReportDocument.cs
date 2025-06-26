using Frontend.Models.Moderation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Frontend.Services.Moderation.MongoDB
{
    public class ContentReportDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public ContentType ContentType { get; set; }
        public string ContentId { get; set; }
        public string ContentUrl { get; set; }
        public string ContentSnippet { get; set; }
        public string ReportedByUserId { get; set; }
        public string ReportedByUsername { get; set; }
        public string ContentCreatorUserId { get; set; }
        public string ContentCreatorUsername { get; set; }
        public ReportReason Reason { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string ModeratorUserId { get; set; }
        public string ModeratorUsername { get; set; }
        public string ModeratorNotes { get; set; }
        public ReportPriority Priority { get; set; }

        // Méthode pour convertir un document MongoDB en modèle ContentReport
        public ContentReport ToContentReport()
        {
            return new ContentReport
            {
                Id = this.Id,
                ContentType = this.ContentType,
                ContentId = this.ContentId,
                ContentUrl = this.ContentUrl,
                ContentSnippet = this.ContentSnippet,
                ReportedByUserId = this.ReportedByUserId,
                ReportedByUsername = this.ReportedByUsername,
                ContentCreatorUserId = this.ContentCreatorUserId,
                ContentCreatorUsername = this.ContentCreatorUsername,
                Reason = this.Reason,
                Description = this.Description,
                CreatedAt = this.CreatedAt,
                Status = this.Status,
                StatusUpdatedAt = this.StatusUpdatedAt,
                ModeratorUserId = this.ModeratorUserId,
                ModeratorUsername = this.ModeratorUsername,
                ModeratorNotes = this.ModeratorNotes,
                Priority = this.Priority
            };
        }

        // Méthode statique pour convertir un ContentReport en document MongoDB
        public static ContentReportDocument FromContentReport(ContentReport report)
        {
            // Si l'ID est null ou vide, on ne l'inclut pas pour que MongoDB génère un ObjectId
            var document = new ContentReportDocument
            {
                ContentType = report.ContentType,
                ContentId = report.ContentId,
                ContentUrl = report.ContentUrl,
                ContentSnippet = report.ContentSnippet,
                ReportedByUserId = report.ReportedByUserId,
                ReportedByUsername = report.ReportedByUsername,
                ContentCreatorUserId = report.ContentCreatorUserId,
                ContentCreatorUsername = report.ContentCreatorUsername,
                Reason = report.Reason,
                Description = report.Description,
                CreatedAt = report.CreatedAt,
                Status = report.Status,
                StatusUpdatedAt = report.StatusUpdatedAt,
                ModeratorUserId = report.ModeratorUserId,
                ModeratorUsername = report.ModeratorUsername,
                ModeratorNotes = report.ModeratorNotes,
                Priority = report.Priority
            };

            // Seulement si l'ID n'est pas null ou vide et est un ObjectId valide
            if (!string.IsNullOrEmpty(report.Id) && ObjectId.TryParse(report.Id, out _))
            {
                document.Id = report.Id;
            }

            return document;
        }
    }
}
