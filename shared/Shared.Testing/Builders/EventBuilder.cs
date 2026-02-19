using Bogus;

namespace Shared.Testing.Builders;

public class EventBuilder
{
    private readonly Faker _faker = new();
    private Guid _id;
    private string _title;
    private string _description;
    private DateTime _startDate;
    private DateTime _endDate;
    private Guid _organizerId;
    private int _maxParticipants;
    private bool _isPublic;

    public EventBuilder()
    {
        _id = Guid.NewGuid();
        _title = _faker.Lorem.Sentence(3);
        _description = _faker.Lorem.Paragraph();
        _startDate = DateTime.UtcNow.AddDays(7);
        _endDate = DateTime.UtcNow.AddDays(7).AddHours(3);
        _organizerId = Guid.NewGuid();
        _maxParticipants = 12;
        _isPublic = true;
    }

    public EventBuilder WithId(Guid id) { _id = id; return this; }
    public EventBuilder WithTitle(string title) { _title = title; return this; }
    public EventBuilder WithDescription(string desc) { _description = desc; return this; }
    public EventBuilder WithStartDate(DateTime date) { _startDate = date; return this; }
    public EventBuilder WithEndDate(DateTime date) { _endDate = date; return this; }
    public EventBuilder WithOrganizerId(Guid id) { _organizerId = id; return this; }
    public EventBuilder WithMaxParticipants(int max) { _maxParticipants = max; return this; }
    public EventBuilder WithIsPublic(bool isPublic) { _isPublic = isPublic; return this; }

    public (Guid Id, string Title, string Description, DateTime StartDate, DateTime EndDate, Guid OrganizerId, int MaxParticipants, bool IsPublic) Build()
    {
        return (_id, _title, _description, _startDate, _endDate, _organizerId, _maxParticipants, _isPublic);
    }
}