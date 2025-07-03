using FluentValidation;
using Frontend.Models.Forum;

namespace Frontend.Validators.Forum
{
    public class CreateForumPostValidator : AbstractValidator<CreateForumPostDto>
    {
        public CreateForumPostValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithMessage("L'ID du topic est requis");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Le contenu est requis")
                .MaximumLength(5000)
                .WithMessage("Le contenu ne peut pas dépasser 5000 caractères")
                .MinimumLength(10)
                .WithMessage("Le contenu doit contenir au moins 10 caractères");
        }
    }
}
