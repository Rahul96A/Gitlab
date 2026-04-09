using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Labels.Commands.CreateLabel;

public sealed record CreateLabelCommand(
    string Slug,
    string Name,
    string Color,
    string? Description
) : IRequest<LabelDto>;

public sealed class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$");
    }
}

public sealed class CreateLabelCommandHandler(
    IUnitOfWork unitOfWork,
    IAppDbContext db
) : IRequestHandler<CreateLabelCommand, LabelDto>
{
    public async Task<LabelDto> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var label = new Label
        {
            Name = request.Name,
            Color = request.Color,
            Description = request.Description,
            ProjectId = project.Id
        };

        await db.Labels.AddAsync(label, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LabelDto(label.Id, label.Name, label.Color, label.Description);
    }
}
