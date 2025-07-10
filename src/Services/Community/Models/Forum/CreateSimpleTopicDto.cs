using System.ComponentModel.DataAnnotations;

namespace CommunityService.Models.Forum
{
    public class CreateSimpleTopicDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;
    }
}
